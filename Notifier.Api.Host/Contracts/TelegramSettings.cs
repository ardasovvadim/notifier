namespace Notifier.Api.Host.Contracts;

public class TelegramSettings
{
    public const string Name = "Telegram";
    public string Secret { get; set; }
    public string WebhookUrl { get; set; }
    public string PublicKeyFile { get; set; }
    public string BotUrl { get; set; }
}