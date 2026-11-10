using CleanCom.Aplicacion.Acceso;
using CleanCom.Aplicacion.Catalogos;
using CleanCom.Aplicacion.Operacion;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CleanCom.Aplicacion;

public static class DependencyInjection
{
    public static IServiceCollection AddAplicacion(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Culture = new System.Globalization.CultureInfo("es-MX");
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Singleton);

        services.AddScoped<IServicioConfiguracion, ServicioConfiguracion>();
        services.AddScoped<ServicioCatalogos>();
        services.AddScoped<ServicioZonas>();
        services.AddScoped<ServicioOperadores>();
        return services;
    }
}
