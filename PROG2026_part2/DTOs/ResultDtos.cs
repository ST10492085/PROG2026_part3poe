using System.ComponentModel.DataAnnotations;

namespace PROG2026_part2.DTOs;

/// <summary>
/// Data an Organizer sends to capture a Participant's result.
/// </summary>
public class CreateResultRequest
{
    /// <summary>The ID of the Participant who finished (must be enrolled in the category).</summary>
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    /// <summary>The category the participant raced in.</summary>
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    /// <summary>Finish time as hh:mm:ss, e.g. "00:45:30". Must be greater than zero.</summary>
    public TimeSpan FinishTime { get; set; }

    /// <summary>Finishing position (1 = first). Must be greater than zero.</summary>
    [Range(1, int.MaxValue)]
    public int Position { get; set; }
}

/// <summary>
/// Data used to correct an existing result.
/// </summary>
public class UpdateResultRequest
{
    /// <summary>Finish time as hh:mm:ss. Must be greater than zero.</summary>
    public TimeSpan FinishTime { get; set; }

    /// <summary>Finishing position (1 = first). Must be greater than zero.</summary>
    [Range(1, int.MaxValue)]
    public int Position { get; set; }
}

/// <summary>
/// A result as returned by the API.
/// </summary>
public class ResultResponse
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int EventId { get; set; }

    public int CategoryId { get; set; }

    public string ParticipantName { get; set; } = string.Empty;

    public string EventName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public TimeSpan FinishTime { get; set; }

    public int Position { get; set; }

    public DateTime RecordedAt { get; set; }
}
