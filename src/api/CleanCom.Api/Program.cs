using System.Text.Json.Serialization;
using CleanCom.Api.Common;
using CleanCom.Application;
using CleanCom.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, log) => log
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddSecurity();
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<ErrorHandler>();

    builder.Services
        .AddControllers(o =>
            // Validation is done by FluentValidation with Spanish messages; without this MVC would
            // mark non-nullable strings as required with English messages.
            o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
        .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            // Numbers as numbers: the client generated from OpenAPI types them as number, not number | string.
            o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
        })
        .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = _ =>
            // Only an empty body or malformed JSON gets here.
            new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Petición no válida.",
                Detail = "Revisa el formato de los datos enviados.",
            }));
    builder.Services.ConfigureHttpJsonOptions(o =>
    {
        o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    });

    // The office web lives on another subdomain (oficina.<dominio>); in development it goes through the Vite proxy.
    var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
    builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
        .WithOrigins(origins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

    builder.Services.AddOpenApi(o => o.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "CleanCom API";
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token de acceso de POST /auth/login.",
        };
        // Swagger UI sends the token on every request after pressing "Authorize".
        document.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] }];
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

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    await app.Services.SeedAdministratorAsync(app.Configuration);
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "The API stopped because of a startup error");
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Entry point; public for integration tests.</summary>
public partial class Program;
