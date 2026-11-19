using CleanCom.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Chofer;

/// <summary>Files of the evidence for the office (Pedidos, Evidencias y caja).</summary>
public class EvidenciaService(ICleanComDbContext db, IFileStore files)
{
    public async Task<(Stream Content, string ContentType)> OpenAsync(int id, CancellationToken ct)
    {
        var evidencia = await db.Evidencias.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.Active, ct)
            ?? throw new NotFoundException("La evidencia no existe.");
        var stream = files.OpenRead(evidencia.FilePath)
            ?? throw new NotFoundException("El archivo de la evidencia no está en el servidor.");
        return (stream, evidencia.ContentType);
    }
}
