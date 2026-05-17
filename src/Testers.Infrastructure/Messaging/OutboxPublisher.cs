using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Testers.Application.Abstractions;
using Testers.Infrastructure.Outbox;
using Testers.Infrastructure.Persistence;

namespace Testers.Infrastructure.Messaging;

/// <summary>
/// Background loop that drains the outbox into RabbitMQ.
///
/// Per tick:
/// <list type="number">
///   <item>Pull up to <see cref="RabbitOptions.OutboxBatchSize"/> unprocessed rows, oldest first.</item>
///   <item>For each: publish to the exchange with the row's routing key + a basic envelope.
///         Wait for the broker's publish-confirm (otherwise the broker may drop the message
///         silently between accepting it and persisting). On confirm: mark processed.
///         On failure: increment AttemptCount + record LastError; the row stays unprocessed
///         and is retried on the next tick.</item>
///   <item>If the batch was empty, sleep <see cref="RabbitOptions.OutboxPollIntervalSeconds"/>;
///         otherwise loop straight back to drain more.</item>
/// </list>
///
/// Single-instance for now. To run multiple publisher instances later, the SELECT for the batch
/// becomes <c>... FOR UPDATE SKIP LOCKED</c> so each instance claims a distinct slice; that's a
/// MySQL 8+ feature, deferred until horizontal scaling is needed.
/// </summary>
internal sealed class OutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitConnectionFactory _connectionFactory;
    private readonly RabbitOptions _options;
    private readonly IClock _clock;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(
        IServiceScopeFactory scopeFactory,
        RabbitConnectionFactory connectionFactory,
        IOptions<RabbitOptions> options,
        IClock clock,
        ILogger<OutboxPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _connectionFactory = connectionFactory;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisher starting; exchange '{Exchange}', batch size {Batch}",
            _options.ExchangeName, _options.OutboxBatchSize);

        // One channel reused for publishing. Channels are not thread-safe but this loop is
        // single-threaded so that's fine.
        using var channel = OpenChannelWithExchangeDeclared();
        channel.ConfirmSelect();

        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, _options.OutboxPollIntervalSeconds));
        var confirmTimeout = TimeSpan.FromSeconds(Math.Max(1, _options.PublisherConfirmTimeoutSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var publishedAny = await DrainOnceAsync(channel, confirmTimeout, stoppingToken).ConfigureAwait(false);
                if (!publishedAny)
                {
                    await Task.Delay(pollInterval, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxPublisher loop error; retrying in {Interval}s", pollInterval.TotalSeconds);
                try
                {
                    await Task.Delay(pollInterval, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("OutboxPublisher stopping.");
    }

    private async Task<bool> DrainOnceAsync(IModel channel, TimeSpan confirmTimeout, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var batch = await db.Outbox
            .Where(o => o.ProcessedAt == null)
            .OrderBy(o => o.OccurredAt)
            .Take(_options.OutboxBatchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (batch.Count == 0)
        {
            return false;
        }

        foreach (var msg in batch)
        {
            try
            {
                Publish(channel, msg);
                channel.WaitForConfirmsOrDie(confirmTimeout);
                msg.MarkProcessed(_clock.UtcNow);
            }
#pragma warning disable CA1031 // catch-all: any failure here parks the row for retry, not the process.
            catch (Exception ex)
            {
                msg.RecordFailure(ex.Message);
                _logger.LogWarning(ex,
                    "Failed to publish OutboxMessage {Id} (attempt {Attempt})", msg.Id, msg.AttemptCount);
            }
#pragma warning restore CA1031
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    private IModel OpenChannelWithExchangeDeclared()
    {
        var channel = _connectionFactory.GetOrOpen().CreateModel();
        channel.ExchangeDeclare(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);
        return channel;
    }

    private void Publish(IModel channel, OutboxMessage msg)
    {
        var props = channel.CreateBasicProperties();
        props.MessageId = msg.Id.ToString();
        props.ContentType = "application/json";
        props.Persistent = true;
        props.Timestamp = new AmqpTimestamp(new DateTimeOffset(msg.OccurredAt, TimeSpan.Zero).ToUnixTimeSeconds());
        props.Headers = new Dictionary<string, object>
        {
            ["EventType"] = msg.EventType,
            ["OccurredAt"] = msg.OccurredAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        };
        if (!string.IsNullOrEmpty(msg.CorrelationId))
        {
            props.CorrelationId = msg.CorrelationId;
        }

        channel.BasicPublish(
            exchange: _options.ExchangeName,
            routingKey: msg.RoutingKey,
            mandatory: true,
            basicProperties: props,
            body: Encoding.UTF8.GetBytes(msg.Payload));
    }
}
