using CleanCom.Application.Avisos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanCom.Jobs;

/// <summary>
/// Sends the pending avisos (the outbox) every <see cref="Every"/>, so a slow provider never slows down the chofer or
/// the office. Moves to Hangfire with the other jobs.
/// </summary>
public class AvisosJob(IServiceScopeFactory scopes, ILogger<AvisosJob> logger) : BackgroundService
{
    public static readonly TimeSpan Every = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<AvisoDispatcher>();
                // A full batch means there may be more: go on without waiting.
                while (await dispatcher.SendPendingAsync(stoppingToken) == AvisoDispatcher.Batch)
                {
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not send the pending avisos");
            }
            try
            {
                await Task.Delay(Every, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
