using System.ComponentModel.DataAnnotations;
using RaceDay.Contracts;

namespace RaceDay.Mvc.ViewModels;

public class ParticipantEventsVm
{
    public List<EventDto> Events { get; set; } = new();
    public string? Type { get; set; }
    public string? Search { get; set; }
}

public class EventDetailsVm
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public EnrolmentDto? ExistingEnrolment { get; set; }
}

public class ProfileFormVm
{
    [Required, MaxLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    public string? ProfileImageUrl { get; set; }

    // Display only, never edited
    public string Role { get; set; } = string.Empty;
    public DateTime MemberSince { get; set; }
}