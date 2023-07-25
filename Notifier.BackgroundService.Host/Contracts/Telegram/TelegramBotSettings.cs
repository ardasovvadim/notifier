namespace Notifier.BackgroundService.Host.Contracts.Telegram;

public class TelegramBotSettings
{
    public const string SectionName = "AppSettings:Telegram";
    
    public string Secret { get; set; }
}