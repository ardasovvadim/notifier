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
                var isNewAvailable = info?.InnerHtml.Contains("доступен") ?? false;
                var isWatched = row.HasClass("watched-row");
                var link = isNewAvailable
                    ? info?.GetAttributeValue("href", null)
                    : title?.GetAttributeValue("href", null);

                var state = MovieState.None;

                if (isNewAvailable)
                    state = MovieState.NewSeriesAvailable;
                else if (isWatched)
                    state = MovieState.Watched;

                var result = new RezkaMovieInfo
                {
                    Id = int.Parse(row.Id.Split("-")[1]),
                    Title = title?.InnerHtml ?? "",
                    Info = info?.InnerText ?? "",
                    State = state,
                    Link = link,
                };

                return result;
            }).ToList() ?? new List<RezkaMovieInfo>();
    }

    private bool IsNotLoggedIn(HtmlDocument doc)
    {
        var node = doc.DocumentNode.SelectSingleNode("//div[@class=\"b-info__message\"]");
        return node?.InnerText.Contains("Раздел доступен для зарегистрированных пользователей") ?? false;
    }

    private async Task<HtmlDocument?> GetContinueDocumentAsync()
    {
        using var client = _httpClientFactory.CreateClient(NConsts.RezkaClientName);
        var response = await client.GetAsync("/continue/");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"There is an error while getting movies info from Rezka. Status code: {response.StatusCode}. Reason: {response.ReasonPhrase}");
            throw new Exception($"There is an error while getting movies info from Rezka. Status code: {response.StatusCode}. Reason: {response.ReasonPhrase}");
        }

        var content = await DecodeResponseAsync(response);

        var doc = new HtmlDocument();
        doc.LoadHtml(content);

        return doc;
    }

    private static async Task<string> DecodeResponseAsync(HttpResponseMessage response)
    {
        if (!response.Content.Headers.ContentEncoding.Contains("gzip"))
            return await response.Content.ReadAsStringAsync();

        await using var gzip = new GZipStream(await response.Content.ReadAsStreamAsync(), CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        return await reader.ReadToEndAsync();
    }
}