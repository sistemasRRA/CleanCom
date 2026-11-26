using CleanCom.Application.Kpis;
using CleanCom.Domain.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanCom.Jobs;

/// <summary>
/// Calculates and saves the KPIs of the open week, month and quarter (and the ones before) every night at
/// <see cref="RunAt"/> Mexico City time, after the day's operations closed. Moves to Hangfire with the other nightly jobs.
/// </summary>
public class KpisNightlyJob(IServiceScopeFactory scopes, IBusinessClock clock, ILogger<KpisNightlyJob> logger) : BackgroundService
{
    public static readonly TimeOnly RunAt = new(1, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var next = BusinessTime.ToUtc(clock.Today, RunAt);
            if (next <= clock.UtcNow)
            {
                next = BusinessTime.ToUtc(clock.Today.AddDays(1), RunAt);
            }
            try
            {
                await Task.Delay(next - clock.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var periods = await scope.ServiceProvider.GetRequiredService<KpiService>().RecalculateOpenAsync(ct);
            logger.LogInformation("KPIs: {Periods} periods calculated", periods);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            // A failure must not stop the API; the screen calculates what is missing and the next night tries again.
            logger.LogError(ex, "KPIs calculation failed");
        }
    }
}
