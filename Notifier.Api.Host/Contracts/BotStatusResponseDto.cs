using Telegram.Bot.Types;

namespace Notifier.Api.Host.Contracts;

public class BotStatusResponseDto
{
    public bool TokenIsValid { get; set; }
    public string Error { get; set; }
    public WebhookInfo WebHookInfo { get; set; }
}