using CleanCom.Aplicacion.Comun;
using CleanCom.Dominio.Comun;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Infraestructura;

/// <summary>
/// Convierte las excepciones conocidas en respuestas ProblemDetails con mensajes en español.
/// </summary>
public sealed class ManejadorErrores(IProblemDetailsService problemas) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken ct)
    {
        ProblemDetails? detalle = excepcion switch
        {
            ValidationException v => new HttpValidationProblemDetails(
                v.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Revisa los datos capturados.",
            },
            ExcepcionDominio => Problema(StatusCodes.Status400BadRequest, "No se puede realizar la operación.", excepcion.Message),
            ExcepcionNoEncontrado => Problema(StatusCodes.Status404NotFound, "No encontrado.", excepcion.Message),
            ExcepcionConflicto => Problema(StatusCodes.Status409Conflict, "Ya existe.", excepcion.Message),
            ExcepcionNoAutorizado => Problema(StatusCodes.Status401Unauthorized, "No autorizado.", excepcion.Message),
            BadHttpRequestException b => Problema(b.StatusCode, "Petición no válida.", "Revisa el formato de los datos enviados."),
            _ => null,
        };

        if (detalle is null)
        {
            return false;
        }

        contexto.Response.StatusCode = detalle.Status!.Value;
        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = detalle,
            Exception = excepcion,
        });
    }

    private static ProblemDetails Problema(int status, string titulo, string detalle) =>
        new() { Status = status, Title = titulo, Detail = detalle };
}
