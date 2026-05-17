using System.Data.Common;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace Testers.Infrastructure.Persistence;

/// <summary>
/// Per-request scoped wrapper around a single <see cref="MySqlConnection"/> that both
/// <c>AppDbContext</c> and <c>TestPlanDbContext</c> share. This is what makes cross-DB writes
/// atomic on a same-server MySQL setup: one connection, one transaction, both schemas reachable
/// via fully-qualified table names (configured per-entity in each context's <c>OnModelCreating</c>).
///
/// Lifecycle: scoped per HTTP request. The connection opens lazily on first DbContext touch.
/// <c>UnitOfWork</c> calls <see cref="BeginTransactionAsync"/> / <see cref="CommitAsync"/> /
/// <see cref="RollbackAsync"/> for commands; queries skip transactions and just share the connection.
/// Both connection and transaction are disposed when the request scope ends.
/// </summary>
public sealed class SharedConnection(IOptions<DatabaseOptions> options) : IAsyncDisposable
{
    private readonly string _connectionString = options.Value.ConnectionString;
    private MySqlConnection? _connection;
    private DbTransaction? _transaction;

    /// <summary>The (lazily opened) underlying MySqlConnection. Both DbContexts use this same instance.</summary>
    public async ValueTask<DbConnection> GetOpenAsync(CancellationToken ct = default)
    {
        if (_connection is null)
        {
            _connection = new MySqlConnection(_connectionString);
            await _connection.OpenAsync(ct).ConfigureAwait(false);
        }
        else if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync(ct).ConfigureAwait(false);
        }

        return _connection;
    }

    /// <summary>The active transaction, if any. EF Core's <c>Database.UseTransaction</c> needs this on each context.</summary>
    public DbTransaction? CurrentTransaction => _transaction;

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("Transaction already active on this scope.");
        }

        var conn = await GetOpenAsync(ct).ConfigureAwait(false);
        _transaction = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.CommitAsync(ct).ConfigureAwait(false);
        await _transaction.DisposeAsync().ConfigureAwait(false);
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync(ct).ConfigureAwait(false);
        await _transaction.DisposeAsync().ConfigureAwait(false);
        _transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync().ConfigureAwait(false);
            _transaction = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
    }
}
