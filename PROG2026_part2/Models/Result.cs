namespace PROG2026_part2.Models;

/// <summary>
/// A Participant's finishing time and finishing position, captured by an Organizer after the event.
/// </summary>
public class Result
{
    public int Id { get; set; }

    // The Participant the result belongs to
    public int UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // The event and category the result was achieved in
    public int EventId { get; set; }
    public Event? Event { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
