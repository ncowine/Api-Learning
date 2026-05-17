using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Testers.SharedKernel.Abstractions;
using Testers.Domain.Execution;

namespace Testers.Infrastructure.Persistence;

// Shared TestPlan DB (other team's). TaskRun submissions go here so all consuming apps see them.
public sealed class TestPlanDbContext : DbContext, ITestPlanDbContext
{
    private readonly DatabaseOptions _databaseOptions;

    public TestPlanDbContext(DbContextOptions<TestPlanDbContext> options, IOptions<DatabaseOptions> databaseOptions)
        : base(options)
    {
        _databaseOptions = databaseOptions.Value;
    }

    public DbSet<TaskRun> TaskRuns => Set<TaskRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        var schema = _databaseOptions.TestPlanSchema;

        modelBuilder.Entity<TaskRun>(b =>
        {
            b.ToTable("task_run", schema);
            b.HasKey(r => r.Id);
            b.Property(r => r.Id).ValueGeneratedNever();
            b.Property(r => r.TaskDefinitionId).IsRequired();
            b.Property(r => r.BuildId).IsRequired();
            b.Property(r => r.TesterId).HasMaxLength(200).IsRequired();
            b.Property(r => r.Outcome)
                .HasConversion(o => o.Value, v => TaskOutcome.FromValue(v))
                .IsRequired();
            b.Property(r => r.Note).HasMaxLength(2000);
            b.Property(r => r.ActionedAt).IsRequired();

            // IAuditable from AggregateRoot<TId>.
            b.Property(r => r.CreatedAt);
            b.Property(r => r.CreatedBy).HasMaxLength(200);
            b.Property(r => r.ModifiedAt);
            b.Property(r => r.ModifiedBy).HasMaxLength(200);

            // Events live in memory only.
            b.Ignore(r => r.DomainEvents);

            b.HasIndex(r => new { r.BuildId, r.TaskDefinitionId });
            b.HasIndex(r => new { r.TesterId, r.ActionedAt });

            b.OwnsMany(r => r.Comments, c =>
            {
                c.ToTable("task_run_comment", schema);
                c.WithOwner().HasForeignKey("TaskRunId");
                c.HasKey(cmt => cmt.Id);
                c.Property(cmt => cmt.Id).ValueGeneratedNever();
                c.Property(cmt => cmt.Body).HasMaxLength(10000).IsRequired();
                c.Property(cmt => cmt.AuthorId).HasMaxLength(200).IsRequired();
                c.Property(cmt => cmt.AddedAt);
            });

            b.OwnsMany(r => r.BugLinks, bl =>
            {
                bl.ToTable("task_run_bug_link", schema);
                bl.WithOwner().HasForeignKey("TaskRunId");
                bl.HasKey(link => link.Id);
                bl.Property(link => link.Id).ValueGeneratedNever();
                bl.Property(link => link.ExternalBugId).HasMaxLength(200).IsRequired();
                bl.Property(link => link.BugTrackerUrl).HasMaxLength(1000);
                bl.Property(link => link.LinkedAt);
            });
        });

        modelBuilder.ApplySnakeCaseNames();
        base.OnModelCreating(modelBuilder);
    }
}
