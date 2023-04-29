using Notifier.BackgroundService.Host.Contracts.Rezka;

namespace Notifier.BackgroundService.Host.Contracts.Emails;

public class NewMoviesEmailModel
{
    public List<RezkaMovieInfo> NewMovies { get; set; }
    public List<RezkaMovieInfo> NewSeasons { get; set; }
    public string? Name { get; set; }
}