using CleanCom.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Chofer;

/// <summary>Files of the evidence for the office (Pedidos, Evidence y caja).</summary>
public class EvidenceService(ICleanComDbContext db, IFileStore files)
{
    public async Task<(Stream Content, string ContentType)> OpenAsync(int id, CancellationToken ct)
    {
        var evidence = await db.Evidence.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.Active, ct)
            ?? throw new NotFoundException("La evidencia no existe.");
        var stream = files.OpenRead(evidence.FilePath)
            ?? throw new NotFoundException("El archivo de la evidencia no está en el servidor.");
        return (stream, evidence.ContentType);
    }
}
