using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Testers.Application.Abstractions;
using Testers.Infrastructure.Audit;
using Testers.Infrastructure.Auth;
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
public sealed class AppDbContext : DbContext, IAppDbContext
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

    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

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

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        modelBuilder.Entity<ApiKey>(b =>
        {
            b.ToTable("api_key", schema);
            b.HasKey(k => k.Id);
            b.Property(k => k.Label).HasMaxLength(200).IsRequired();
            b.Property(k => k.KeyHash).HasMaxLength(128).IsRequired();
            b.HasIndex(k => k.KeyHash).IsUnique();
            b.Property(k => k.Prefix).HasMaxLength(20).IsRequired();
            b.Property(k => k.OwnerId).HasMaxLength(200).IsRequired();
            b.Property(k => k.Scopes)
                .HasColumnType("json")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>(),
                    new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<string>>(
                        (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
                        v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode(StringComparison.Ordinal))),
                        v => v.ToList()));
        });

        modelBuilder.ApplySnakeCaseNames();

        base.OnModelCreating(modelBuilder);
    }
}
