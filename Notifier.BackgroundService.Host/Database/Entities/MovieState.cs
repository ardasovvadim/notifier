namespace Notifier.BackgroundService.Host.Database.Entities;

public enum MovieState
{
    None = 0,
    NewSeriesAvailable = 1,
    Watched = 2,
    NotCompleted = 3,
}