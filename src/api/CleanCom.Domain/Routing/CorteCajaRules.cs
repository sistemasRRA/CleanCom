using CleanCom.Domain.Common;

namespace CleanCom.Domain.Routing;

/// <summary>A forma de pago of a corte de caja with its three amounts and differences.</summary>
/// <param name="Registered">Recorded by the chofer in the visits (reference payments).</param>
/// <param name="Declared">Declared by the chofer at the end of the shift.</param>
/// <param name="Pos">Official, from the POS; null when not compared.</param>
public readonly record struct CorteLine(PaymentMethod Method, decimal Registered, decimal Declared, decimal? Pos)
{
    /// <summary>Declared minus recorded in the visits: negative when money is missing.</summary>
    public decimal VsRegistered => Declared - Registered;

    /// <summary>Declared minus the POS: negative when money is missing; null when there is no POS amount.</summary>
    public decimal? VsPos => Pos is { } pos ? Declared - pos : null;

    public bool HasDifference => VsRegistered != 0 || VsPos is { } d && d != 0;
}

/// <summary>
/// Corte de caja (docs/03-reglas-de-negocio.md, Cobro): what the chofer declares is compared against what they
/// recorded in the visits and against the POS. The office closes it with a folio; with differences it has to write
/// why.
/// </summary>
public static class CorteCajaRules
{
    /// <summary>Every forma de pago, in order, with what each source says (a missing amount counts as 0).</summary>
    public static IReadOnlyList<CorteLine> Lines(
        IReadOnlyDictionary<PaymentMethod, decimal> registered,
        IReadOnlyDictionary<PaymentMethod, decimal> declared,
        IReadOnlyDictionary<PaymentMethod, decimal?> pos) =>
        Enum.GetValues<PaymentMethod>()
            .Select(m => new CorteLine(m, registered.GetValueOrDefault(m), declared.GetValueOrDefault(m), pos.GetValueOrDefault(m)))
            .ToList();

    /// <summary>"CC-20261126-F": the date and the ruta, unique because there is one corte per ruta and day.</summary>
    public static string Folio(DateOnly date, string rutaCode) => $"CC-{date:yyyyMMdd}-{rutaCode}";

    /// <summary>Throws with the reason, in Spanish, when the office cannot close the corte.</summary>
    public static void EnsureCanClose(bool closed, IReadOnlyList<CorteLine> lines, string? notes)
    {
        if (closed)
        {
            throw new DomainException("Este corte ya está cerrado.");
        }
        if (lines.Any(l => l.HasDifference) && string.IsNullOrWhiteSpace(notes))
        {
            throw new DomainException("Hay diferencias: anota qué pasó antes de cerrar el corte.");
        }
    }

    /// <summary>
    /// Reopening a closed corte (only with the permiso reabrir-cortes) needs a reason, which is kept with who did it.
    /// </summary>
    public static void EnsureCanReopen(bool closed, string? reason)
    {
        if (!closed)
        {
            throw new DomainException("Este corte no está cerrado.");
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Escribe por qué se reabre el corte.");
        }
    }
}
