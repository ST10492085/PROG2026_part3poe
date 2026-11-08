namespace PROG2026_part3.Models.Api;

// These classes describe the JSON that the Part 2 API sends back.
// Each property name matches a property in the API's DTO classes.

/// <summary>An event (race) with its categories.</summary>
public class EventModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int OrganizerId { get; set; }

    /// <summary>Azure Blob Storage URL of the banner image (null when none was uploaded).</summary>
    public string? BannerImageUrl { get; set; }

    public List<CategoryModel> Categories { get; set; } = new();
}

/// <summary>An age / distance category of an event.</summary>
public class CategoryModel
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? DistanceKm { get; set; }
}

/// <summary>A participant's enrolment as the participant sees it.</summary>
public class EnrollmentModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
}

/// <summary>An enrolment as an organiser sees it (includes the participant's details).</summary>
public class OrganizerEnrollmentModel
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

/// <summary>A race result: finish time and finishing position.</summary>
public class ResultModel
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

/// <summary>The logged-in user's profile.</summary>
public class UserProfileModel
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }

    /// <summary>Azure Blob Storage URL of the profile picture (null when none was uploaded).</summary>
    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>What the API returns after a successful register / login.</summary>
public class AuthResponseModel
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>Numbers shown on the organiser dashboard.</summary>
public class OrganizerDashboardModel
{
    public int UserId { get; set; }
    public string OrganizerName { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public int EnrollmentCount { get; set; }
    public int ResultCount { get; set; }
    public List<OrganizerEventSummaryModel> Events { get; set; } = new();
}

/// <summary>One event line on the organiser dashboard.</summary>
public class OrganizerEventSummaryModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public int EnrollmentCount { get; set; }
    public int ResultCount { get; set; }
}
