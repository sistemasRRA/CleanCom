using CleanCom.Aplicacion.Comun;
using CleanCom.Dominio.Catalogos;
using CleanCom.Dominio.Comun;
using CleanCom.Dominio.Geografia;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Aplicacion.Catalogos;

public class ServicioZonas(
    ICleanComDbContext db,
    IValidator<GuardarZonaRequest> validadorZona,
    IValidator<DiasVisitaRequest> validadorDias)
{
    public async Task<IReadOnlyList<ZonaDto>> ListarAsync(int? rutaId, bool incluirInactivas, CancellationToken ct)
    {
        var zonas = await Consulta()
            .Where(z => (rutaId == null || z.RutaId == rutaId) && (incluirInactivas || z.Activo))
            .OrderBy(z => z.Ruta!.Codigo).ThenBy(z => z.Codigo)
            .ToListAsync(ct);
        return zonas.Select(ADto).ToList();
    }

    public async Task<ZonaDto> ObtenerAsync(int id, CancellationToken ct) => ADto(await Cargar(id, ct));

    public async Task<ZonaDto> CrearAsync(GuardarZonaRequest r, CancellationToken ct)
    {
        await validadorZona.ValidateAndThrowAsync(r, ct);
        var zona = new Zona();
        await Aplicar(zona, r, ct);
        db.Zonas.Add(zona);
        await db.SaveChangesAsync(ct);
        return await ObtenerAsync(zona.Id, ct);
    }

    public async Task<ZonaDto> ActualizarAsync(int id, GuardarZonaRequest r, CancellationToken ct)
    {
        await validadorZona.ValidateAndThrowAsync(r, ct);
        var zona = await db.Zonas.FindAsync([id], ct) ?? throw new ExcepcionNoEncontrado($"La zona {id} no existe.");

        if (r.ZonaNucleoId is not null && !zona.EsExtension
            && await db.Zonas.AnyAsync(z => z.ZonaNucleoId == id, ct))
        {
            throw new ExcepcionDominio($"La zona {zona.Codigo} es núcleo de otras zonas EXT; no puede volverse EXT.");
        }

        await Aplicar(zona, r, ct);
        await db.SaveChangesAsync(ct);
        return await ObtenerAsync(id, ct);
    }

    public async Task DesactivarAsync(int id, CancellationToken ct)
    {
        var zona = await db.Zonas.FindAsync([id], ct) ?? throw new ExcepcionNoEncontrado($"La zona {id} no existe.");
        zona.Activo = false;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Reemplaza los días de visita. Las zonas EXT no tienen días propios: siguen a su zona núcleo.
    /// </summary>
    public async Task<ZonaDto> CambiarDiasAsync(int id, DiasVisitaRequest r, CancellationToken ct)
    {
        await validadorDias.ValidateAndThrowAsync(r, ct);
        var zona = await db.Zonas.Include(z => z.DiasVisita).FirstOrDefaultAsync(z => z.Id == id, ct)
            ?? throw new ExcepcionNoEncontrado($"La zona {id} no existe.");
        if (zona.EsExtension)
        {
            throw new ExcepcionDominio($"La zona {zona.Codigo} es EXT y sigue los días de su zona núcleo; cambia los días del núcleo.");
        }

        var nuevos = r.Dias.Distinct().ToHashSet();
        db.ZonasDiasVisita.RemoveRange(zona.DiasVisita.Where(d => !nuevos.Contains(d.DiaSemana)));
        foreach (var dia in nuevos.Where(n => zona.DiasVisita.All(d => d.DiaSemana != n)))
        {
            zona.DiasVisita.Add(new ZonaDiaVisita { DiaSemana = dia });
        }
        await db.SaveChangesAsync(ct);
        return await ObtenerAsync(id, ct);
    }

    private async Task Aplicar(Zona zona, GuardarZonaRequest r, CancellationToken ct)
    {
        if (!await db.Rutas.AnyAsync(x => x.Id == r.RutaId, ct))
        {
            throw new ExcepcionNoEncontrado($"La ruta {r.RutaId} no existe.");
        }
        if (await db.Zonas.AnyAsync(x => x.Codigo == r.Codigo && x.Id != zona.Id, ct))
        {
            throw new ExcepcionConflicto($"Ya existe la zona {r.Codigo}.");
        }

        zona.Codigo = r.Codigo;
        zona.RutaId = r.RutaId;
        zona.Activo = r.Activo;
        zona.Poligono = r.Poligono is { Count: > 0 } ? Geo.Poligono(r.Poligono) : null;

        if (r.ZonaNucleoId is { } nucleoId)
        {
            var nucleo = await db.Zonas.AsNoTracking().FirstOrDefaultAsync(z => z.Id == nucleoId, ct)
                ?? throw new ExcepcionNoEncontrado($"La zona núcleo {nucleoId} no existe.");
            zona.ValidarNucleo(nucleo);
        }
        zona.ZonaNucleoId = r.ZonaNucleoId;
    }

    private IQueryable<Zona> Consulta() =>
        db.Zonas.AsNoTracking()
            .Include(z => z.Ruta)
            .Include(z => z.DiasVisita)
            .Include(z => z.ZonaNucleo).ThenInclude(n => n!.DiasVisita);

    private async Task<Zona> Cargar(int id, CancellationToken ct) =>
        await Consulta().FirstOrDefaultAsync(z => z.Id == id, ct) ?? throw new ExcepcionNoEncontrado($"La zona {id} no existe.");

    private static ZonaDto ADto(Zona z) => new(
        z.Id,
        z.Codigo,
        z.RutaId,
        z.Ruta?.Codigo ?? "",
        z.ZonaNucleoId,
        z.ZonaNucleo?.Codigo,
        z.Activo,
        z.DiasVisita.Select(d => d.DiaSemana).Order().ToList(),
        z.DiasDeVisitaEfectivos(),
        z.Poligono is null ? null : Geo.Vertices(z.Poligono));
}
