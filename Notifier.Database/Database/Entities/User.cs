using System.ComponentModel.DataAnnotations;

namespace Notifier.Database.Database.Entities;

public class User
{
    [Key]
    public int Id { get; set; }
    public long ChatId { get; set; }
}