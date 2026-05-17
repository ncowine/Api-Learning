namespace Testers.Infrastructure.Messaging;

// Topic exchange we publish to. Routing keys follow {aggregate}.{event}.v1.
public sealed class RabbitOptions
{
    public const string SectionName = "Rabbit";

    public string Uri { get; init; } = "amqp://guest:guest@localhost:5672/";
    public string ExchangeName { get; init; } = "testers.events.v1";
    public int PublisherConfirmTimeoutSeconds { get; init; } = 5;
    public int OutboxPollIntervalSeconds { get; init; } = 1;
    public int OutboxBatchSize { get; init; } = 100;
}
