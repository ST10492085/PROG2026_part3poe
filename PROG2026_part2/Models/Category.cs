namespace PROG2026_part2.Models;

/// <summary>
/// An age or distance category belonging to one event (e.g. "Under 20", "10K Open").
/// </summary>
public class Category
{
    public int Id { get; set; }

    // Foreign key: the event this category belongs to.
    public int EventId { get; set; }
    public Event? Event { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Optional age limits and distance; null means "no limit / not specified".
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? DistanceKm { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Result> Results { get; set; } = new List<Result>();
}
