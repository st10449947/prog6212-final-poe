using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class RaceRoute
{
    [Key]
    public int RouteId { get; set; }

    public int CategoryId { get; set; }        // FK + UNIQUE (1:1 with Categories)
    public Category? Category { get; set; }

    [Required, MaxLength(100)]
    public string RouteName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string StartPoint { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string EndPoint { get; set; } = string.Empty;

    public int ElevationGainM { get; set; }

    [MaxLength(255)]
    public string? MapUrl { get; set; }
}