using System.Text;
using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;

namespace CleanCom.Tests.Catalogs;

/// <summary>
/// Format of the official postal code export. The rows here are made up: the real catalog may not be distributed.
/// </summary>
public class SepomexFileTests
{
    private const string Header =
        "d_codigo|d_asenta|d_tipo_asenta|D_mnpio|d_estado|d_ciudad|d_CP|c_estado|c_oficina|c_tipo_asenta|c_mnpio|id_asenta_cpcons|d_zona|c_cve_ciudad|c_CP";

    private static Stream File(params string[] lines) =>
        new MemoryStream(Encoding.Latin1.GetBytes(string.Join("\r\n", lines)));

    [Fact]
    public void Reads_settlements_after_the_terms_line_and_the_header()
    {
        var rows = SepomexFile.Read(File(
            "Términos de uso del catálogo.",
            Header,
            "09990|Colonia Ejemplo|Colonia|Alcaldía Ejemplo|Ciudad de México|Ciudad de México|09991|09|09991|09|099|0001|Urbano|01|",
            "09990|Barrio Ejemplo|Barrio|Alcaldía Ejemplo|Ciudad de México|Ciudad de México|09991|09|09991|02|099|0002|Urbano|01|"));

        Assert.Equal(2, rows.Count);
        var first = rows[0];
        Assert.Equal("09990", first.Code);
        Assert.Equal("Colonia Ejemplo", first.Settlement);
        Assert.Equal("Colonia", first.SettlementType);
        Assert.Equal("Alcaldía Ejemplo", first.Borough);
        Assert.Equal("Ciudad de México", first.City);
        Assert.Equal("09", first.StateCode);
        Assert.Equal("099", first.BoroughCode);
        Assert.Equal("Urbano", first.AreaType);
        Assert.Equal("09|099|0001", SepomexFile.Key(first));
        Assert.Equal("Barrio", rows[1].SettlementType);
    }

    [Fact]
    public void Accents_are_read_from_ISO_8859_1()
    {
        var rows = SepomexFile.Read(File(Header,
            "09990|Ampliación Peñón|Colonia|Álvaro Ejemplo|Ciudad de México||09991|09|09991|09|099|0003|Urbano||"));
        Assert.Equal("Ampliación Peñón", rows[0].Settlement);
        Assert.Equal("Álvaro Ejemplo", rows[0].Borough);
        Assert.Null(rows[0].City);
    }

    [Fact]
    public void A_file_without_the_header_is_rejected()
    {
        Assert.Throws<DomainException>(() => SepomexFile.Read(File("cp,colonia", "09990,Colonia Ejemplo")));
    }

    [Fact]
    public void A_header_missing_columns_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() => SepomexFile.Read(File("d_codigo|d_asenta", "09990|Colonia Ejemplo")));
        Assert.Contains("d_tipo_asenta", ex.Message);
    }

    [Fact]
    public void A_wrong_postal_code_says_which_line()
    {
        var ex = Assert.Throws<DomainException>(() => SepomexFile.Read(File(Header,
            "9990|Colonia Ejemplo|Colonia|Alcaldía Ejemplo|Ciudad de México||09991|09|09991|09|099|0001|Urbano||")));
        Assert.Contains("Línea 2", ex.Message);
    }
}
