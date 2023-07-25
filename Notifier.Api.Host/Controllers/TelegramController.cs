using Microsoft.AspNetCore.Mvc;
using Notifier.Api.Host.Contracts;
using Notifier.Api.Host.Etc;
using Notifier.Api.Host.Services;
using Telegram.Bot.Types;

namespace Notifier.Api.Host.Controllers;

[ApiController, Route("notifier-api/telegram")]
public class TelegramController : ControllerBase
{
    public TelegramController(ITelegramService telegramService)
    {
        TelegramService = telegramService;
    }

    private ITelegramService TelegramService { get; }

    [HttpPost("19cee13e707c4ca89eca8e4bb07f2222")]
    public async Task GetAsync([ModelBinder(typeof(TelegramModelBinder))] Update update)
    {
        await TelegramService.ProcessUpdateAsync(update);
    }

    [HttpPost("configure")]
    public async Task<BotStatusResponseDto> ConfigureAsync()
    {
        return await TelegramService.ConfigureAsync();
    }

    [HttpGet("state")]
    public async Task<BotStatusResponseDto> GetStatusAsync()
    {
        return await TelegramService.GetStatusAsync();
    }
}