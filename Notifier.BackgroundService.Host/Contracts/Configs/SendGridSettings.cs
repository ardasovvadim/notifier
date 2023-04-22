namespace Notifier.BackgroundService.Host.Contracts.Configs;

public class SendGridSettings
{
    public const string SectionName = "SendGrid";
    public string FromEmail { get; set; }
    public string FromName { get; set; }
    public string ToEmail { get; set; }
    public string ToName { get; set; }
    public string ApiKey { get; set; }
}