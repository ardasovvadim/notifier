using Notifier.BackgroundService.Host.Contracts.Rezka;

namespace Notifier.BackgroundService.Host.Contracts.Emails;

public class NewMoviesEmailModel
{
    public List<RezkaMovieInfo> Movies { get; set; }
    public string? Name { get; set; }
}