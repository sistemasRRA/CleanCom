using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CleanCom.Infrastructure.Persistence;

/// <summary>
/// Database naming convention: tables, columns, keys, foreign keys and indexes in snake_case
/// (<c>CupoParadas</c> → <c>cupo_paradas</c>). Applied once to the whole model, so new entities follow it without extra code.
/// </summary>
public static class SnakeCaseNaming
{
    public static void Apply(ModelBuilder builder)
    {
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            if (entity.GetTableName() is { } table)
            {
                entity.SetTableName(ToSnakeCase(table));
            }

            var storeObject = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());

            foreach (var property in entity.GetProperties())
            {
                if (property.GetColumnName(storeObject) is { } column)
                {
                    property.SetColumnName(ToSnakeCase(column));
                }
            }
            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName()!));
            }
            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName()!));
            }
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
            }
        }
    }

    /// <summary>"ZonaVisitDays" → "zona_visit_days"; "PK_Rutas" → "pk_rutas"; acronyms stay together ("RFC" → "rfc").</summary>
    public static string ToSnakeCase(string name)
    {
        var result = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                var previous = i > 0 ? name[i - 1] : '_';
                var next = i + 1 < name.Length ? name[i + 1] : '_';
                var startsWord = previous != '_' && (char.IsLower(previous) || char.IsDigit(previous) || (char.IsUpper(previous) && char.IsLower(next)));
                if (startsWord)
                {
                    result.Append('_');
                }
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }
}
