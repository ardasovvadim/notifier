using Notifier.Database.Database.Entities;

namespace Notifier.BackgroundService.Host.Contracts.Rezka;

public class RezkaMovieInfo
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string? Info { get; set; }
    public string? Link { get; set; }
    public MovieState State { get; set; }
    public int? LastSeason { get; set; }
    public int? LastEpisode { get; set; }
}