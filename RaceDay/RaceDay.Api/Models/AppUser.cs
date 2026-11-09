using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class AppUser
{
    [Key]
    

    // ...the rest stays the same
    public int UserId { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Role { get; set; } = string.Empty;   // Organiser or Participant

    [MaxLength(500)]
    public string? ProfileImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<Event> Events { get; set; } = new();
    public List<Enrolment> Enrolments { get; set; } = new();
}