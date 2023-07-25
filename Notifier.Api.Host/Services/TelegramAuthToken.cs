namespace Notifier.Api.Host.Services;

public class TelegramAuthToken
{
    public Guid UserId { get; set; }
    public string Token { get; set; }
}