using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Testers.SharedKernel.Abstractions;
using Testers.Infrastructure.Outbox;
using Testers.Infrastructure.Persistence;

namespace Testers.Infrastructure.Messaging;

// Polls unprocessed outbox rows, publishes to RabbitMQ with publisher-confirms, marks processed.
// Single-instance for now (no SELECT FOR UPDATE SKIP LOCKED yet).
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

        // One channel reused for the loop. Channels aren't thread-safe; we're single-threaded here.
        using var channel = OpenChannel();
        channel.ConfirmSelect();

        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, _options.OutboxPollIntervalSeconds));
        var confirmTimeout = TimeSpan.FromSeconds(Math.Max(1, _options.PublisherConfirmTimeoutSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await DrainOnce(channel, confirmTimeout, stoppingToken))
                    await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxPublisher loop error; retrying in {Interval}s", pollInterval.TotalSeconds);
                try { await Task.Delay(pollInterval, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }

        _logger.LogInformation("OutboxPublisher stopping.");
    }

    private async Task<bool> DrainOnce(IModel channel, TimeSpan confirmTimeout, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var batch = await db.Outbox
            .Where(o => o.ProcessedAt == null)
            .OrderBy(o => o.OccurredAt)
            .Take(_options.OutboxBatchSize)
            .ToListAsync(ct);

        if (batch.Count == 0) return false;

        foreach (var msg in batch)
        {
            try
            {
                Publish(channel, msg);
                channel.WaitForConfirmsOrDie(confirmTimeout);
                msg.MarkProcessed(_clock.UtcNow);
            }
#pragma warning disable CA1031  // any failure here parks the row for retry
            catch (Exception ex)
            {
                msg.RecordFailure(ex.Message);
                _logger.LogWarning(ex, "Failed to publish OutboxMessage {Id} (attempt {Attempt})", msg.Id, msg.AttemptCount);
            }
#pragma warning restore CA1031
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    private IModel OpenChannel()
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
        if (!string.IsNullOrEmpty(msg.CorrelationId)) props.CorrelationId = msg.CorrelationId;

        channel.BasicPublish(
            exchange: _options.ExchangeName,
            routingKey: msg.RoutingKey,
            mandatory: true,
            basicProperties: props,
            body: Encoding.UTF8.GetBytes(msg.Payload));
    }
}
