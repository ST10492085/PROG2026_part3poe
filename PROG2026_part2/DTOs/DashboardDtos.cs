namespace PROG2026_part2.DTOs;

/// <summary>
/// Summary shown on a Participant's dashboard: their enrolments and results.
/// </summary>
public class ParticipantDashboardResponse
{
    public int UserId { get; set; }

    public string ParticipantName { get; set; } = string.Empty;

    public int EnrollmentCount { get; set; }

    public int ResultCount { get; set; }

    public List<EnrollmentResponse> Enrollments { get; set; } = new();

    public List<ResultResponse> Results { get; set; } = new();
}

/// <summary>
/// Summary shown on an Organizer's dashboard: their events and totals.
/// </summary>
public class OrganizerDashboardResponse
{
    public int UserId { get; set; }

    public string OrganizerName { get; set; } = string.Empty;

    public int EventCount { get; set; }

    /// <summary>Total enrolments across all of the organizer's events.</summary>
    public int EnrollmentCount { get; set; }

    /// <summary>Total results captured across all of the organizer's events.</summary>
    public int ResultCount { get; set; }

    public List<OrganizerEventSummary> Events { get; set; } = new();
}

/// <summary>
/// One event line on the Organizer dashboard.
/// </summary>
public class OrganizerEventSummary
{
    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    public string Location { get; set; } = string.Empty;

    public int EnrollmentCount { get; set; }

    public int ResultCount { get; set; }
}
