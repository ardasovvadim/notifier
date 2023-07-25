using Notifier.Api.Host.Contracts;
using Telegram.Bot.Types;

namespace Notifier.Api.Host.Services;

public interface ITelegramService
{
    Task<BotStatusResponseDto> ConfigureAsync();
    Task<BotStatusResponseDto> GetStatusAsync();
    Task ProcessUpdateAsync(Update update);
}