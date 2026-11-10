using CleanCom.Aplicacion.Comun;
using CleanCom.Dominio.Comun;
using CleanCom.Dominio.Operacion;
using CleanCom.Dominio.Tiempo;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Aplicacion.Operacion;

public class ServicioConfiguracion(
    ICleanComDbContext db,
    IRelojNegocio reloj,
    IValidator<CambiarConfiguracionRequest> validador) : IServicioConfiguracion
{
    public async Task<IReadOnlyList<ConfiguracionDto>> ListarAsync(CancellationToken ct)
    {
        var historial = await db.Configuraciones.AsNoTracking().ToListAsync(ct);
        var ahora = reloj.AhoraUtc;

        return CatalogoConfiguracion.Definiciones
            .Select(d =>
            {
                var vigente = ResolutorConfiguracion.Vigente(historial, d.Clave, ahora);
                var programado = ResolutorConfiguracion.Programado(historial, d.Clave, ahora);
                return new ConfiguracionDto(
                    d.Clave,
                    d.Descripcion,
                    d.Tipo,
                    d.PorRuta,
                    vigente?.Valor ?? d.ValorInicial,
                    vigente?.VigenteDesde ?? DateTimeOffset.MinValue,
                    programado is null ? null : new CambioProgramadoDto(programado.Valor, programado.VigenteDesde));
            })
            .ToList();
    }

    public async Task<IReadOnlyList<HistorialConfiguracionDto>> HistorialAsync(string? clave, CancellationToken ct)
    {
        var consulta = db.Configuraciones.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(clave))
        {
            consulta = consulta.Where(c => c.Clave == clave);
        }

        var registros = await consulta.ToListAsync(ct);
        return registros
            .OrderBy(c => c.Clave)
            .ThenByDescending(c => c.VigenteDesde)
            .ThenByDescending(c => c.Id)
            .Select(AHistorial)
            .ToList();
    }

    public async Task<HistorialConfiguracionDto> CambiarAsync(CambiarConfiguracionRequest request, CancellationToken ct)
    {
        await validador.ValidateAndThrowAsync(request, ct);
        var definicion = CatalogoConfiguracion.Buscar(request.Clave)!;
        var ahora = reloj.AhoraUtc;

        var vigenteDesde = request.VigenteDesde?.ToUniversalTime() ?? ahora;
        if (vigenteDesde < ahora.AddMinutes(-1))
        {
            throw new ExcepcionDominio("La fecha de vigencia no puede estar en el pasado.");
        }

        var registro = new Configuracion
        {
            Clave = definicion.Clave,
            Tipo = definicion.Tipo,
            Valor = ValorConfiguracion.Normalizar(definicion.Tipo, request.Valor),
            VigenteDesde = vigenteDesde < ahora ? ahora : vigenteDesde,
            Nota = string.IsNullOrWhiteSpace(request.Nota) ? null : request.Nota.Trim(),
        };
        db.Configuraciones.Add(registro);
        await db.SaveChangesAsync(ct);
        return AHistorial(registro);
    }

    public async Task<string> ObtenerAsync(string clave, int? rutaId, CancellationToken ct)
    {
        var historial = await db.Configuraciones.AsNoTracking().Where(c => c.Clave == clave).ToListAsync(ct);
        var ruta = rutaId is null ? null : await db.Rutas.AsNoTracking().FirstOrDefaultAsync(r => r.Id == rutaId, ct)
            ?? throw new ExcepcionNoEncontrado($"La ruta {rutaId} no existe.");
        return ResolutorConfiguracion.Resolver(historial, clave, reloj.AhoraUtc, ruta);
    }

    private static HistorialConfiguracionDto AHistorial(Configuracion c) =>
        new(c.Id, c.Clave, c.Valor, c.VigenteDesde, c.CreadoEn, c.CreadoPor, c.Nota);
}
