using System.Text;
using ConsoleTables;
using Microsoft.Extensions.Options;
using Notifier.BackgroundService.Host.Contracts.Configs;
using Notifier.BackgroundService.Host.Contracts.Emails;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Notifier.BackgroundService.Host.Services.Emails;

public class EmailService : IEmailService
{
    private readonly SendGridSettings _settings;
    private readonly IEmailTemplateRenderer _emailTemplateRenderer;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SendGridSettings> options, ILogger<EmailService> logger, IEmailTemplateRenderer emailTemplateRenderer)
    {
        _logger = logger;
        _emailTemplateRenderer = emailTemplateRenderer;
        _settings = options.Value;
    }

    private async Task SendEmailAsync(string subject, string htmlContent, string plainTextContent)
    {
        var client = new SendGridClient(_settings.ApiKey);
        var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
        var to = new EmailAddress(_settings.ToEmail, _settings.ToName);
        
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        
        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("SendEmailAsync failed with status code {StatusCode}", response.StatusCode);
            throw new Exception($"SendEmailAsync failed with status code {response.StatusCode}");
        }
            
        _logger.LogInformation("SendEmailAsync succeeded with status code {StatusCode}", response.StatusCode);
    }

    public async Task SendNewSeriesAvailableMoviesAsync(NewMoviesEmailModel model)
    {
        model.Name ??= _settings.ToName;
        
        var htmlContent = await _emailTemplateRenderer.RenderEmailTemplateAsync(EmailTemplate.NewSeriesNotificationTemplate, model);
        
        _logger.LogInformation("Trying to send email with {MoviesCount} movies", model.Movies.Count);

        await SendEmailAsync("New series available", htmlContent, ToTextPlain(model));

        _logger.LogInformation("Email with {MoviesCount} movies was sent", model.Movies.Count);
    }

    public async Task SendErrorEmailAsync(string title, string error)
    {
        var htmlContent = await  _emailTemplateRenderer.RenderEmailTemplateAsync(EmailTemplate.ErrorNotificationTemplate, new ErrorEmailModel
        {
            ErrorText = error
        });
        
        _logger.LogInformation("Trying to send error email with title {Title}", title);
        
        await SendEmailAsync(title, htmlContent, $"{title}: {error}");
        
        _logger.LogInformation("Error email with title {Title} was sent", title);
    }

    private string ToTextPlain(NewMoviesEmailModel model)
    {
        var result = new StringBuilder();
        
        result.AppendLine($"New series available: {model.Movies.Count}");
        
        var table = new ConsoleTable("Title", "Info", "Link");
        
        foreach (var movie in model.Movies)
            table.AddRow(movie.Title, movie.Info, movie.Link);

        result.AppendLine(table.ToMarkDownString());
        
        return result.ToString();
    }
}