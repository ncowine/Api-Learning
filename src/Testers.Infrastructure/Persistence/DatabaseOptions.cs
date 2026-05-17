namespace Testers.Infrastructure.Persistence;

// One MySQL server, two databases. AppSchema is ours; TestPlanSchema is shared with other apps.
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; init; } = string.Empty;
    public string AppSchema { get; init; } = "app_db";
    public string TestPlanSchema { get; init; } = "testplan_db";
}
