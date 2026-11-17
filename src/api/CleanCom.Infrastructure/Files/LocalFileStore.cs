using CleanCom.Application.Common;
using Microsoft.Extensions.Configuration;

namespace CleanCom.Infrastructure.Files;

/// <summary>
/// Files on the server's disk. The folder is Archivos:Ruta (absolute, or relative to the API's folder); by default
/// "archivos" next to the API. Names are generated, so nothing the client sends ends up in a path.
/// </summary>
internal sealed class LocalFileStore : IFileStore
{
    private readonly string root;

    public LocalFileStore(IConfiguration configuration)
    {
        var configured = configuration["Archivos:Ruta"];
        // The host puts its content root (the API folder) in the configuration.
        var contentRoot = configuration["contentRoot"] ?? Directory.GetCurrentDirectory();
        root = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(contentRoot, "archivos")
            : Path.IsPathRooted(configured) ? configured : Path.Combine(contentRoot, configured));
    }

    public async Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct)
    {
        var relative = Path.Combine(folder, $"{Guid.NewGuid():N}{extension}").Replace('\\', '/');
        var full = Resolve(relative) ?? throw new InvalidOperationException("Invalid file path.");
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var file = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, ct);
        return relative;
    }

    public Stream? OpenRead(string relativePath)
    {
        var full = Resolve(relativePath);
        return full is not null && File.Exists(full) ? File.OpenRead(full) : null;
    }

    /// <summary>The full path, only when it stays inside the root folder.</summary>
    private string? Resolve(string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(root, relativePath));
        return full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? full : null;
    }
}
