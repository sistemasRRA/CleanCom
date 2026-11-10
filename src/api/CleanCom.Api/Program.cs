using System.Text.Json.Serialization;
using CleanCom.Api.Infraestructura;
using CleanCom.Aplicacion;
using CleanCom.Infraestructura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((servicios, log) => log
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(servicios)
        .Enrich.FromLogContext());

    builder.Services.AddAplicacion();
    builder.Services.AddInfraestructura(builder.Configuration);
    builder.Services.AddSeguridad();

    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<ManejadorErrores>();

    builder.Services
        .AddControllers(o =>
            // Las validaciones las hace FluentValidation con mensajes en español; sin esto MVC
            // marcaría como requeridos los string no anulables con mensajes en inglés.
            o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
        .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
        .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = _ =>
            // Solo llega aquí un cuerpo vacío o con JSON mal formado.
            new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Petición no válida.",
                Detail = "Revisa el formato de los datos enviados.",
            }));
    builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.Services.AddOpenApi(o => o.AddDocumentTransformer((documento, _, _) =>
    {
        documento.Info.Title = "CleanCom API";
        documento.Components ??= new OpenApiComponents();
        documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        documento.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token de acceso de POST /auth/login.",
        };
        // Swagger UI manda el token en todas las peticiones después de pulsar "Authorize".
        documento.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", documento)] = [] }];
        return Task.CompletedTask;
    }));

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi().AllowAnonymous();
        app.UseSwaggerUI(o =>
        {
            o.SwaggerEndpoint("/openapi/v1.json", "CleanCom API v1");
            o.DocumentTitle = "CleanCom API";
            o.EnablePersistAuthorization();
        });
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    await app.Services.SembrarAdministradorAsync(app.Configuration);

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "El API se detuvo por un error al iniciar");
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Punto de entrada; público para pruebas de integración.</summary>
public partial class Program;
