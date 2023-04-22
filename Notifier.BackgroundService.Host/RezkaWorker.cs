using Microsoft.Extensions.Options;
using Notifier.BackgroundService.Host.Contracts.Configs;
using Notifier.BackgroundService.Host.Contracts.Emails;
using Notifier.BackgroundService.Host.Contracts.Rezka;

namespace Notifier.BackgroundService.Host;

public class RezkaWorker : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly ILogger<RezkaWorker> _logger;
    private readonly AppSettings _appSettings;
    private readonly IServiceProvider _serviceProvider;

    public RezkaWorker(
        ILogger<RezkaWorker> logger,
        IOptions<AppSettings> appSettings,
        IServiceProvider serviceProvider 
    )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _appSettings = appSettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            
            await using var scope = _serviceProvider.CreateAsyncScope();
            var continueMovieService = scope.ServiceProvider.GetRequiredService<IMovieSyncService>();

            try
            {
                await continueMovieService.SyncMoviesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while syncing movies. Stop worker until error will be fixed.");
                await SendErrorEmailAsync(scope, e);

                break;
            }
            
            await Task.Delay(TimeSpan.FromMinutes(_appSettings.RezkaPeriodInMinutes), stoppingToken);
        }
    }

    private async Task SendErrorEmailAsync(AsyncServiceScope scope, Exception exception)
    {
        try
        {
            await scope.ServiceProvider
                .GetRequiredService<IEmailService>()
                .SendErrorEmailAsync("Error while syncing movies", exception.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while sending error email");
        }
    }
}