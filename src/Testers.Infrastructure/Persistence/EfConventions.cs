using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Testers.Infrastructure.Persistence;

// snake_case naming on the DB side; CLR stays PascalCase.
public static class EfConventions
{
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var sb = new StringBuilder(input.Length + 10);
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(input[i - 1]))
                sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    // Call AFTER all modelBuilder.Entity<...>(...) calls in OnModelCreating.
    public static void ApplySnakeCaseNames(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (entity.GetTableName() is { } tableName)
                entity.SetTableName(tableName.ToSnakeCase());

            foreach (var property in entity.GetProperties())
                property.SetColumnName(property.Name.ToSnakeCase());

            foreach (var key in entity.GetKeys())
                if (key.GetName() is { } keyName) key.SetName(keyName.ToSnakeCase());

            foreach (var fk in entity.GetForeignKeys())
                if (fk.GetConstraintName() is { } fkName) fk.SetConstraintName(fkName.ToSnakeCase());

            foreach (var idx in entity.GetIndexes())
                if (idx.GetDatabaseName() is { } idxName) idx.SetDatabaseName(idxName.ToSnakeCase());
        }
    }
}
