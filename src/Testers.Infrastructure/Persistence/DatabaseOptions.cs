namespace Testers.Infrastructure.Persistence;

/// <summary>
/// Bound from the <c>Database</c> section of <c>appsettings.json</c>. One MySQL server, two schemas
/// (databases in MySQL terms): the user-owned App schema (this API's outbox/inbox/audit/etc.) and
/// the shared TestPlan schema (other team's source-of-truth tables).
///
/// Sample <c>appsettings.json</c>:
/// <code>
/// "Database": {
///   "ConnectionString": "Server=localhost;User Id=testers;Password=...;Database=app_db",
///   "AppSchema":      "app_db",
///   "TestPlanSchema": "testplan_db"
/// }
/// </code>
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Single connection string used by both DbContexts. Default <c>Database=</c> is the App schema.</summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>Schema name for tables in the App DB (outbox, inbox, audit_log, app-owned aggregates).</summary>
    public string AppSchema { get; init; } = "app_db";

    /// <summary>Schema name for tables in the TestPlan DB (shared TestPlan structure, TaskRun submissions).</summary>
    public string TestPlanSchema { get; init; } = "testplan_db";
}
