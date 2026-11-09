using System.ComponentModel.DataAnnotations;
using RaceDay.Contracts;

namespace RaceDay.Mvc.ViewModels;

public class OrganiserDashboardVm
{
    public List<OrganiserEventSummaryVm> Events { get; set; } = new();

    public int TotalEvents => Events.Count;
    public int UpcomingEvents => Events.Count(e => e.EventDate.Date >= DateTime.Today);
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
}

public class OrganiserEventSummaryVm
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int EnrolmentCount { get; set; }
}

/// <summary>One form class used for both creating and editing an event.</summary>
public class EventFormVm
{
    public int EventId { get; set; }

    [Required, MaxLength(150), Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Event date")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30);

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal Distance { get; set; }

    [Required, Display(Name = "Event type")]
    public string EventType { get; set; } = "Running";

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public string? BannerImageUrl { get; set; }
}

public class CategoryFormVm
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required, MaxLength(100), Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Range(1, 100000, ErrorMessage = "Maximum participants must be at least 1.")]
    [Display(Name = "Maximum participants")]
    public int MaxParticipants { get; set; } = 100;
}

public class CategoriesPageVm
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
}

public class EventEnrolmentsVm
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class ResultsPageVm
{
    public List<EventDto> Events { get; set; } = new();
    public EventDto? SelectedEvent { get; set; }
    public List<EnrolmentDto> Enrolments { get; set; } = new();
    public Dictionary<int, ResultDto> ResultsByEnrolment { get; set; } = new();
}