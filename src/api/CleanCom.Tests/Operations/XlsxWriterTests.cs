using System.IO.Compression;
using CleanCom.Application.Common;

namespace CleanCom.Tests.Operations;

/// <summary>The Excel export of the office (Evidencias y caja).</summary>
public class XlsxWriterTests
{
    [Fact]
    public void Columns_are_named_like_Excel()
    {
        Assert.Equal("A", XlsxWriter.Column(0));
        Assert.Equal("Z", XlsxWriter.Column(25));
        Assert.Equal("AA", XlsxWriter.Column(26));
        Assert.Equal("AQ", XlsxWriter.Column(42));
    }

    [Fact]
    public void Text_is_escaped_and_numbers_stay_numbers()
    {
        var bytes = XlsxWriter.Write("Visitas", ["Invitado", "Monto"], [["Pérez & <Hijos>", 350.5m], [null, 20]]);
        using var zip = new ZipArchive(new MemoryStream(bytes));
        Assert.NotNull(zip.GetEntry("xl/workbook.xml"));
        using var reader = new StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var sheet = reader.ReadToEnd();
        Assert.Contains("Pérez &amp; &lt;Hijos&gt;", sheet);
        Assert.Contains("<c r=\"B2\"><v>350.5</v></c>", sheet);
        Assert.Contains("<c r=\"B3\"><v>20</v></c>", sheet);
        Assert.DoesNotContain("r=\"A3\"", sheet);
    }
}
