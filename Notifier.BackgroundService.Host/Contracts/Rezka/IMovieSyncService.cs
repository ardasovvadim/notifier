namespace Notifier.BackgroundService.Host.Contracts.Rezka;

public interface IMovieSyncService
{
    Task SyncMoviesAsync();
}