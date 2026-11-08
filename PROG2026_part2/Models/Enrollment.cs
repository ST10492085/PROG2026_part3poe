namespace PROG2026_part2.Models;

/// <summary>
/// Links a Participant to an event and the category they chose.
/// This is the "event enrolment" record required by the brief.
/// </summary>
public class Enrollment
{
    public int Id { get; set; }

    // Who enrolled
    public int UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Which event they entered
    public int EventId { get; set; }
    public Event? Event { get; set; }

    // Which category they selected within that event
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
}
