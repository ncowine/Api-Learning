using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Testers.Infrastructure.Persistence;

/// <summary>
/// The shared TestPlan DB owned by another team. This API has heavy read + write access
/// (TaskRun submissions land here so all consuming apps see them).
///
/// No entities are mapped yet — those land in step #8 with TestPlan / Category / SubCategory /
/// TaskDefinition (reads) and TaskRun / Comment / BugLink (writes). The context exists now so
/// the shared-connection + cross-DB transaction plumbing has a second consumer to exercise.
/// </summary>
public sealed class TestPlanDbContext : DbContext
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052",
        Justification = "Read once we map entities to TestPlanSchema in step #8.")]
    private readonly DatabaseOptions _databaseOptions;

    public TestPlanDbContext(DbContextOptions<TestPlanDbContext> options, IOptions<DatabaseOptions> databaseOptions)
        : base(options)
    {
        _databaseOptions = databaseOptions.Value;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Entity mappings land in step #8 using _databaseOptions.TestPlanSchema for fully-qualified names.

        modelBuilder.ApplySnakeCaseNames();
        base.OnModelCreating(modelBuilder);
    }
}
