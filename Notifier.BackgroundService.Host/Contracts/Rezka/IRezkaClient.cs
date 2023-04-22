namespace Notifier.BackgroundService.Host.Contracts.Rezka;

public interface IRezkaClient
{
    Task<List<RezkaMovieInfo>> GetMoviesInfoAsync();
}