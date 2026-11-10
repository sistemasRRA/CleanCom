using CleanCom.Api.Infraestructura;
using CleanCom.Aplicacion.Operacion;
using CleanCom.Dominio.Acceso;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>Valores configurables de Operación: cortes, cupos, metas, turnos, umbrales.</summary>
[ApiController]
[Route("configuracion")]
[Tags("Configuración")]
public class ConfiguracionController(IServicioConfiguracion servicio) : ControllerBase
{
    [HttpGet]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Ver)]
    public Task<IReadOnlyList<ConfiguracionDto>> Listar(CancellationToken ct) =>
        servicio.ListarAsync(ct);

    /// <summary>Cambia un valor desde hoy o desde una fecha futura.</summary>
    [HttpPut]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    public Task<HistorialConfiguracionDto> Cambiar(CambiarConfiguracionRequest request, CancellationToken ct) =>
        servicio.CambiarAsync(request, ct);

    [HttpGet("historial")]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Ver)]
    public Task<IReadOnlyList<HistorialConfiguracionDto>> Historial([FromQuery] string? clave, CancellationToken ct) =>
        servicio.HistorialAsync(clave, ct);
}
