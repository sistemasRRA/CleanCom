using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace CleanCom.IntegrationTests.Infrastructure;

/// <summary>
/// Throwaway SQL Server database for one test run. With CLEANCOM_TEST_SQL (a connection string without database, for
/// example the local SQL Server Express) it is created there and dropped at the end; without it, a SQL Server container
/// is started (CI; needs Docker). Always real SQL Server: geography columns, filtered indexes and transactions as in
/// production.
/// </summary>
public sealed class TestDatabase : IAsyncDisposable
{
    public const string ServerVariable = "CLEANCOM_TEST_SQL";

    private readonly string name = "cleancom_it_" + Guid.NewGuid().ToString("N")[..12];
    private MsSqlContainer? container;
    private string server = "";

    public string ConnectionString { get; private set; } = "";

    public async Task StartAsync()
    {
        var local = Environment.GetEnvironmentVariable(ServerVariable);
        if (!string.IsNullOrWhiteSpace(local))
        {
            server = local;
        }
        else
        {
            container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
            try
            {
                await container.StartAsync();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"The integration tests need SQL Server: set {ServerVariable} (for example " +
                    "\"Server=.\\SQLEXPRESS;Trusted_Connection=True;TrustServerCertificate=True;\") or start Docker.", e);
            }
            server = container.GetConnectionString();
        }

        ConnectionString = new SqlConnectionStringBuilder(server) { InitialCatalog = name, TrustServerCertificate = true }.ConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
            return;
        }

        if (server.Length == 0) return;
        SqlConnection.ClearAllPools();
        await using var connection = new SqlConnection(new SqlConnectionStringBuilder(server) { InitialCatalog = "master", TrustServerCertificate = true }.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        // The name is generated above (letters, digits and _), never user input.
#pragma warning disable CA2100
        command.CommandText = $"IF DB_ID('{name}') IS NOT NULL BEGIN ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]; END";
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }
}
