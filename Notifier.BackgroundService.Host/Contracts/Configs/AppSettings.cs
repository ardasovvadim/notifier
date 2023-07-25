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

public class VisaCheckerSettings
{
    public const string SectionName = "VisaChecker";
    
    public int OfficeId { get; set; }
    public int ServiceId { get; set; }
    public int MinutesInterval { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
}