namespace CleanCom.Application.Common;

/// <summary>
/// Recognizes an uploaded file by its first bytes, not by what the client says it is.
/// </summary>
public static class FileSignatures
{
    /// <summary>".jpg", ".png" or ".webp"; null when it is not one of those images.</summary>
    public static string? Image(ReadOnlySpan<byte> b)
    {
        if (b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
        {
            return ".jpg";
        }
        if (b.Length > 8 && b[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return ".png";
        }
        if (b.Length > 12 && b[..4].SequenceEqual("RIFF"u8) && b[8..12].SequenceEqual("WEBP"u8))
        {
            return ".webp";
        }
        return null;
    }

    /// <summary>An image or a PDF ("%PDF-").</summary>
    public static string? ImageOrPdf(ReadOnlySpan<byte> b) =>
        Image(b) ?? (b.Length > 5 && b[..5].SequenceEqual("%PDF-"u8) ? ".pdf" : null);

    public static string ContentType(string extension) => extension switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".pdf" => "application/pdf",
        _ => "image/jpeg",
    };
}
