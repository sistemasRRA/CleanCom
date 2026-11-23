using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Common;

/// <summary>Request limits per IP for endpoints anyone can call.</summary>
public static class RateLimits
{
    /// <summary>Sign-in codes of the invitado app: asking and checking, 10 a minute per IP.</summary>
    public const string AccessCodes = "AccessCodes";

    public static IServiceCollection AddRateLimits(this IServiceCollection services) =>
        services.AddRateLimiter(o =>
        {
            o.AddPolicy(AccessCodes, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.OnRejected = async (context, ct) => await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Demasiados intentos.",
                Detail = "Espera un minuto e intenta de nuevo.",
            }, ct);
        });
}
