using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace CleanCom.Application.Common;

/// <summary>
/// A minimal Excel workbook (.xlsx) with one sheet: a bold header row and text or number cells. Enough for the
/// office's exports without a library; numbers stay numbers so Excel can add them up.
/// </summary>
public static class XlsxWriter
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>The workbook bytes. A cell is a string, a number (int, decimal, double) or null (empty).</summary>
    public static byte[] Write(string sheetName, IReadOnlyList<string> header, IEnumerable<IReadOnlyList<object?>> rows)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(zip, "[Content_Types].xml",
                """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/></Types>""");
            Add(zip, "_rels/.rels",
                """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
            Add(zip, "xl/workbook.xml",
                $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="{Escape(SheetName(sheetName))}" sheetId="1" r:id="rId1"/></sheets></workbook>""");
            Add(zip, "xl/_rels/workbook.xml.rels",
                """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>""");
            // Style 1 = bold (header).
            Add(zip, "xl/styles.xml",
                """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts><fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills><borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/></cellXfs></styleSheet>""");

            var sheet = new StringBuilder();
            sheet.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>""");
            AppendRow(sheet, 1, header.Cast<object?>().ToList(), bold: true);
            var r = 2;
            foreach (var row in rows)
            {
                AppendRow(sheet, r++, row, bold: false);
            }
            sheet.Append("</sheetData></worksheet>");
            Add(zip, "xl/worksheets/sheet1.xml", sheet.ToString());
        }
        return buffer.ToArray();
    }

    private static void AppendRow(StringBuilder sheet, int r, IReadOnlyList<object?> cells, bool bold)
    {
        sheet.Append("<row r=\"").Append(r).Append("\">");
        for (var c = 0; c < cells.Count; c++)
        {
            var reference = Column(c) + r.ToString(CultureInfo.InvariantCulture);
            var style = bold ? " s=\"1\"" : "";
            switch (cells[c])
            {
                case null:
                    break;
                case int or long or decimal or double or float:
                    var number = Convert.ToString(cells[c], CultureInfo.InvariantCulture);
                    sheet.Append($"<c r=\"{reference}\"{style}><v>{number}</v></c>");
                    break;
                default:
                    var text = Escape(Convert.ToString(cells[c], CultureInfo.InvariantCulture) ?? "");
                    sheet.Append($"<c r=\"{reference}\"{style} t=\"inlineStr\"><is><t xml:space=\"preserve\">{text}</t></is></c>");
                    break;
            }
        }
        sheet.Append("</row>");
    }

    /// <summary>0 → A, 25 → Z, 26 → AA.</summary>
    public static string Column(int index)
    {
        var name = "";
        for (var n = index + 1; n > 0; n = (n - 1) / 26)
        {
            name = (char)('A' + (n - 1) % 26) + name;
        }
        return name;
    }

    private static void Add(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    /// <summary>XML-escaped, without the control characters XML does not allow.</summary>
    private static string Escape(string text) =>
        SecurityElement.Escape(new string(text.Where(ch => ch is '\t' or '\n' or '\r' || ch >= ' ').ToArray()));

    /// <summary>Excel sheet names: at most 31 characters, without : \ / ? * [ ].</summary>
    private static string SheetName(string name)
    {
        var clean = new string(name.Where(ch => ch is not (':' or '\\' or '/' or '?' or '*' or '[' or ']')).ToArray());
        return clean.Length > 31 ? clean[..31] : clean;
    }
}
