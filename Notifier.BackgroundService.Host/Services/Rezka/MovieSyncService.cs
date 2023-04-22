using ConsoleTables;
using Microsoft.EntityFrameworkCore;
using Notifier.BackgroundService.Host.Contracts.Emails;
using Notifier.BackgroundService.Host.Contracts.Rezka;
using Notifier.BackgroundService.Host.Database;
using Notifier.BackgroundService.Host.Database.Entities;

namespace Notifier.BackgroundService.Host.Services.Rezka;

public class MovieSyncService : IMovieSyncService
{
    private readonly IRezkaClient _rezkaClient;
    private readonly ILogger<MovieSyncService> _logger;
    private readonly NContext _context;
    private readonly IEmailService _emailService;

    public MovieSyncService(
        IRezkaClient rezkaClient, 
        NContext context,
        ILogger<MovieSyncService> logger, 
        IEmailService emailService)
    {
        _rezkaClient = rezkaClient;
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task SyncMoviesAsync()
    {
        _logger.LogInformation("SyncMoviesAsync started");

        var movieInfos = await _rezkaClient.GetMoviesInfoAsync();

        _logger.LogInformation("SyncMoviesAsync got {Count} movie infos from Rezka", movieInfos.Count);
        
        var newSeriesAvailableMovies = movieInfos
            .Where(e => e.State == MovieState.NewSeriesAvailable)
            .ToList();
        
        var newMovies = new List<MovieRecord>();

        foreach (var movieInfo in newSeriesAvailableMovies)
        {
            var movieExists = await _context.MoviesRecords.AnyAsync(e => e.Id == movieInfo.Id);
            
            if (movieExists)
                continue;
            
            var movie = new MovieRecord
            {
                Id = movieInfo.Id,
                Title = movieInfo.Title,
                Info = movieInfo.Info,
                State = movieInfo.State,
                Link = movieInfo.Link,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            newMovies.Add(movie);
        }

        if (newMovies.Count == 0)
        {
            _logger.LogInformation("SyncMoviesAsync no new movies found");
            return;
        }
        
        await _context.MoviesRecords.AddRangeAsync(newMovies);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("SyncMoviesAsync got {Count} new series available movies", newMovies.Count);
        LogToLoggerTable(newMovies);
        
        await _emailService.SendNewSeriesAvailableMoviesAsync(new NewMoviesEmailModel
        {
            Movies = newSeriesAvailableMovies
                .Where(e => newMovies.Any(m => m.Id == e.Id))
                .ToList()
        });
    }

    private void LogToLoggerTable(List<MovieRecord> movies)
    {
        var table = new ConsoleTable("Id", "Title", "Info", "State", "Link");
        
        foreach (var movie in movies)
            table.AddRow(movie.Id, movie.Title, movie.Info, movie.State.ToString("G"), movie.Link);

        _logger.LogInformation(table.ToString());
    }
}