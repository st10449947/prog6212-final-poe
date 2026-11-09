namespace RaceDay.Api.Models;

public class Result
{
    public int ResultId { get; set; }

    public int EnrolmentId { get; set; }       // FK + UNIQUE (1:1 with Enrolments)
    public Enrolment? Enrolment { get; set; }

    public int UserId { get; set; }            // FK: the Organiser who captured it
    public AppUser? RecordedBy { get; set; }

    public int FinishTimeSeconds { get; set; }
    public int FinishPosition { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.Now;
}