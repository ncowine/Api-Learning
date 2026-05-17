using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Testers.Infrastructure.Messaging;

// One IConnection per process (RabbitMQ best practice). Lazy-opens; channels are per-operation.
public sealed class RabbitConnectionFactory : IDisposable
{
    private readonly RabbitOptions _options;
    private readonly ILogger<RabbitConnectionFactory> _logger;
    private readonly object _gate = new();
    private IConnection? _connection;

    public RabbitConnectionFactory(IOptions<RabbitOptions> options, ILogger<RabbitConnectionFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public IConnection GetOrOpen()
    {
        if (_connection is { IsOpen: true }) return _connection;

        lock (_gate)
        {
            if (_connection is { IsOpen: true }) return _connection;

            _connection?.Dispose();

            var factory = new ConnectionFactory
            {
                Uri = new Uri(_options.Uri),
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                DispatchConsumersAsync = true,
                ClientProvidedName = "testers-api",
            };

            _connection = factory.CreateConnection();
            _logger.LogInformation("Opened RabbitMQ connection to {Uri}", _options.Uri);
            return _connection;
        }
    }

    public void Dispose()
    {
        if (_connection is null) return;

        try { _connection.Close(); }
        catch (Exception ex) { _logger.LogWarning(ex, "Error closing RabbitMQ connection."); }

        _connection.Dispose();
        _connection = null;
    }
}
