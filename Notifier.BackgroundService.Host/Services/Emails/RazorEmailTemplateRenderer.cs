using Notifier.BackgroundService.Host.Contracts.Emails;
using RazorLight;

namespace Notifier.BackgroundService.Host.Services.Emails;

public class RazorEmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly IRazorLightEngine _engine;
    private readonly ILogger<RazorEmailTemplateRenderer> _logger;

    public RazorEmailTemplateRenderer(
        IRazorLightEngine engine,
        ILogger<RazorEmailTemplateRenderer> logger
    )
    {
        _engine = engine;
        _logger = logger;
    }

    public async Task<string?> RenderEmailTemplateAsync(EmailTemplate emailTemplate, object model)
    {
        var key = ResolveEmailTemplateKey(emailTemplate);
        
        if (key == null)
        {
            _logger.LogError("Unknown email template key: {emailTemplate}", emailTemplate);
            return null;
        }
        
        return await _engine.CompileRenderAsync(key, model);
    }

    private string? ResolveEmailTemplateKey(EmailTemplate emailTemplate)
    {
        return emailTemplate switch
        {
            EmailTemplate.NewSeriesNotificationTemplate => "NewSeriesNotificationTemplate",
            EmailTemplate.ErrorNotificationTemplate => "ErrorNotificationTemplate",
            _ => null
        };
    }
}