using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Event
{
    public int EventId { get; set; }

    public int UserId { get; set; }            // FK: the Organiser who created it
    public AppUser? Organiser { get; set; }

    [Required, MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    public string? Description { get; set; }
    public DateTime EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    public decimal Distance { get; set; }

    [Required, MaxLength(20)]
    public string EventType { get; set; } = string.Empty;   // Running, Walking or Cycling

    [MaxLength(500)]
    public string? BannerImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<Category> Categories { get; set; } = new();
    public List<Enrolment> Enrolments { get; set; } = new();
}
