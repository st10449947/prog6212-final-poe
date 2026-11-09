using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }

    public int UserId { get; set; }            // FK: the Participant who enrolled
    public AppUser? Participant { get; set; }

    public int EventId { get; set; }
    public Event? Event { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime EnrolmentDate { get; set; } = DateTime.Now;

    [MaxLength(20)]
    public string Status { get; set; } = "Confirmed";

    public Result? Result { get; set; }        // zero or one result
}