using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace Testers.Infrastructure.Persistence;

// Per-request scoped. Both DbContexts pull their DbConnection from here so cross-DB writes
// can sit in one transaction. For SQLite, both contexts live in the same file; for MySQL,
// they're separate schemas on the same server.
public sealed class SharedConnection(IOptions<DatabaseOptions> options) : IAsyncDisposable
{
    private readonly DatabaseOptions _options = options.Value;
    private DbConnection? _connection;
    private DbTransaction? _transaction;

    public async ValueTask<DbConnection> GetOpenAsync(CancellationToken ct = default)
    {
        if (_connection is null)
        {
            _connection = _options.Provider switch
            {
                DbProvider.Sqlite => new SqliteConnection(_options.ConnectionString),
                DbProvider.MySql => new MySqlConnection(_options.ConnectionString),
                _ => throw new InvalidOperationException($"Unknown DbProvider: {_options.Provider}"),
            };
            await _connection.OpenAsync(ct);
        }
        else if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync(ct);
        }

        return _connection;
    }

    public DbTransaction? CurrentTransaction => _transaction;

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("Transaction already active on this scope.");

        var conn = await GetOpenAsync(ct);
        _transaction = await conn.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
