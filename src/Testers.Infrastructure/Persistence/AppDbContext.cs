using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Testers.Infrastructure.Audit;
using Testers.Infrastructure.Outbox;

namespace Testers.Infrastructure.Persistence;

/// <summary>
/// The user-owned DB. Holds the outbox, inbox, audit log, and any app-specific entities
/// (read-model projections, API keys, etc.). Shares its underlying MySqlConnection with
/// <see cref="TestPlanDbContext"/> via <see cref="SharedConnection"/> so cross-DB writes
/// commit atomically within one transaction on the same MySQL server.
///
/// Schema (database name in MySQL terms) is configurable via <see cref="DatabaseOptions.AppSchema"/>;
/// entities are mapped with explicit <c>ToTable(name, schema)</c> so cross-DB queries can use
/// fully-qualified names without changing the connection's current database.
/// </summary>
public sealed class AppDbContext : DbContext
{
    private readonly DatabaseOptions _databaseOptions;

    public AppDbContext(DbContextOptions<AppDbContext> options, IOptions<DatabaseOptions> databaseOptions)
        : base(options)
    {
        _databaseOptions = databaseOptions.Value;
    }

    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    public DbSet<InboxMessage> Inbox => Set<InboxMessage>();

    public DbSet<AuditLog> AuditLog => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        var schema = _databaseOptions.AppSchema;

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("outbox_message", schema);
            b.HasKey(o => o.Id);
            b.Property(o => o.EventType).HasMaxLength(300).IsRequired();
            b.Property(o => o.RoutingKey).HasMaxLength(200).IsRequired();
            b.Property(o => o.Payload).HasColumnType("json").IsRequired();
            b.Property(o => o.CorrelationId).HasMaxLength(100);
            b.Property(o => o.LastError).HasMaxLength(2000);
            // The OutboxPublisher polls "unprocessed, oldest-first" — this index covers it.
            b.HasIndex(o => new { o.ProcessedAt, o.OccurredAt });
        });

        modelBuilder.Entity<InboxMessage>(b =>
        {
            b.ToTable("inbox_message", schema);
            b.HasKey(i => new { i.MessageId, i.ConsumerName });
            b.Property(i => i.ConsumerName).HasMaxLength(200);
        });

        modelBuilder.Entity<AuditLog>(b =>
        {
            b.ToTable("audit_log", schema);
            b.HasKey(a => a.Id);
            b.Property(a => a.EntityType).HasMaxLength(300).IsRequired();
            b.Property(a => a.EntityKey).HasMaxLength(200).IsRequired();
            b.Property(a => a.Action).HasMaxLength(20).IsRequired();
            b.Property(a => a.UserId).HasMaxLength(200).IsRequired();
            b.Property(a => a.UserDisplayName).HasMaxLength(300).IsRequired();
            b.Property(a => a.StateJson).HasColumnType("json");
            b.Property(a => a.CorrelationId).HasMaxLength(100);
            b.HasIndex(a => new { a.EntityType, a.EntityKey });
            b.HasIndex(a => a.OccurredAt);
        });

        modelBuilder.ApplySnakeCaseNames();

        base.OnModelCreating(modelBuilder);
    }
}
