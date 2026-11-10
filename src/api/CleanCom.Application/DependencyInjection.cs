using CleanCom.Application.Access;
using CleanCom.Application.Catalogs;
using CleanCom.Application.Operations;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CleanCom.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Default FluentValidation messages in Spanish; field names come from WithName in each validator.
        ValidatorOptions.Global.LanguageManager.Culture = new System.Globalization.CultureInfo("es-MX");
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Singleton);

        services.AddScoped<ISettingService, SettingService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<ZonaService>();
        services.AddScoped<OperadorService>();
        return services;
    }
}
