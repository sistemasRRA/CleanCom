using Microsoft.Extensions.DependencyInjection;

namespace CleanCom.Jobs;

public static class DependencyInjection
{
    public static IServiceCollection AddJobs(this IServiceCollection services)
    {
        services.AddHostedService<FijasNightlyJob>();
        services.AddHostedService<AvisosJob>();
        services.AddHostedService<KpisNightlyJob>();
        return services;
    }
}
