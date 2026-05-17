using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Testers.Infrastructure.Persistence;

/// <summary>
/// Cross-cutting EF Core conventions applied in <c>OnModelCreating</c> on both DbContexts.
/// Keeps DB-side naming snake_case (MySQL idiom) while CLR types stay PascalCase. Naming is
/// stylistic on MySQL (case-insensitive on most platforms) but consistent snake_case avoids
/// quoting and reads cleanly in raw SQL.
/// </summary>
public static class EfConventions
{
    /// <summary>Convert PascalCase / camelCase to snake_case. <c>OutboxMessage</c> → <c>outbox_message</c>.</summary>
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var sb = new StringBuilder(input.Length + 10);
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(input[i - 1]))
            {
                sb.Append('_');
            }

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Rewrites every table, column, primary key, foreign key, and index name in the model to snake_case.
    /// Call from <c>OnModelCreating</c> AFTER all <c>modelBuilder.Entity&lt;...&gt;(...)</c> calls so the
    /// renames cover everything.
    /// </summary>
    public static void ApplySnakeCaseNames(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (entity.GetTableName() is { } tableName)
            {
                entity.SetTableName(tableName.ToSnakeCase());
            }

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.Name.ToSnakeCase());
            }

            foreach (var key in entity.GetKeys())
            {
                if (key.GetName() is { } keyName)
                {
                    key.SetName(keyName.ToSnakeCase());
                }
            }

            foreach (var fk in entity.GetForeignKeys())
            {
                if (fk.GetConstraintName() is { } fkName)
                {
                    fk.SetConstraintName(fkName.ToSnakeCase());
                }
            }

            foreach (var idx in entity.GetIndexes())
            {
                if (idx.GetDatabaseName() is { } idxName)
                {
                    idx.SetDatabaseName(idxName.ToSnakeCase());
                }
            }
        }
    }
}
