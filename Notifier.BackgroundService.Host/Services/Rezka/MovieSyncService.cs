using AutoMapper;
using ConsoleTables;
using Microsoft.EntityFrameworkCore;
using Notifier.BackgroundService.Host.Contracts.Emails;
using Notifier.BackgroundService.Host.Contracts.Rezka;
using Notifier.Database.Database;
using Notifier.Database.Database.Entities;

namespace Notifier.BackgroundService.Host.Services.Rezka;

public class MovieSyncService : IMovieSyncService
{
    private readonly IRezkaClient _rezkaClient;
    private readonly ILogger<MovieSyncService> _logger;
    private readonly NContext _context;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;

    public MovieSyncService(
        IRezkaClient rezkaClient, 
        NContext context,
        ILogger<MovieSyncService> logger, 
        IEmailService emailService, 
        IMapper mapper
        )
    {
        _rezkaClient = rezkaClient;
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _mapper = mapper;
    }

    public async Task SyncMoviesAsync()
    {
        _logger.LogInformation("SyncMoviesAsync started");

        var movieInfos = await _rezkaClient.GetMoviesInfoAsync();

        _logger.LogInformation("SyncMoviesAsync got {Count} movie infos from Rezka", movieInfos.Count);
        
        await Sync(movieInfos);
    }

    private async Task Sync(List<RezkaMovieInfo> movieInfos)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var newMoviesAvailable = await SyncNewMovieSeriesAvailableAsync(movieInfos);
            // var newSeasonAvailable = await SyncNextMoreMovieSeries(movieInfos);
            var newSeasonAvailable = new List<RezkaMovieInfo>();

            if (newMoviesAvailable.Count > 0 || newSeasonAvailable.Count > 0)
            {
                await _emailService.SendNewSeriesAvailableMoviesAsync(new NewMoviesEmailModel
                {
                    NewMovies = movieInfos.Where(e => newMoviesAvailable.Any(m => m.Id == e.Id)).ToList(),
                    NewSeasons = newSeasonAvailable
                });

                newMoviesAvailable.ForEach(m => m.SetNotified());

                await _context.SaveChangesAsync();
            }
        }
        catch (Exception)
        {
            _logger.LogError("Rollback transaction");
            await transaction.RollbackAsync();
            
            throw;
        }
        
        await transaction.CommitAsync();
    }

    private async Task<List<RezkaMovieInfo>> SyncNextMoreMovieSeries(List<RezkaMovieInfo> movieInfos)
    {
        var newSeasonAvailable = new List<RezkaMovieInfo>();
        var watchNextMoviesInfo = movieInfos
            .Where(e => e.State == MovieState.WatchNext)
            .ToList();
        
        if (watchNextMoviesInfo.Count == 0)
            return newSeasonAvailable;
        
        var watchNextMoviesInfoIds = watchNextMoviesInfo.Select(e => e.Id).ToList();
        var dbMovies = await _context.MoviesRecords
            .Where(e => watchNextMoviesInfoIds.Contains(e.Id))
            .ToListAsync();
        
        var newMoviesInfo = watchNextMoviesInfo
            .Where(e => dbMovies.All(m => m.Id != e.Id))
            .ToList();
        var newMovies = _mapper.Map<List<MovieRecord>>(newMoviesInfo);

        if (newMovies.Count != 0)
        {
            await _context.MoviesRecords.AddRangeAsync(newMovies);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("SyncMoviesAsync got {Count} new watch next movies", newMovies.Count);
            
            dbMovies.AddRange(newMovies);
        }

        const int maxErrors = 5;
        
        foreach (var movieRecord in dbMovies)
        {
            var movieInfo = watchNextMoviesInfo.First(e => e.Id == movieRecord.Id);
            var movieLink = movieInfo.Link;

            if (string.IsNullOrEmpty(movieLink))
            {
                _logger.LogWarning("SyncMoviesAsync movie link is null or empty for movie {Id}", movieRecord.Id);
                continue;
            }

            RezkaLastMovieSeasonInfo? lastSeasonInfo = null;
            var errors = 0;

            while (true)
            {
                try
                {
                    lastSeasonInfo = await _rezkaClient.GetLastSeasonInfoAsync(movieLink);
                }
                catch (Exception e)
                {
                    errors++;
                    
                    _logger.LogError(e, "SyncMoviesAsync error while getting last season info for movie {id}. Error count: {errors}", movieRecord.Id, errors);

                    if (errors <= maxErrors)
                    {
                        const int delay = 3000;
                        
                        _logger.LogInformation("SyncMoviesAsync delay for {delay} ms", delay);
                        await Task.Delay(delay);
                        
                        continue;
                    }
                    
                    _logger.LogError("SyncMoviesAsync max errors count reached: {maxErrors}", maxErrors);
                }
                
                break;
            }
            
            if (lastSeasonInfo == null)
                continue;
            
            var lastCheckedSeason = movieRecord.LastSeason ?? 0;
            var lastCheckedEpisode = movieRecord.LastEpisode ?? 0;
            var newSeason = lastSeasonInfo.LastSeason ?? 0;
            var newEpisode = lastSeasonInfo.LastEpisode ?? 0;

            if (newSeason == 0 
                || newEpisode == 0 
                || lastCheckedSeason > newSeason 
                || (newSeason == lastCheckedSeason && newEpisode <= lastCheckedEpisode))
                continue;
            
            movieRecord.LastEpisode = newEpisode;
            movieRecord.LastSeason = newSeason;
            
            newSeasonAvailable.Add(movieInfo);
        }

        if (newSeasonAvailable.Count == 0)
            return newSeasonAvailable;
        
        await _context.SaveChangesAsync();

        _logger.LogInformation("SyncMoviesAsync got {Count} new seasons available movies", newSeasonAvailable.Count);
        LogToLoggerTable(newSeasonAvailable);
        
        return newSeasonAvailable;
    }

    private void LogToLoggerTable(List<RezkaMovieInfo> movies)
    {
        var table = new ConsoleTable("Id", "Title", "Info", "Season", "Episode", "State", "Link");
        
        foreach (var movie in movies)
            table.AddRow(movie.Id, movie.Title, movie.Info, movie.LastSeason, movie.LastEpisode, movie.State.ToString("G"), movie.Link);

        _logger.LogInformation(table.ToString());
    }

    private async Task<List<MovieRecord>> SyncNewMovieSeriesAvailableAsync(List<RezkaMovieInfo> movieInfos)
    {
        var newSeriesAvailableMovies = movieInfos
            .Where(e => e.State == MovieState.NewSeriesAvailable)
            .ToList();

        var newMovies = await GetNewMovieRecordsAsync(newSeriesAvailableMovies);

        if (newMovies.Count == 0)
        {
            _logger.LogInformation("SyncMoviesAsync no new movies found");
            return new List<MovieRecord>();
        }

        await _context.MoviesRecords.AddRangeAsync(newMovies);
        await _context.SaveChangesAsync();

        _logger.LogInformation("SyncMoviesAsync got {Count} new series available movies", newMovies.Count);
        LogToLoggerTable(newMovies);

        return newMovies;
    }

    private async Task<List<MovieRecord>> GetNewMovieRecordsAsync(List<RezkaMovieInfo> newSeriesAvailableMovies)
    {
        var newSeriesAvailableMoviesIds = newSeriesAvailableMovies.Select(e => e.Id).ToList();
        var existingMovieIds = await _context.MoviesRecords
            .Where(m => newSeriesAvailableMoviesIds.Contains(m.Id))
            .Select(m => m.Id)
            .ToListAsync();
        var notTrackedMoviesIds = newSeriesAvailableMoviesIds.Except(existingMovieIds).ToList();
        var notTrackedMovies = newSeriesAvailableMovies.Where(e => notTrackedMoviesIds.Contains(e.Id)).ToList();
        
        return _mapper.Map<List<MovieRecord>>(notTrackedMovies);
    }

    private void LogToLoggerTable(List<MovieRecord> movies)
    {
        var table = new ConsoleTable("Id", "Title", "Info", "Season", "Episode", "State", "Link");
        
        foreach (var movie in movies)
            table.AddRow(movie.Id, movie.Title, movie.Info, movie.LastSeason, movie.LastEpisode, movie.State.ToString("G"), movie.Link);

        _logger.LogInformation(table.ToString());
    }
}