namespace Notifier.BackgroundService.Host.Contracts.Emails;

public interface IEmailService
{
    Task SendNewSeriesAvailableMoviesAsync(NewMoviesEmailModel model);
    Task SendErrorEmailAsync(string title, string error);
}