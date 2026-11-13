using CleanCom.Application.ParadasFijas;
using CleanCom.Domain.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanCom.Jobs;

/// <summary>
/// Generates the paradas fijas for the days ahead (Fijas.DiasAnticipacion): once when the API starts and every night
/// at <see cref="RunAt"/> Mexico City time. Generation is idempotent, so running it twice creates nothing new.
/// Moves to Hangfire when the other nightly jobs (CRM status, KPIs, fleet) arrive.
/// </summary>
public class FijasNightlyJob(IServiceScopeFactory scopes, IBusinessClock clock, ILogger<FijasNightlyJob> logger) : BackgroundService
{
    public static readonly TimeOnly RunAt = new(0, 15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunOnceAsync(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            var next = BusinessTime.ToUtc(clock.Today.AddDays(1), RunAt);
            var wait = next - clock.UtcNow;
            try
            {
                await Task.Delay(wait > TimeSpan.Zero ? wait : TimeSpan.FromMinutes(1), stoppingToken);
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
            var result = await scope.ServiceProvider.GetRequiredService<ParadaFijaService>().GenerateAheadAsync(ct);
            logger.LogInformation("Paradas fijas: {Message}", result.Message);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            // A failure (database down, for example) must not stop the API; the next run tries again.
            logger.LogError(ex, "Paradas fijas generation failed");
        }
    }
}
