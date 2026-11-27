using CleanCom.Domain.Common;

namespace CleanCom.Domain.People;

/// <summary>The invitado's account in the app, as the CRM shows it.</summary>
public enum AppAccountState
{
    /// <summary>Never signed in and not invited.</summary>
    SinApp,
    /// <summary>The office sent the invitation; they have not signed in yet.</summary>
    Invitado,
    Activa,
    /// <summary>The office blocked the access; the app answers "tu cuenta está bloqueada".</summary>
    Bloqueada,
}

/// <summary>
/// The invitado's app account and duplicates in the CRM (docs/pantallas/oficina/clientes-crm.md, Ficha: mejora).
/// </summary>
public static class InvitadoAccount
{
    /// <summary>Length of the invitado's notes (observaciones).</summary>
    public const int MaxNotes = 1000;

    /// <param name="hasUser">They signed in at least once (the app created their user).</param>
    /// <param name="userActive">Their user is not blocked.</param>
    public static AppAccountState State(bool hasUser, bool userActive, DateTimeOffset? invitedAt) =>
        hasUser ? userActive ? AppAccountState.Activa : AppAccountState.Bloqueada
        : invitedAt is null ? AppAccountState.SinApp : AppAccountState.Invitado;

    /// <summary>The invitado's phones (WhatsApp and the other one), normalized, without the empty ones.</summary>
    public static IReadOnlySet<string> Phones(string whatsApp, string? other) =>
        new[] { whatsApp, other }.Select(TryNormalize).OfType<string>().ToHashSet();

    /// <summary>The phone normalized, or null when it is empty or not a valid phone.</summary>
    private static string? TryNormalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }
        try
        {
            return People.Phones.Normalize(phone);
        }
        catch (DomainException)
        {
            return null;
        }
    }

    /// <summary>Two invitados are possible duplicates when they share a phone (WhatsApp or the other one).</summary>
    public static bool SharePhone(Invitado a, Invitado b) =>
        Phones(a.WhatsAppPhone, a.OtherPhone).Overlaps(Phones(b.WhatsAppPhone, b.OtherPhone));

    /// <summary>
    /// While capturing an invitado: does <paramref name="other"/> already have one of the phones being captured, and
    /// does it stop the save? Only the same WhatsApp stops it (the WhatsApp is the app sign-in); sharing any other phone
    /// is a warning, because a family or an office can share a phone. Null when they share none.
    /// Example: capturing WhatsApp 5512345678 when Ana has it as WhatsApp → blocks; when Ana has it as her other phone
    /// → warning.
    /// </summary>
    public static bool? CaptureConflict(string whatsApp, string? otherPhone, Invitado other)
    {
        var captured = Phones(whatsApp, otherPhone);
        if (!captured.Overlaps(Phones(other.WhatsAppPhone, other.OtherPhone)))
        {
            return null;
        }
        return TryNormalize(whatsApp) is { } w && w == TryNormalize(other.WhatsAppPhone);
    }

    /// <summary>
    /// Throws with the reason, in Spanish, when <paramref name="duplicate"/> cannot be merged into <paramref name="main"/>.
    /// Both must be active and different; only one of them can have an app account (the sign-in is the phone).
    /// </summary>
    public static void EnsureCanMerge(Invitado main, Invitado duplicate)
    {
        if (main.Id == duplicate.Id)
        {
            throw new DomainException("Elige otro invitado para fusionar.");
        }
        if (!main.Active || !duplicate.Active)
        {
            throw new DomainException("Solo se fusionan invitados activos.");
        }
        if (main.UserId is not null && duplicate.UserId is not null)
        {
            throw new DomainException("Los dos tienen cuenta en la app; no se pueden fusionar. Bloquea la que no se usa y avisa al invitado.");
        }
    }

    /// <summary>
    /// The main invitado keeps their data and takes from the duplicate what they do not have: the other phone (the
    /// duplicate's WhatsApp when it is different), email, cómo nos conoció, campaign and the app account. The notes
    /// are joined. Example: main 5512345678 without email, duplicate 5587654321 with email → main keeps 5512345678 and
    /// gets 5587654321 as other phone and the email.
    /// </summary>
    public static void TakeMissing(Invitado main, Invitado duplicate)
    {
        var mainPhones = Phones(main.WhatsAppPhone, main.OtherPhone);
        if (string.IsNullOrWhiteSpace(main.OtherPhone))
        {
            main.OtherPhone = new[] { duplicate.WhatsAppPhone, duplicate.OtherPhone }
                .Select(TryNormalize)
                .FirstOrDefault(p => p is not null && !mainPhones.Contains(p));
        }
        main.Email ??= duplicate.Email;
        main.HowFoundUs ??= duplicate.HowFoundUs;
        main.Campaign ??= duplicate.Campaign;
        if (!string.IsNullOrWhiteSpace(duplicate.Notes))
        {
            var joined = string.IsNullOrWhiteSpace(main.Notes) ? duplicate.Notes : $"{main.Notes}\n{duplicate.Notes}";
            main.Notes = joined.Length > MaxNotes ? joined[..MaxNotes] : joined;
        }
        main.DoNotContact |= duplicate.DoNotContact;
        main.UserId ??= duplicate.UserId;
        duplicate.UserId = null;
        duplicate.Active = false;
        duplicate.Notes = $"Fusionado con el invitado {main.Id} ({main.Name}).";
    }
}
