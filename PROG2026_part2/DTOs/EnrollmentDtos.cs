using System.ComponentModel.DataAnnotations;

namespace PROG2026_part2.DTOs;

/// <summary>
/// Data a Participant sends to enter an event.
/// </summary>
public class CreateEnrollmentRequest
{
    /// <summary>The ID of the category (belonging to the event) the participant wants to enter.</summary>
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

/// <summary>
/// An enrolment as seen by the Participant who made it.
/// </summary>
public class EnrollmentResponse
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int EventId { get; set; }

    public int CategoryId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public DateTime EnrolledAt { get; set; }
}

/// <summary>
/// An enrolment as seen by an Organizer, including the participant's details.
/// </summary>
public class OrganizerEnrollmentResponse
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string ParticipantName { get; set; } = string.Empty;

    public string ParticipantEmail { get; set; } = string.Empty;

    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public DateTime EnrolledAt { get; set; }
}
