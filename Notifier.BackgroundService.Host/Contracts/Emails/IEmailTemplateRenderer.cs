namespace Notifier.BackgroundService.Host.Contracts.Emails;

public interface IEmailTemplateRenderer
{
    Task<string?> RenderEmailTemplateAsync(EmailTemplate emailTemplate, object model);
}