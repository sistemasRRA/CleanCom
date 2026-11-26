using CleanCom.Application.Common;
using CleanCom.Domain.Agenda;
using CleanCom.Application.Operations;
using CleanCom.Domain.Common;
using CleanCom.Domain.Fleet;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Chofer;

/// <summary>
/// Inicio and fin de turno and fallas of the unidad in the chofer app (docs/pantallas/apps/chofer.md, Flujo del turno,
/// steps 1 and 7, and Otras funciones). The odometer readings update the unidad's km; the end of the shift leaves the
/// chofer's declaration for the corte de caja. Every action carries the app's local id, so a resend after losing
/// signal is not saved twice.
/// </summary>
public class ChoferShiftService(
    ICleanComDbContext db,
    ISettingService settings,
    IFileStore files,
    ChoferService chofer,
    IValidator<StartShiftRequest> startValidator,
    IValidator<EndShiftRequest> endValidator,
    IValidator<ReportFailureRequest> failureValidator)
{
    public const long MaxPhotoBytes = ChoferVisitService.MaxPhotoBytes;

    /// <summary>
    /// Inicio de turno: the odometer reading of the unidad the office assigned to the ruta that day (required). A
    /// reading lower than the unidad's km is rejected; one more than Unidad.KmMaximoPorTurno above it asks to confirm.
    /// </summary>
    public async Task<ChoferRutaDto> StartAsync(StartShiftRequest r, CancellationToken ct)
    {
        await startValidator.ValidateAndThrowAsync(r, ct);
        var day = chofer.ShiftDay(r.At);
        var (rutaDia, _) = await chofer.ShiftRutaDiaAsync(day, ct);
        if (rutaDia.ShiftStartedAt is { } started)
        {
            if (r.LocalId is not null && r.LocalId == rutaDia.ShiftStartLocalId)
            {
                return await chofer.RutaAsync(day, ct);
            }
            throw new DomainException($"Ya iniciaste tu turno a las {Time(started)}.");
        }

        var unidad = await UnidadForStartAsync(rutaDia, ct);
        var warning = ShiftRules.CheckStart(r.Km, unidad.Km, unidad.Plate, await settings.GetIntegerAsync(SettingKeys.UnidadMaxKmPerShift, ct));
        if (warning is not null && !r.Confirm)
        {
            throw new ConfirmationRequiredException($"{warning} ¿Es correcto el kilometraje de {ShiftRules.Km(r.Km)} km?");
        }

        var at = chofer.ActionTime(r.At, day);
        rutaDia.ShiftStartedAt = at;
        rutaDia.StartKm = r.Km;
        rutaDia.ShiftStartLocalId = r.LocalId;
        UpdateKm(unidad, r.Km, at);
        await db.SaveChangesAsync(ct);
        return await chofer.RutaAsync(day, ct) with { Message = $"Turno iniciado con la unidad {unidad.Plate}." };
    }

    /// <summary>
    /// Fin de turno: the final odometer reading and what the chofer collected per forma de pago (corte de caja). A long
    /// shift or paradas still open ask to confirm.
    /// </summary>
    public async Task<ChoferRutaDto> EndAsync(EndShiftRequest r, CancellationToken ct)
    {
        await endValidator.ValidateAndThrowAsync(r, ct);
        var day = chofer.ShiftDay(r.At);
        var (rutaDia, operador) = await chofer.ShiftRutaDiaAsync(day, ct);
        if (rutaDia.ShiftEndedAt is { } ended)
        {
            if (r.LocalId is not null && r.LocalId == rutaDia.ShiftEndLocalId)
            {
                return await chofer.RutaAsync(day, ct);
            }
            throw new DomainException($"Ya cerraste tu turno a las {Time(ended)}.");
        }
        if (rutaDia.ShiftStartedAt is not { } startedAt || rutaDia.StartKm is not { } startKm)
        {
            throw new DomainException("Primero inicia tu turno.");
        }

        var open = await db.Paradas.CountAsync(p => p.RutaDiaId == rutaDia.Id && p.Active
            && (p.State == ParadaState.Pendiente || p.State == ParadaState.EnCamino || p.State == ParadaState.EnSitio), ct);
        var warnings = ShiftRules.CheckEnd(r.Km, startKm, await settings.GetIntegerAsync(SettingKeys.UnidadMaxKmPerShift, ct), open);
        if (warnings.Count > 0 && !r.Confirm)
        {
            throw new ConfirmationRequiredException($"{string.Join(" ", warnings)} ¿Cerrar el turno?");
        }

        var at = chofer.ActionTime(r.At, day);
        rutaDia.ShiftEndedAt = at < startedAt ? startedAt : at;
        rutaDia.EndKm = r.Km;
        rutaDia.ShiftEndLocalId = r.LocalId;
        if (rutaDia.UnidadId is { } unidadId && await db.Unidades.FirstOrDefaultAsync(u => u.Id == unidadId, ct) is { } unidad)
        {
            UpdateKm(unidad, r.Km, at);
        }
        db.CortesCaja.Add(new CorteCaja
        {
            RutaDiaId = rutaDia.Id,
            OperadorId = operador.Id,
            Date = day,
            ChoferNotes = string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes.Trim(),
            Lines = r.Declared.Select(l => new CorteCajaLine { Method = l.Method, Declared = l.Amount }).ToList(),
        });
        await db.SaveChangesAsync(ct);
        return await chofer.RutaAsync(day, ct) with { Message = $"Turno cerrado: {ShiftRules.Km(r.Km - startKm)} km recorridos." };
    }

    /// <summary>
    /// A falla of the unidad of the day, with its gravedad and an optional photo (JPG, PNG or WebP). It goes to the
    /// unidad's bitácora as an open falla; the office sees it in Unidades and when assigning the unidad.
    /// </summary>
    public async Task<ChoferRutaDto> ReportFailureAsync(ReportFailureRequest r, Stream? photo, long length, CancellationToken ct)
    {
        await failureValidator.ValidateAndThrowAsync(r, ct);
        var day = chofer.ShiftDay(r.At);
        if (r.LocalId is not null && await db.UnidadEvents.AnyAsync(e => e.LocalId == r.LocalId, ct))
        {
            return await chofer.RutaAsync(day, ct);
        }
        var (rutaDia, operadorName) = await chofer.DayAsync(day, ct);
        var unidad = rutaDia?.UnidadId is { } unidadId
            ? await db.Unidades.FirstOrDefaultAsync(u => u.Id == unidadId && u.Active, ct)
            : null;
        if (unidad is null)
        {
            throw new DomainException("No tienes unidad asignada este día. Inicia tu turno con tu unidad o avisa a oficina.");
        }

        (string Path, string Type)? saved = null;
        if (photo is not null)
        {
            if (length <= 0 || length > MaxPhotoBytes)
            {
                throw new DomainException($"La foto debe pesar menos de {MaxPhotoBytes / 1024 / 1024} MB.");
            }
            using var buffer = new MemoryStream();
            await photo.CopyToAsync(buffer, ct);
            var extension = FileSignatures.Image(buffer.GetBuffer().AsSpan(0, (int)buffer.Length))
                ?? throw new DomainException("La foto debe ser una imagen JPG, PNG o WebP.");
            buffer.Position = 0;
            saved = (await files.SaveAsync(buffer, $"unidades/{unidad.Id}", extension, ct), FileSignatures.ContentType(extension));
        }

        var falla = new UnidadEvent
        {
            UnidadId = unidad.Id,
            Kind = UnidadEventKind.Falla,
            Date = day,
            Km = unidad.Km > 0 ? unidad.Km : null,
            Description = $"{r.Description.Trim()} (reportó {operadorName})",
            Severity = r.Severity,
            LocalId = r.LocalId,
        };
        db.UnidadEvents.Add(falla);
        await db.SaveChangesAsync(ct);
        if (saved is { } file)
        {
            db.UnidadDocuments.Add(new UnidadDocument
            {
                UnidadId = unidad.Id,
                Kind = UnidadDocumentKind.FotoFalla,
                ValidFrom = day,
                Description = "Foto de la falla",
                EventId = falla.Id,
                FilePath = file.Path,
                ContentType = file.Type,
            });
            await db.SaveChangesAsync(ct);
        }
        return await chofer.RutaAsync(day, ct) with { Message = $"Falla reportada en la unidad {unidad.Plate}; oficina la verá en Unidades." };
    }

    /// <summary>The unidad of the shift: always the one the office assigned to the ruta that day (Rutas).</summary>
    private async Task<Unidad> UnidadForStartAsync(RutaDia rutaDia, CancellationToken ct)
    {
        var unidad = rutaDia.UnidadId is { } assignedId
            ? await db.Unidades.FirstOrDefaultAsync(u => u.Id == assignedId && u.Active, ct)
            : null;
        return unidad ?? throw new DomainException(
            $"Oficina no te ha asignado unidad el {AgendaCalculator.Format(rutaDia.Date)}. Pide a Logística que te la asigne en Rutas.");
    }

    /// <summary>The unidad's km only goes up (an older reading that syncs late does not lower it).</summary>
    private static void UpdateKm(Unidad unidad, int km, DateTimeOffset at)
    {
        if (km > unidad.Km)
        {
            unidad.Km = km;
            unidad.KmUpdatedAt = at;
        }
    }

    private static string Time(DateTimeOffset at) => BusinessTime.TimeOfDay(at).ToString("H:mm");
}
