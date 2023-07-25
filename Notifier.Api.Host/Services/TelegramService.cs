using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Notifier.Api.Host.Contracts;
using Notifier.Database.Database;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Notifier.Database.Database.Entities.User;

namespace Notifier.Api.Host.Services;

public class TelegramService : ITelegramService
{
    private readonly ILogger<TelegramService> _logger;
    private readonly NContext _context;
    private readonly TelegramSettings _settings;
    private readonly ITelegramBotClient _client;
    
    public TelegramService(
        IOptions<TelegramSettings> botSettings,
        ITelegramBotClient client, ILogger<TelegramService> logger, NContext context)
    {
        _logger = logger;
        _context = context;
        _settings = botSettings.Value;
        _client = client;
    }

    public async Task<BotStatusResponseDto> ConfigureAsync()
    {
        if (!await _client.TestApiAsync())
            return new BotStatusResponseDto
            {
                TokenIsValid = false,
                Error = "Telegram bot is not ok"
            };

        await _client.DeleteWebhookAsync();
        await _client.SetWebhookAsync(
            url: _settings.WebhookUrl,
            allowedUpdates: new[] { UpdateType.Message }
        );

        return new BotStatusResponseDto
        {
            TokenIsValid = true,
            WebHookInfo = await _client.GetWebhookInfoAsync()
        };
    }

    public async Task<BotStatusResponseDto> GetStatusAsync()
    {
        var webHookStatusTask = _client.GetWebhookInfoAsync();
        var tokenIsValidTask = _client.TestApiAsync();

        await Task.WhenAll(webHookStatusTask, tokenIsValidTask);

        return new BotStatusResponseDto
        {
            TokenIsValid = tokenIsValidTask.Result,
            WebHookInfo = webHookStatusTask.Result
        };
    }

    public async Task ProcessUpdateAsync(Update update)
    {
        LogUpdate(update, "New update");

        var message = update.Message;

        if (message == null)
        {
            LogUpdate(update, "Message is null");
            return;
        }

        var chatId = message.Chat.Id;
        LogUpdate(update, "Text message received.");

        try
        {
            switch (message.Type)
            {
                case MessageType.Text:
                {
                    var text = message.Text;
                
                    if (text == null)
                        break;

                    if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
                    {
                        await _client.SendTextMessageAsync(chatId, "Hola! To subscribe on updates, please type /subscribe or use command.");
                    } 
                    else if (text.StartsWith("/subscribe", StringComparison.OrdinalIgnoreCase))
                    {
                        _context.Add(new User
                        {
                            ChatId = chatId
                        });

                        await _context.SaveChangesAsync();
                        
                        await _client.SendTextMessageAsync(chatId, "You have been subscribed on updates.");
                    }
                    else if (text.StartsWith("/unsubscribe", StringComparison.OrdinalIgnoreCase))
                    {
                        var u = await _context.Users.FirstOrDefaultAsync(u => u.ChatId == chatId);

                        if (u != null)
                        {
                            _context.Users.Remove(u);
                            await _context.SaveChangesAsync();
                        }

                        await _client.SendTextMessageAsync(chatId, "You have been unsubscribed from updates.");
                    }
                    
                    break;
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "During processing telegram message something went wrong");
        }
    }

    public async Task SendMessageAsync(long chatId, string message)
    {
        await _client.SendTextMessageAsync(chatId, message, parseMode: ParseMode.Html);
    }

    private void LogUpdate(Update update, string message)
    {
        _logger.LogInformation("Update id: {id}. {message}", update.Id, message);
    }
}