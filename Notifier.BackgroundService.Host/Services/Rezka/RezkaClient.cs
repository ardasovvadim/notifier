using System.IO.Compression;
using System.Net;
using HtmlAgilityPack;
using Notifier.BackgroundService.Host.Contracts;
using Notifier.BackgroundService.Host.Contracts.Rezka;
using Notifier.BackgroundService.Host.Database.Entities;

namespace Notifier.BackgroundService.Host.Services.Rezka;

public class RezkaClient : IRezkaClient
{
    private const string ContinueRowsXpath = "//*[@id=\"videosaves-list\"]/*[starts-with(@id, \"videosave\")]";
    private const string ContinueRowInfoXpath = ".//*[contains(@class, \"info\")]/*/a";
    private const string ContinueRowTitleXPath = ".//*[contains(@class, \"title\")]/a";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RezkaClient> _logger;
    
    private static readonly IReadOnlyCollection<string> NextSeriesAvailableStates = new List<string>
    {
        "доступен",
        "смотреть следующий",
        "доступна"
    }; 
    
    private static readonly IReadOnlyCollection<string> WatchNextStates = new List<string>
    {
        "смотреть ещё",
    }; 
    
    private const string SeasonsXPath = "//*[@id=\"simple-seasons-tabs\"]/li";
    private string LastEpisodeXPath(int season) => $"//*[@id=\"simple-episodes-list-{season}\"]/li[last()]";

    public RezkaClient(IHttpClientFactory httpClientFactory, ILogger<RezkaClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<RezkaMovieInfo>> GetMoviesInfoAsync()
    {
        var doc = await GetContinueDocumentAsync();

        if (doc == null)
            return new List<RezkaMovieInfo>();

        if (IsNotLoggedIn(doc))
        {
            _logger.LogError("Seems like cookie is expired or invalid. Please, login to rezka.ag and try again.");
            throw new Exception("Seems like cookie is expired or invalid. Please, login to rezka.ag and try again.");
        }

        return doc
            .DocumentNode
            ?.SelectNodes(ContinueRowsXpath)
            ?.Select(row =>
            {
                var info = row.SelectSingleNode(ContinueRowInfoXpath);
                var title = row.SelectSingleNode(ContinueRowTitleXPath);
                var isNewAvailable = IsNextSeriesAvailable(info);

                var state = MovieState.None;

                if (isNewAvailable)
                    state = MovieState.NewSeriesAvailable;
                else if (IsWatchNextAvailable(info))
                    state = MovieState.WatchNext;
                else if (row.HasClass("watched-row"))
                    state = MovieState.Watched;
                
                var (lastSeason, lastEpisode) = GetLastSeasonAndEpisode(info, state);

                var link = state switch
                {
                    MovieState.NewSeriesAvailable => info?.GetAttributeValue("href", null),
                    MovieState.WatchNext => info?.GetAttributeValue("href", null),
                    _ => title?.GetAttributeValue("href", null)
                };

                var result = new RezkaMovieInfo
                {
                    Id = int.Parse(row.Id.Split("-")[1]),
                    Title = title?.InnerText ?? "",
                    Info = info?.InnerText ?? "",
                    State = state,
                    Link = link,
                    LastSeason = lastSeason,
                    LastEpisode = lastEpisode
                };

                return result;
            }).ToList() ?? new List<RezkaMovieInfo>();
    }

    public async Task<RezkaLastMovieSeasonInfo> GetLastSeasonInfoAsync(string link)
    {
        var result = new RezkaLastMovieSeasonInfo();
        var document = await GetPageDocumentAsync(link);
        var lastSeasonNode = document
            .DocumentNode
            ?.SelectNodes(SeasonsXPath)
            ?.LastOrDefault()
            ;

        if (lastSeasonNode == null)
            return result;

        var lastSeasonStr = lastSeasonNode.GetAttributeValue("data-tab_id", null);
        
        if (string.IsNullOrWhiteSpace(lastSeasonStr) || !int.TryParse(lastSeasonStr, out var lastSeason))
            return result;
        
        result.LastSeason = lastSeason;
        
        var lastEpisodeNode = document
            .DocumentNode
            ?.SelectSingleNode(LastEpisodeXPath(lastSeason))
            ;

        if (lastEpisodeNode == null)
            return new RezkaLastMovieSeasonInfo { LastSeason = lastSeason };
        
        var lastEpisodeStr = lastEpisodeNode.GetAttributeValue("data-episode_id", null);
        
        if (string.IsNullOrWhiteSpace(lastEpisodeStr) || !int.TryParse(lastEpisodeStr, out var lastEpisode))
            return result;
        
        result.LastEpisode = lastEpisode;
        
        return result;
    }

    private async Task<HtmlDocument> GetPageDocumentAsync(string link)
    {
        using var client = _httpClientFactory.CreateClient(NConsts.RezkaClientName);
        var response = await client.GetAsync(link);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("There is an error while getting Rezka page. Link: {link}. Status code: {statusCode}. Reason: {reason}", link, response.StatusCode, response.ReasonPhrase);
            throw new Exception($"There is an error while getting Rezka page. Link: {link}. Status code: {response.StatusCode}. Reason: {response.ReasonPhrase}");
        }

        var content = await DecodeResponseAsync(response);

        var doc = new HtmlDocument();
        doc.LoadHtml(content);

        return doc;
    }

    private (int? lastSeason, int? lastEpisode) GetLastSeasonAndEpisode(HtmlNode? info, MovieState state)
    {
        if (info == null || state != MovieState.WatchNext)
            return (null, null);
        
        var parts = info.InnerText
            .Split(' ')
            .Where(p => !string.IsNullOrWhiteSpace(p) && int.TryParse(p, out _))
            .Select(int.Parse)
            .ToList();
                
        if (parts.Count == 2)
            return (parts.Last(), parts.First());
        
        return (null, null);
    }

    private bool IsWatchNextAvailable(HtmlNode? info) => info != null && WatchNextStates.Any(state => info.InnerHtml.Contains(state, StringComparison.OrdinalIgnoreCase));
    private bool IsNextSeriesAvailable(HtmlNode? info) => info != null && NextSeriesAvailableStates.Any(state => info.InnerHtml.Contains(state, StringComparison.OrdinalIgnoreCase));

    private bool IsNotLoggedIn(HtmlDocument doc)
    {
        var node = doc.DocumentNode.SelectSingleNode("//div[@class=\"b-info__message\"]");
        return node?.InnerText.Contains("Раздел доступен для зарегистрированных пользователей") ?? false;
    }

    private async Task<HtmlDocument?> GetContinueDocumentAsync() => await GetPageDocumentAsync("/continue/");

    private static async Task<string> DecodeResponseAsync(HttpResponseMessage response)
    {
        if (!response.Content.Headers.ContentEncoding.Contains("gzip"))
            return await response.Content.ReadAsStringAsync();

        await using var gzip = new GZipStream(await response.Content.ReadAsStreamAsync(), CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        return await reader.ReadToEndAsync();
    }
}