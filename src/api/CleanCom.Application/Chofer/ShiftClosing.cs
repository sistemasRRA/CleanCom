using CleanCom.Application.Common;
using CleanCom.Domain.Fleet;
using CleanCom.Domain.Routing;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Chofer;

/// <summary>
/// Closing a shift, the same from the chofer app (POST /chofer/turno/cerrar) and from the office (permiso
/// cerrar-turnos): end time and km, the unidad's km and the corte de caja with what was declared.
/// </summary>
internal static class ShiftClosing
{
    /// <summary>Paradas of the shift not closed yet (pending, en camino or en sitio).</summary>
    public static Task<int> OpenParadasAsync(ICleanComDbContext db, int rutaDiaId, CancellationToken ct) =>
        db.Paradas.CountAsync(p => p.RutaDiaId == rutaDiaId && p.Active
            && (p.State == ParadaState.Pendiente || p.State == ParadaState.EnCamino || p.State == ParadaState.EnSitio), ct);

    /// <summary>Marks the shift closed and leaves its corte de caja (not saved). <paramref name="km"/> may be unknown (office).</summary>
    public static async Task CloseAsync(
        ICleanComDbContext db,
        RutaDia rutaDia,
        int? operadorId,
        int? km,
        IEnumerable<DeclaredPaymentRequest> declared,
        string? notes,
        DateTimeOffset at,
        int? endedBy,
        string? localId,
        CancellationToken ct)
    {
        var startedAt = rutaDia.ShiftStartedAt!.Value;
        rutaDia.ShiftEndedAt = at < startedAt ? startedAt : at;
        rutaDia.EndKm = km;
        rutaDia.ShiftEndLocalId = localId;
        rutaDia.ShiftEndedBy = endedBy;
        if (km is { } reading && rutaDia.UnidadId is { } unidadId && await db.Unidades.FirstOrDefaultAsync(u => u.Id == unidadId, ct) is { } unidad)
        {
            UpdateKm(unidad, reading, at);
        }
        db.CortesCaja.Add(new CorteCaja
        {
            RutaDiaId = rutaDia.Id,
            OperadorId = operadorId,
            Date = rutaDia.Date,
            ChoferNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Lines = declared.Select(l => new CorteCajaLine { Method = l.Method, Declared = l.Amount }).ToList(),
        });
    }

    /// <summary>The unidad's km only goes up (an older reading that syncs late does not lower it).</summary>
    public static void UpdateKm(Unidad unidad, int km, DateTimeOffset at)
    {
        if (km > unidad.Km)
        {
            unidad.Km = km;
            unidad.KmUpdatedAt = at;
        }
    }
}
