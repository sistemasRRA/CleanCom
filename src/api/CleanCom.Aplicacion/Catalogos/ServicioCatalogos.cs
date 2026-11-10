using CleanCom.Aplicacion.Comun;
using CleanCom.Dominio.Catalogos;
using CleanCom.Dominio.Geografia;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Aplicacion.Catalogos;

/// <summary>
/// Catálogos de Operación. Las bajas son lógicas salvo en días inhábiles y cobertura, que se quitan.
/// </summary>
public class ServicioCatalogos(ICleanComDbContext db, IServiceProvider validadores)
{
    private Task Validar<T>(T request, CancellationToken ct) =>
        ((IValidator<T>)validadores.GetService(typeof(IValidator<T>))!).ValidateAndThrowAsync(request, ct);

    // Rutas

    public async Task<IReadOnlyList<RutaDto>> ListarRutasAsync(CancellationToken ct) =>
        await db.Rutas.AsNoTracking().OrderBy(r => r.Codigo).Select(r => ARutaDto(r)).ToListAsync(ct);

    public async Task<RutaDto> CrearRutaAsync(GuardarRutaRequest r, CancellationToken ct)
    {
        await Validar(r, ct);
        if (await db.Rutas.AnyAsync(x => x.Codigo == r.Codigo, ct))
        {
            throw new ExcepcionConflicto($"Ya existe la ruta {r.Codigo}.");
        }
        var ruta = new Ruta();
        AplicarRuta(ruta, r);
        db.Rutas.Add(ruta);
        await db.SaveChangesAsync(ct);
        return ARutaDto(ruta);
    }

    public async Task<RutaDto> ActualizarRutaAsync(int id, GuardarRutaRequest r, CancellationToken ct)
    {
        await Validar(r, ct);
        var ruta = await db.Rutas.FindAsync([id], ct) ?? throw new ExcepcionNoEncontrado($"La ruta {id} no existe.");
        if (await db.Rutas.AnyAsync(x => x.Codigo == r.Codigo && x.Id != id, ct))
        {
            throw new ExcepcionConflicto($"Ya existe la ruta {r.Codigo}.");
        }
        AplicarRuta(ruta, r);
        await db.SaveChangesAsync(ct);
        return ARutaDto(ruta);
    }

    private static void AplicarRuta(Ruta ruta, GuardarRutaRequest r)
    {
        ruta.Codigo = r.Codigo;
        ruta.Nombre = r.Nombre.Trim();
        ruta.Color = r.Color.ToUpperInvariant();
        ruta.CupoParadas = r.CupoParadas;
        ruta.MetaRecolecciones = r.MetaRecolecciones;
        ruta.RegresoMaximo = r.RegresoMaximo;
        ruta.RegresoMaximoSabado = r.RegresoMaximoSabado;
        ruta.Activo = r.Activo;
    }

    private static RutaDto ARutaDto(Ruta r) =>
        new(r.Id, r.Codigo, r.Nombre, r.Color, r.CupoParadas, r.MetaRecolecciones, r.RegresoMaximo, r.RegresoMaximoSabado, r.Activo);

    // Lugares

    public async Task<IReadOnlyList<LugarDto>> ListarLugaresAsync(bool incluirInactivos, CancellationToken ct)
    {
        var lugares = await db.Lugares.AsNoTracking()
            .Where(l => incluirInactivos || l.Activo)
            .OrderBy(l => l.Tipo).ThenBy(l => l.Nombre)
            .ToListAsync(ct);
        return lugares.Select(ALugarDto).ToList();
    }

    public async Task<LugarDto> CrearLugarAsync(GuardarLugarRequest r, CancellationToken ct)
    {
        await Validar(r, ct);
        var lugar = new Lugar();
        AplicarLugar(lugar, r);
        db.Lugares.Add(lugar);
        await db.SaveChangesAsync(ct);
        return ALugarDto(lugar);
    }

    public async Task<LugarDto> ActualizarLugarAsync(int id, GuardarLugarRequest r, CancellationToken ct)
    {
        await Validar(r, ct);
        var lugar = await db.Lugares.FindAsync([id], ct) ?? throw new ExcepcionNoEncontrado($"El lugar {id} no existe.");
        AplicarLugar(lugar, r);
        await db.SaveChangesAsync(ct);
        return ALugarDto(lugar);
    }

    public async Task DesactivarLugarAsync(int id, CancellationToken ct)
    {
        var lugar = await db.Lugares.FindAsync([id], ct) ?? throw new ExcepcionNoEncontrado($"El lugar {id} no existe.");
        lugar.Activo = false;
        await db.SaveChangesAsync(ct);
    }

    private static void AplicarLugar(Lugar lugar, GuardarLugarRequest r)
    {
        lugar.Nombre = r.Nombre.Trim();
        lugar.Tipo = r.Tipo;
        lugar.Direccion = r.Direccion.Trim();
        lugar.Ubicacion = r.Lat is { } lat && r.Lng is { } lng ? Geo.Punto(lat, lng) : null;
        lugar.Horario = string.IsNullOrWhiteSpace(r.Horario) ? null : r.Horario.Trim();
        lugar.Activo = r.Activo;
    }

    private static LugarDto ALugarDto(Lugar l) =>
        new(l.Id, l.Nombre, l.Tipo, l.Direccion, l.Ubicacion?.Y, l.Ubicacion?.X, l.Horario, l.Activo);

    // Tipos de parada

    public async Task<IReadOnlyList<TipoParadaDto>> ListarTiposParadaAsync(CancellationToken ct) =>
        await db.TiposParada.AsNoTracking().OrderBy(t => t.Nombre)
            .Select(t => new TipoParadaDto(t.Id, t.Nombre, t.CamposRequeridos, t.Activo)).ToListAsync(ct);

    public async Task<TipoParadaDto> GuardarTipoParadaAsync(int? id, GuardarTipoParadaRequest r, CancellationToken ct)
    {
        await Validar(r, ct);
        var tipo = id is null ? new TipoParada() : await db.TiposParada.FindAsync([id], ct) ?? throw new ExcepcionNoEncontrado($"El tipo de parada {id} no existe.");
        if (await db.TiposParada.AnyAsync(x => x.Nombre == r.Nombre.Trim() && x.Id != tipo.Id, ct))
        {
            throw new ExcepcionConflicto($"Ya existe el tipo de parada {r.Nombre}.");
        }
        tipo.Nombre = r.Nombre.Trim();
        tipo.CamposRequeridos = string.IsNullOrWhiteSpace(r.CamposRequeridos) ? null : r.CamposRequeridos.Trim();
        tipo.Activo = r.Activo;
        if (id is null)
        {
            db.TiposParada.Add(tipo);
        }
        await db.SaveChangesAsync(ct);
        return new TipoParadaDto(tipo.Id, tipo.Nombre, tipo.CamposRequeridos, tipo.Activo);
    }

    // Líneas de servicio

    public async Task<IReadOnlyList<LineaServicioDto>> ListarLineasServicioAsync(CancellationToken ct) =>
        await db.LineasServicio.AsNoTracking().OrderBy(l => l.Nombre)
            .Select(l => new LineaServicioDto(l.Id, l.Nombre, l.DiasProceso, l.Activo)).ToListAsync(ct);

    public async Task<LineaServicioDto> GuardarLineaServicioAsync(int? id, GuardarLineaServicioRequest r, CancellationToken ct)
    {
        await Validar(r, ct);
        var linea = id is null ? new LineaServicio() : await db.LineasServicio.FindAsync([id], ct) ?? throw new ExcepcionNoEncontrado($"La línea de servicio {id} no existe.");
        if (await db.LineasServicio.AnyAsync(x => x.Nombre == r.Nombre.Trim() && x.Id != linea.Id, ct))
        {
            throw new ExcepcionConflicto($"Ya existe la línea de servicio {r.Nombre}.");
        }
        linea.Nombre = r.Nombre.Trim();
        linea.DiasProceso = r.DiasProceso;
        linea.Activo = r.Activo;
        if (id is null)
        {
            db.LineasServicio.Add(linea);
        }
        await db.SaveChangesAsync(ct);
        return new LineaServicioDto(linea.Id, linea.Nombre, linea.DiasProceso, linea.Activo);
    }

    // Motivos

    public async Task<IReadOnlyList<MotivoDto>> ListarMotivosAsync(TipoMotivo? tipo, CancellationToken ct) =>
        await db.Motivos.AsNoTracking()
            .Where(m => tipo == null || m.Tipo == tipo)
            .OrderBy(m => m.Tipo).ThenBy(m => m.Texto)
            .Select(m => new MotivoDto(m.Id, m.Tipo, m.Texto, m.Activo)).ToListAsync(ct);

    public async Task<MotivoDto> GuardarMotivoAsync(int? id, GuardarMotivoRequest r, CancellationToken ct)
    {
        await Validar(r, ct);
        var motivo = id is null ? new MotivoCatalogo() : await db.Motivos.FindAsync([id], ct) ?? throw new ExcepcionNoEncontrado($"El motivo {id} no existe.");
        motivo.Tipo = r.Tipo;
        motivo.Texto = r.Texto.Trim();
        motivo.Activo = r.Activo;
        if (id is null)
        {
            db.Motivos.Add(motivo);
        }
        await db.SaveChangesAsync(ct);
        return new MotivoDto(motivo.Id, motivo.Tipo, motivo.Texto, motivo.Activo);
    }

    // Días inhábiles

    public async Task<IReadOnlyList<DiaInhabilDto>> ListarDiasInhabilesAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct) =>
        await db.DiasInhabiles.AsNoTracking()
            .Where(d => (desde == null || d.Fecha >= desde) && (hasta == null || d.Fecha <= hasta))
            .OrderBy(d => d.Fecha)
            .Select(d => new DiaInhabilDto(d.Id, d.Fecha, d.Motivo)).ToListAsync(ct);

    public async Task<DiaInhabilDto> GuardarDiaInhabilAsync(int? id, GuardarDiaInhabilRequest r, CancellationToken ct)
    {
        await Validar(r, ct);
        var dia = id is null ? new DiaInhabil() : await db.DiasInhabiles.FindAsync([id], ct) ?? throw new ExcepcionNoEncontrado($"El día inhábil {id} no existe.");
        if (await db.DiasInhabiles.AnyAsync(x => x.Fecha == r.Fecha && x.Id != dia.Id, ct))
        {
            throw new ExcepcionConflicto($"El {r.Fecha:yyyy-MM-dd} ya está marcado como inhábil.");
        }
        dia.Fecha = r.Fecha;
        dia.Motivo = r.Motivo.Trim();
        if (id is null)
        {
            db.DiasInhabiles.Add(dia);
        }
        await db.SaveChangesAsync(ct);
        return new DiaInhabilDto(dia.Id, dia.Fecha, dia.Motivo);
    }

    public async Task EliminarDiaInhabilAsync(int id, CancellationToken ct)
    {
        var dia = await db.DiasInhabiles.FindAsync([id], ct) ?? throw new ExcepcionNoEncontrado($"El día inhábil {id} no existe.");
        db.DiasInhabiles.Remove(dia);
        await db.SaveChangesAsync(ct);
    }

    // Cobertura

    public async Task<IReadOnlyList<CoberturaDto>> ListarCoberturaAsync(string? buscar, int? zonaId, CancellationToken ct)
    {
        var consulta = db.Coberturas.AsNoTracking();
        if (zonaId is not null)
        {
            consulta = consulta.Where(c => c.ZonaId == zonaId);
        }
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            consulta = consulta.Where(c => c.CodigoPostal.StartsWith(texto) || c.Colonia.Contains(texto) || c.Alcaldia.Contains(texto));
        }
        return await consulta
            .OrderBy(c => c.CodigoPostal).ThenBy(c => c.Colonia)
            .Select(c => new CoberturaDto(c.Id, c.CodigoPostal, c.Colonia, c.Alcaldia, c.ZonaId, c.Zona!.Codigo))
            .ToListAsync(ct);
    }

    public async Task<CoberturaDto> GuardarCoberturaAsync(int? id, GuardarCoberturaRequest r, CancellationToken ct)
    {
        await Validar(r, ct);
        var zona = await db.Zonas.AsNoTracking().FirstOrDefaultAsync(z => z.Id == r.ZonaId, ct)
            ?? throw new ExcepcionNoEncontrado($"La zona {r.ZonaId} no existe.");
        var cobertura = id is null ? new CoberturaCP() : await db.Coberturas.FindAsync([id], ct) ?? throw new ExcepcionNoEncontrado($"El registro de cobertura {id} no existe.");
        var colonia = r.Colonia.Trim();
        if (await db.Coberturas.AnyAsync(x => x.CodigoPostal == r.CodigoPostal && x.Colonia == colonia && x.Id != cobertura.Id, ct))
        {
            throw new ExcepcionConflicto($"La colonia {colonia} ({r.CodigoPostal}) ya está en la cobertura.");
        }
        cobertura.CodigoPostal = r.CodigoPostal;
        cobertura.Colonia = colonia;
        cobertura.Alcaldia = r.Alcaldia.Trim();
        cobertura.ZonaId = r.ZonaId;
        if (id is null)
        {
            db.Coberturas.Add(cobertura);
        }
        await db.SaveChangesAsync(ct);
        return new CoberturaDto(cobertura.Id, cobertura.CodigoPostal, cobertura.Colonia, cobertura.Alcaldia, zona.Id, zona.Codigo);
    }

    public async Task EliminarCoberturaAsync(int id, CancellationToken ct)
    {
        var cobertura = await db.Coberturas.FindAsync([id], ct) ?? throw new ExcepcionNoEncontrado($"El registro de cobertura {id} no existe.");
        db.Coberturas.Remove(cobertura);
        await db.SaveChangesAsync(ct);
    }
}
