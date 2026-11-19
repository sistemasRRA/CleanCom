using System.Data;
using CleanCom.Application.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CleanCom.Infrastructure.Persistence;

/// <summary>
/// SqlBulkCopy with the table and column names of the EF model (snake_case), so an import of thousands of rows takes
/// seconds instead of thousands of round trips. Identity and computed columns are left to SQL Server.
/// </summary>
internal sealed class BulkInserter(CleanComDbContext db) : IBulkInserter
{
    public async Task InsertAsync<T>(IReadOnlyCollection<T> rows, CancellationToken ct) where T : class
    {
        if (rows.Count == 0)
        {
            return;
        }
        var entity = db.Model.FindEntityType(typeof(T)) ?? throw new InvalidOperationException($"{typeof(T).Name} is not mapped.");
        var table = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
        var columns = entity.GetProperties()
            .Where(p => !(p.IsPrimaryKey() && p.ValueGenerated == ValueGenerated.OnAdd) && p.GetComputedColumnSql() is null && p.PropertyInfo is not null)
            .Select(p => (Property: p, Name: p.GetColumnName(table)!))
            .ToList();

        using var data = new DataTable();
        foreach (var (property, name) in columns)
        {
            var type = property.GetValueConverter()?.ProviderClrType ?? property.ClrType;
            data.Columns.Add(name, Nullable.GetUnderlyingType(type) ?? type);
        }
        foreach (var row in rows)
        {
            data.Rows.Add(columns.Select(c =>
            {
                var value = c.Property.PropertyInfo!.GetValue(row);
                if (value is not null && c.Property.GetValueConverter() is { } converter)
                {
                    value = converter.ConvertToProvider(value);
                }
                return value ?? DBNull.Value;
            }).ToArray());
        }

        var connection = (SqlConnection)db.Database.GetDbConnection();
        var opened = connection.State != ConnectionState.Open;
        if (opened)
        {
            await connection.OpenAsync(ct);
        }
        try
        {
            using var copy = new SqlBulkCopy(connection, SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.UseInternalTransaction, null)
            {
                DestinationTableName = $"[{table.Name}]",
                BatchSize = 5000,
                BulkCopyTimeout = 300,
            };
            foreach (var (_, name) in columns)
            {
                copy.ColumnMappings.Add(name, name);
            }
            await copy.WriteToServerAsync(data, ct);
        }
        finally
        {
            if (opened)
            {
                await connection.CloseAsync();
            }
        }
    }
}
