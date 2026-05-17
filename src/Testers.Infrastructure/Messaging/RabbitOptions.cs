namespace Testers.Infrastructure.Messaging;

/// <summary>
/// Bound from the <c>Rabbit</c> section of <c>appsettings.json</c>. One topic exchange per
/// bounded context (this API publishes to <c>testers.events.v1</c>); routing keys carry the
/// <c>{aggregate}.{event}.v1</c> convention. Dead-letter exchange (<c>.dlx</c>) is named
/// derivatively. Retry tiers (1s → 5s → 30s → 5m → DLQ) require the
/// <c>rabbitmq-delayed-message-exchange</c> plugin enabled on the broker (the user has it).
///
/// Sample <c>appsettings.json</c>:
/// <code>
/// "Rabbit": {
///   "Uri":          "amqp://testers:secret@localhost:5672/",
///   "ExchangeName": "testers.events.v1",
///   "PublisherConfirmTimeoutSeconds": 5,
///   "OutboxPollIntervalSeconds":      1,
///   "OutboxBatchSize":                100
/// }
/// </code>
/// </summary>
public sealed class RabbitOptions
{
    public const string SectionName = "Rabbit";

    /// <summary>amqp:// or amqps:// URI to the broker.</summary>
    public string Uri { get; init; } = "amqp://guest:guest@localhost:5672/";

    /// <summary>Topic exchange this API publishes to. Auto-declared (idempotent) at startup.</summary>
    public string ExchangeName { get; init; } = "testers.events.v1";

    /// <summary>How long to wait for a broker publish-confirm before treating the publish as failed.</summary>
    public int PublisherConfirmTimeoutSeconds { get; init; } = 5;

    /// <summary>How often the OutboxPublisher polls when the previous batch was empty.</summary>
    public int OutboxPollIntervalSeconds { get; init; } = 1;

    /// <summary>How many outbox rows to claim per poll.</summary>
    public int OutboxBatchSize { get; init; } = 100;
}
