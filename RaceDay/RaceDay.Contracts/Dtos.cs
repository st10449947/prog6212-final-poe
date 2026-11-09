using System.ComponentModel.DataAnnotations;

namespace RaceDay.Contracts;

// ---------- Authentication ----------
public class RegisterRequest
{
    [Required, MaxLength(100)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
    [Required] public string Role { get; set; } = string.Empty;   // Organiser or Participant
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

// ---------- Profile ----------
public class ProfileDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UpdateProfileRequest
{
    [Required, MaxLength(100)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = string.Empty;
}

// ---------- Events ----------
public class CreateEventRequest
{
    [Required, MaxLength(150)] public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required] public DateTime EventDate { get; set; }
    [Required, MaxLength(150)] public string Location { get; set; } = string.Empty;
    [Range(0.1, 1000)] public decimal Distance { get; set; }
    [Required, RegularExpression("^(Running|Walking|Cycling)$",
        ErrorMessage = "EventType must be Running, Walking or Cycling.")]
    public string EventType { get; set; } = string.Empty;
}

public class UpdateEventRequest : CreateEventRequest { }

public class EventDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string? BannerImageUrl { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
}

// ---------- Categories ----------
public class CreateCategoryRequest
{
    [Required, MaxLength(100)] public string CategoryName { get; set; } = string.Empty;
    [Range(0.1, 1000)] public decimal DistanceKm { get; set; }
    [Range(1, 100000)] public int MaxParticipants { get; set; } = 100;
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
}

// ---------- Enrolments ----------
public class CreateEnrolmentRequest
{
    public int EventId { get; set; }
    public int CategoryId { get; set; }
}

public class UpdateEnrolmentStatusRequest
{
    [Required, RegularExpression("^(Pending|Confirmed|Cancelled)$",
        ErrorMessage = "Status must be Pending, Confirmed or Cancelled.")]
    public string Status { get; set; } = string.Empty;
}

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

// ---------- Results ----------
public class CreateResultRequest
{
    public int EnrolmentId { get; set; }
    [Range(1, int.MaxValue)] public int FinishTimeSeconds { get; set; }
    [Range(1, int.MaxValue)] public int FinishPosition { get; set; }
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public int FinishTimeSeconds { get; set; }
    public int FinishPosition { get; set; }
    public DateTime RecordedAt { get; set; }

    public class UploadResponse
    {
        public string ImageUrl { get; set; } = string.Empty;
    }
}
