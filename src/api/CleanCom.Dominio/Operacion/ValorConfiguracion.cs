using System.Globalization;
using CleanCom.Dominio.Comun;

namespace CleanCom.Dominio.Operacion;

public readonly record struct RangoHoras(TimeOnly Inicio, TimeOnly Fin);

/// <summary>
/// Valida, normaliza y lee los valores de configuración según su tipo.
/// </summary>
public static class ValorConfiguracion
{
    private const string FormatoHora = "HH:mm";
    private static readonly CultureInfo Invariante = CultureInfo.InvariantCulture;

    /// <summary>
    /// Devuelve el valor en su forma canónica o lanza <see cref="ExcepcionDominio"/> con el motivo.
    /// </summary>
    public static string Normalizar(TipoValorConfiguracion tipo, string? valor)
    {
        var texto = valor?.Trim() ?? "";
        if (texto.Length == 0)
        {
            throw new ExcepcionDominio("El valor no puede ir vacío.");
        }

        return tipo switch
        {
            TipoValorConfiguracion.Texto => texto,
            TipoValorConfiguracion.Entero => ComoEntero(texto).ToString(Invariante),
            TipoValorConfiguracion.Decimal => ComoDecimal(texto).ToString(Invariante),
            TipoValorConfiguracion.Booleano => ComoBooleano(texto) ? "true" : "false",
            TipoValorConfiguracion.Hora => ComoHora(texto).ToString(FormatoHora, Invariante),
            TipoValorConfiguracion.RangoHoras => FormatearRango(ComoRangoHoras(texto)),
            TipoValorConfiguracion.Coordenada => FormatearCoordenada(ComoCoordenada(texto)),
            _ => throw new ExcepcionDominio($"Tipo de valor no soportado: {tipo}."),
        };
    }

    public static int ComoEntero(string valor)
    {
        if (!int.TryParse(valor, NumberStyles.Integer, Invariante, out var n) || n < 0)
        {
            throw new ExcepcionDominio($"\"{valor}\" no es un número entero válido.");
        }
        return n;
    }

    public static decimal ComoDecimal(string valor)
    {
        if (!decimal.TryParse(valor, NumberStyles.Number, Invariante, out var n) || n < 0)
        {
            throw new ExcepcionDominio($"\"{valor}\" no es un número válido.");
        }
        return n;
    }

    public static bool ComoBooleano(string valor) => valor.Trim().ToLowerInvariant() switch
    {
        "true" or "si" or "sí" or "1" => true,
        "false" or "no" or "0" => false,
        _ => throw new ExcepcionDominio($"\"{valor}\" no es sí o no."),
    };

    public static TimeOnly ComoHora(string valor)
    {
        if (!TimeOnly.TryParseExact(valor.Trim(), ["H:mm", "HH:mm"], Invariante, DateTimeStyles.None, out var hora))
        {
            throw new ExcepcionDominio($"\"{valor}\" no es una hora válida (HH:mm).");
        }
        return hora;
    }

    public static RangoHoras ComoRangoHoras(string valor)
    {
        var partes = valor.Split('-', StringSplitOptions.TrimEntries);
        if (partes.Length != 2)
        {
            throw new ExcepcionDominio($"\"{valor}\" no es un rango válido (HH:mm-HH:mm).");
        }
        var rango = new RangoHoras(ComoHora(partes[0]), ComoHora(partes[1]));
        if (rango.Inicio >= rango.Fin)
        {
            throw new ExcepcionDominio("La hora de inicio debe ser anterior a la hora de fin.");
        }
        return rango;
    }

    public static (double Lat, double Lng) ComoCoordenada(string valor)
    {
        var partes = valor.Split(',', StringSplitOptions.TrimEntries);
        if (partes.Length != 2
            || !double.TryParse(partes[0], NumberStyles.Float, Invariante, out var lat)
            || !double.TryParse(partes[1], NumberStyles.Float, Invariante, out var lng)
            || lat is < -90 or > 90
            || lng is < -180 or > 180)
        {
            throw new ExcepcionDominio($"\"{valor}\" no es una coordenada válida (latitud,longitud).");
        }
        return (lat, lng);
    }

    private static string FormatearRango(RangoHoras r) =>
        $"{r.Inicio.ToString(FormatoHora, Invariante)}-{r.Fin.ToString(FormatoHora, Invariante)}";

    private static string FormatearCoordenada((double Lat, double Lng) c) =>
        $"{c.Lat.ToString(Invariante)},{c.Lng.ToString(Invariante)}";
}
