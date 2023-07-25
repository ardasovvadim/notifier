using System.Net;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Notifier.BackgroundService.Host.Contracts.Configs;
using Notifier.Database.Database;
using Telegram.Bot;

namespace Notifier.BackgroundService.Host.Workers
{
    public class VisaCheckerWorker : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly VisaCheckerSettings _settings;
        private readonly ILogger<VisaCheckerWorker> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly List<string> _lastNotifiedMonths = new();

        private const string MainPageUrl = "https://pieraksts.mfa.gov.lv/ru/moskva";
        private const string Step1PageUrl = "https://pieraksts.mfa.gov.lv/ru/moskva/index";
        private const string Step2PageUrl = "https://pieraksts.mfa.gov.lv/ru/moskva/step2";
        private const string DatesPageUrl = "https://pieraksts.mfa.gov.lv/ru/calendar/available-month-dates?year={0}&month={1}";
        private const string TokenXpath = "//input[@name='_csrf-mfa-scheduler']";
        private const string DatesNotAvailableText = "Šobrīd visi pieejamie laiki ir aizņemti";

        public VisaCheckerWorker(IOptions<VisaCheckerSettings> settings, ILogger<VisaCheckerWorker> logger, IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider)
        {
            _settings = settings.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Checking visa dates...");
                
                try
                {
                    await DoCheck(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while checking visa dates.");
                }
                
                _logger.LogInformation("Waiting for next check in {minutes} minutes...", _settings.MinutesInterval);

                await Task.Delay(TimeSpan.FromMinutes(_settings.MinutesInterval), stoppingToken);
            }
        }

        private async Task DoCheck(CancellationToken stoppingToken)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Clear();

            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
                UseDefaultCredentials = false,
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            using var client = new HttpClient(handler);

            // Step 1 - Initial request, getting token
            _logger.LogInformation("Step 1 - Initial request, getting token");

            var mainPageResponse = await client.GetAsync(MainPageUrl, stoppingToken);
            mainPageResponse.EnsureSuccessStatusCode();

            var mainPageContent = await mainPageResponse.Content.ReadAsStringAsync(stoppingToken);
            var doc = new HtmlDocument();
            doc.LoadHtml(mainPageContent);

            var tokenNode = doc.DocumentNode.SelectSingleNode(TokenXpath);
            var token = tokenNode?.GetAttributeValue("value", null);

            if (string.IsNullOrWhiteSpace(token))
                throw new Exception("Can't find scheduler token");

            _logger.LogInformation("Token: {token}", token);

            // Step 2 - Set data
            _logger.LogInformation("Step 2 - Setting data");
            
            var step1Response = await client.PostAsync(Step1PageUrl, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "_csrf-mfa-scheduler", token },
                { "branch_office_id", _settings.OfficeId.ToString() },
                { "Persons[0][first_name]", _settings.FirstName },
                { "Persons[0][last_name]", _settings.LastName },
                { "e_mail", _settings.Email },
                { "e_mail_repeat", _settings.Email },
                { "phone", _settings.Phone }
            }), stoppingToken);
            
            step1Response.EnsureSuccessStatusCode();
            
            var step1Content = await step1Response.Content.ReadAsStringAsync(stoppingToken);
            doc.LoadHtml(step1Content);
            
            tokenNode = doc.DocumentNode.SelectSingleNode(TokenXpath);
            token = tokenNode?.GetAttributeValue("value", null);

            if (string.IsNullOrWhiteSpace(token))
                throw new Exception("Can't find scheduler token");

            _logger.LogInformation("New token: {token}", token);

            // Step 3 - Request dates page
            _logger.LogInformation("Step 3 - Requesting dates page");
            
            var step2Response = await client.PostAsync(Step2PageUrl, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "_csrf-mfa-scheduler", token },
                { "Persons[0][service_ids][]", _settings.ServiceId.ToString() }
            }), stoppingToken);
            
            step2Response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("Step 3 done");

            // Step 4 - Checking available dates
            _logger.LogInformation("Step 4 - Checking available dates");
            
            var currentMonthRequestPath = string.Format(DatesPageUrl, DateTime.UtcNow.Year, DateTime.UtcNow.Month);
            var currentMonthResponse = await client.GetAsync(currentMonthRequestPath, stoppingToken);
            
            currentMonthResponse.EnsureSuccessStatusCode();
            
            var currentMonthContent = await currentMonthResponse.Content.ReadAsStringAsync(stoppingToken);

            if (!currentMonthContent.Contains(DatesNotAvailableText))
            {
                _logger.LogInformation("Dates available for current month: " + currentMonthContent);

                await NotifyAsync(currentMonthContent);
                
                return;
            }

            _logger.LogInformation($"Dates not available for current month {DateTime.UtcNow.Month}. Checking next month");

            var nextMonthRequestPath = string.Format(DatesPageUrl, DateTime.UtcNow.Year, DateTime.UtcNow.Month + 1);
            var nextMonthResponse = await client.GetAsync(nextMonthRequestPath, stoppingToken);
            
            nextMonthResponse.EnsureSuccessStatusCode();
            
            var nextMonthContent = await nextMonthResponse.Content.ReadAsStringAsync(stoppingToken);

            if (!nextMonthContent.Contains(DatesNotAvailableText))
            {
                _logger.LogInformation("Dates available for next month: " + nextMonthContent);

                await NotifyAsync(nextMonthContent);
                
                return;
            }

            _logger.LogInformation($"Dates not available for next month {DateTime.UtcNow.Month + 1}.");
            _lastNotifiedMonths.Clear();
        }

        private async Task NotifyAsync(string months)
        { 
            if (_lastNotifiedMonths.Contains(months))
            {
                _logger.LogInformation("Already notified");
                return;
            }
            
            await using var sc = _serviceProvider.CreateAsyncScope();
            await using var context = sc.ServiceProvider.GetRequiredService<NContext>();
            
            var users = await context.Users.ToListAsync();

            if (!users.Any())
            {
                _logger.LogInformation("No users found");
                return;
            }
            
            var telegramBot = sc.ServiceProvider.GetRequiredService<ITelegramBotClient>();
            
            foreach (var user in users)
            {
                await telegramBot.SendTextMessageAsync(user.ChatId, $"Visa dates available! Dates: {months}");
            }
            
            _lastNotifiedMonths.Add(months);
        }
    }
}
