namespace CleanCom.Application.Agenda;

/// <summary>
/// Exclusive lock per (ruta, date) inside a database transaction, so two people cannot take the last place of
/// the same day. Released when the handle is disposed; changes are kept only if <see cref="IAgendaLockHandle.CommitAsync"/> ran.
/// </summary>
public interface IAgendaLock
{
    Task<IAgendaLockHandle> AcquireAsync(int rutaId, DateOnly date, CancellationToken ct);
}

public interface IAgendaLockHandle : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}
