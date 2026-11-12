using CleanCom.Domain.Common;

namespace CleanCom.Domain.People;

/// <summary>
/// Mexican phone numbers are stored as 10 digits, so "55 1234 5678", "+52 55 1234 5678" and
/// "5512345678" are the same invitado.
/// </summary>
public static class Phones
{
    public static string Normalize(string? phone)
    {
        var digits = new string((phone ?? "").Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 13 && digits.StartsWith("521", StringComparison.Ordinal))
        {
            digits = digits[3..];
        }
        else if (digits.Length == 12 && digits.StartsWith("52", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        if (digits.Length != 10)
        {
            throw new DomainException($"El teléfono \"{phone}\" debe tener 10 dígitos.");
        }
        return digits;
    }
}
