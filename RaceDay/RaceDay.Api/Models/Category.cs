using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Category
{
    public int CategoryId { get; set; }

    public int EventId { get; set; }           // FK to Events
    public Event? Event { get; set; }

    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; } = 100;

    public RaceRoute? Route { get; set; }      // 1:1 with Routes
    public List<Enrolment> Enrolments { get; set; } = new();
}