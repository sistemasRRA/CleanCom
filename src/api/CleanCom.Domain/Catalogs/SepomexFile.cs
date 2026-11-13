using System.Text;
using CleanCom.Domain.Common;

namespace CleanCom.Domain.Catalogs;

/// <summary>
/// Reads the TXT export of the official postal code catalog (correosdemexico.gob.mx › Consulta CP › Exportar):
/// ISO-8859-1, a terms-of-use line, a header line and one settlement per line separated by "|".
/// Works for one state or for the whole country.
/// </summary>
public static class SepomexFile
{
    private static readonly string[] Required =
        ["d_codigo", "d_asenta", "d_tipo_asenta", "D_mnpio", "d_estado", "d_ciudad", "c_estado", "c_mnpio", "id_asenta_cpcons", "d_zona"];

    public static IReadOnlyList<PostalCode> Read(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.Latin1);
        Dictionary<string, int>? columns = null;
        var rows = new List<PostalCode>();

        var lineNumber = 0;
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            var fields = line.Split('|');
            if (columns is null)
            {
                // The terms-of-use line comes before the header.
                if (!fields.Contains("d_codigo"))
                {
                    continue;
                }
                columns = fields.Select((name, i) => (name: name.Trim(), i)).ToDictionary(x => x.name, x => x.i, StringComparer.Ordinal);
                var missing = Required.Where(r => !columns.ContainsKey(r)).ToList();
                if (missing.Count > 0)
                {
                    throw new DomainException($"El archivo no es el catálogo de Correos de México: faltan las columnas {string.Join(", ", missing)}.");
                }
                continue;
            }

            string Get(string name) => columns[name] < fields.Length ? fields[columns[name]].Trim() : "";
            var code = Get("d_codigo");
            if (code.Length != 5 || !code.All(char.IsAsciiDigit))
            {
                throw new DomainException($"Línea {lineNumber}: el código postal \"{code}\" no es válido.");
            }
            rows.Add(new PostalCode
            {
                Code = code,
                Settlement = Get("d_asenta"),
                SettlementType = Get("d_tipo_asenta"),
                Borough = Get("D_mnpio"),
                City = Get("d_ciudad") is { Length: > 0 } city ? city : null,
                State = Get("d_estado"),
                StateCode = Get("c_estado"),
                BoroughCode = Get("c_mnpio"),
                SettlementId = Get("id_asenta_cpcons"),
                AreaType = Get("d_zona") is { Length: > 0 } area ? area : null,
            });
        }

        if (columns is null)
        {
            throw new DomainException("El archivo no es el catálogo de Correos de México: no tiene la línea de encabezados.");
        }
        if (rows.Count == 0)
        {
            throw new DomainException("El archivo no trae códigos postales.");
        }
        return rows;
    }

    /// <summary>Identifies a settlement across imports.</summary>
    public static string Key(PostalCode p) => $"{p.StateCode}|{p.BoroughCode}|{p.SettlementId}";
}
