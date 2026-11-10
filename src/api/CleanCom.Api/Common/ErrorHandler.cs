using CleanCom.Application.Common;
using CleanCom.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Common;

/// <summary>
/// Turns known exceptions into ProblemDetails responses. Titles and details reach the user, so they are in Spanish.
/// </summary>
public sealed class ErrorHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        ProblemDetails? details = exception switch
        {
            ValidationException v => new HttpValidationProblemDetails(
                v.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Revisa los datos capturados.",
            },
            DomainException => Problem(StatusCodes.Status400BadRequest, "No se puede realizar la operación.", exception.Message),
            NotFoundException => Problem(StatusCodes.Status404NotFound, "No encontrado.", exception.Message),
            ConflictException => Problem(StatusCodes.Status409Conflict, "Ya existe.", exception.Message),
            UnauthorizedException => Problem(StatusCodes.Status401Unauthorized, "No autorizado.", exception.Message),
            BadHttpRequestException b => Problem(b.StatusCode, "Petición no válida.", "Revisa el formato de los datos enviados."),
            _ => null,
        };

        if (details is null)
        {
            return false;
        }

        context.Response.StatusCode = details.Status!.Value;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = details,
            Exception = exception,
        });
    }

    private static ProblemDetails Problem(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };
}
