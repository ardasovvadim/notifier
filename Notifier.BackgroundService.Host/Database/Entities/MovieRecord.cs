using System.ComponentModel.DataAnnotations;

namespace Notifier.BackgroundService.Host.Database.Entities;

public class MovieRecord
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; }

    public string? Info { get; set; }
    
    public string? Link { get; set; }

    public MovieState State { get; set; }

    public bool Notified { get; set; }

    public DateTime? NotifiedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }

    public bool IsRemoved { get; set; }
}