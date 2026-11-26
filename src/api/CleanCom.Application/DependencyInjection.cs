using CleanCom.Application.Access;
using CleanCom.Application.Agenda;
using CleanCom.Application.Avisos;
using CleanCom.Application.Chofer;
using CleanCom.Application.Invitados;
using CleanCom.Application.Kpis;
using CleanCom.Application.Pedidos;
using CleanCom.Application.PostalCodes;
using CleanCom.Application.Catalogs;
using CleanCom.Application.Crm;
using CleanCom.Application.Fleet;
using CleanCom.Application.Operations;
using CleanCom.Application.Panel;
using CleanCom.Application.ParadasFijas;
using CleanCom.Application.Promociones;
using CleanCom.Application.Rutas;
using CleanCom.Application.Tracking;
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
        services.AddScoped<ZonaLocator>();
        services.AddScoped<AgendaService>();
        services.AddScoped<InvitadoService>();
        services.AddScoped<MeService>();
        services.AddScoped<InvitadoAgendaService>();
        services.AddScoped<PedidoService>();
        services.AddScoped<PostalCodeService>();
        services.AddScoped<CrmService>();
        services.AddScoped<ChoferService>();
        services.AddScoped<FijaCalendar>();
        services.AddScoped<ParadaFijaService>();
        services.AddScoped<RutaDiaService>();
        services.AddScoped<PedidoBoardService>();
        services.AddScoped<PlantaService>();
        services.AddScoped<ChoferVisitService>();
        services.AddScoped<ChoferShiftService>();
        services.AddScoped<CleanCom.Application.Evidencias.EvidenciasService>();
        services.AddScoped<CleanCom.Application.Evidencias.CorteCajaService>();
        services.AddScoped<EvidenceService>();
        services.AddSingleton<LiveLocations>();
        services.AddScoped<LocationService>();
        services.AddScoped<ChoferPreviewService>();
        services.AddScoped<UnidadService>();
        services.AddScoped<PanelService>();
        services.AddScoped<BeneficioService>();
        services.AddScoped<PromocionService>();
        services.AddScoped<AvisoService>();
        services.AddScoped<AvisoDispatcher>();
        services.AddScoped<PlantillaService>();
        services.AddScoped<KpiCalculator>();
        services.AddScoped<KpiService>();
        services.AddScoped<KpiGoalService>();
        return services;
    }
}
