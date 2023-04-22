namespace Notifier.BackgroundService.Host.Contracts.Configs;

public class AppSettings
{
    public const string SectionName = "AppSettings";
    public string RezkaUrl { get; set; }
    public string RezkaCookieDleUserId { get; set; }
    public string RezkaCookieDlePassword { get; set; }
    public int RezkaPeriodInMinutes { get; set; }
    public string SendGridApiKey { get; set; }
}