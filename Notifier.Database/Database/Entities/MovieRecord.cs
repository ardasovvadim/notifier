using System.ComponentModel.DataAnnotations;

namespace Notifier.Database.Database.Entities;

public class MovieRecord
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; }

    public string? Info { get; set; }
    
    public string? Link { get; set; }

    public MovieState State { get; set; }

    public bool Notified { get; protected set; }

    public DateTime? NotifiedAt { get; protected set; }

    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }

    public bool IsRemoved { get; set; }

    public int? LastSeason { get; set; }

    public int? LastEpisode { get; set; }
    
    public void SetNotified()
    {
        Notified = true;
        NotifiedAt = DateTime.UtcNow;
    }
}