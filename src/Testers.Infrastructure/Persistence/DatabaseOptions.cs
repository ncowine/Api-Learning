namespace Testers.Infrastructure.Persistence;

public enum DbProvider { Sqlite, MySql }

// SQLite local dev: one file, all tables; schemas ignored (EF Sqlite doesn't support them).
// MySQL prod: one server, two databases (schemas), atomic cross-schema writes on shared connection.
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public DbProvider Provider { get; init; } = DbProvider.Sqlite;

    // For SQLite: Data Source=app.db. For MySQL: Server=...;Database=app_db;...
    public string ConnectionString { get; init; } = "Data Source=app.db";

    // MySQL only. Null/unset for SQLite (which ignores schemas anyway).
    public string? AppSchema { get; init; }

    public string? TestPlanSchema { get; init; }
}
