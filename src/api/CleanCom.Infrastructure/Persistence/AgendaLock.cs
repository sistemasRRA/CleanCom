using CleanCom.Application.Agenda;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CleanCom.Infrastructure.Persistence;

/// <summary>
/// SQL Server application lock (<c>sp_getapplock</c>) named after the ruta and date, owned by the transaction:
/// a second request for the same day waits until the first commits or rolls back. Other days are not blocked.
/// </summary>
public sealed class AgendaLock(CleanComDbContext db) : IAgendaLock
{
    /// <summary>How long a request waits for the same day before giving up.</summary>
    private const int TimeoutMs = 10_000;

    public async Task<IAgendaLockHandle> AcquireAsync(int rutaId, DateOnly date, CancellationToken ct)
    {
        var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var resource = $"agenda:{rutaId}:{date:yyyy-MM-dd}";
            await db.Database.ExecuteSqlRawAsync(
                """
                DECLARE @result int;
                EXEC @result = sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = {1};
                IF @result < 0 THROW 50001, 'Agenda lock timeout', 1;
                """,
                [resource, TimeoutMs],
                ct);
            return new Handle(transaction);
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    private sealed class Handle(IDbContextTransaction transaction) : IAgendaLockHandle
    {
        public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);

        // Without a commit, disposing rolls back. Either way the lock is released with the transaction.
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
