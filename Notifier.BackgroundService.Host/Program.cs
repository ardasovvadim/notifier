using System.Net;
using Microsoft.EntityFrameworkCore;
using Notifier.BackgroundService.Host;
using Notifier.BackgroundService.Host.Contracts;
using Notifier.BackgroundService.Host.Contracts.Configs;
using Notifier.BackgroundService.Host.Contracts.Emails;
using Notifier.BackgroundService.Host.Contracts.Rezka;
using Notifier.BackgroundService.Host.Database;
using Notifier.BackgroundService.Host.Services.Emails;
using Notifier.BackgroundService.Host.Services.Rezka;
using RazorLight;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File( "logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var host = Host
            .CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                var config = context.Configuration;
                var appSettings = config.GetSection(AppSettings.SectionName).Get<AppSettings>()!;

                services.Configure<AppSettings>(config.GetSection(AppSettings.SectionName));
                services.Configure<SendGridSettings>(config.GetSection(SendGridSettings.SectionName));

                services.AddTransient<IMovieSyncService, MovieSyncService>();
                services.AddTransient<IRezkaClient, RezkaClient>();
                services.AddTransient<IEmailTemplateRenderer, RazorEmailTemplateRenderer>();
                services.AddTransient<IEmailService, EmailService>();
                services.AddSingleton<IRazorLightEngine>(_ =>
                {
                    var engine = new RazorLightEngineBuilder()
                        .UseEmbeddedResourcesProject(typeof(Program).Assembly, "Notifier.BackgroundService.Host.EmailTemplates")
                        .UseMemoryCachingProvider()
                        .Build();

                    return engine;
                });

                services.AddDbContext<NContext>(options => { options.UseMySQL(context.Configuration.GetConnectionString("DefaultConnection")!); });

                services.AddHttpClient(NConsts.RezkaClientName, client => { client.BaseAddress = new Uri(config[$"{AppSettings.SectionName}:{nameof(AppSettings.RezkaUrl)}"]!); })
                    .ConfigureHttpMessageHandlerBuilder(builder =>
                    {
                        var cookieContainer = new CookieContainer();
                        var handler = new HttpClientHandler
                        {
                            CookieContainer = cookieContainer
                        };

                        cookieContainer.Add(new Uri(appSettings.RezkaUrl), new Cookie("dle_user_id", appSettings.RezkaCookieDleUserId));
                        cookieContainer.Add(new Uri(appSettings.RezkaUrl), new Cookie("dle_password", appSettings.RezkaCookieDlePassword));

                        builder.PrimaryHandler = handler;
                    })
                    .ConfigureHttpClient(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);

                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");
                        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, sdch, br");
                        client.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.6,en;q=0.4");
                        client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
                        client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                        client.DefaultRequestHeaders.Add("Host", "rezka.ag");
                        client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
                    })
                    ;

                services.AddHostedService<RezkaWorker>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));
            })
            .UseSerilog()
            .Build()
        ;

    // migrate db
    using (var scope = host.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<NContext>>();

        logger.LogInformation("Migrating database");

        var context = scope.ServiceProvider.GetRequiredService<NContext>();

        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while migrating database");
            throw;
        }
    }

    await host.RunAsync();
}
catch (Exception e)
{
    Log.Fatal(e, "Host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}