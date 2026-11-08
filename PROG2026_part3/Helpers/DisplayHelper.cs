namespace PROG2026_part3.Helpers;

/// <summary>
/// Small helper methods used by the views: statuses, badge colours and number formatting.
/// They contain no web code, which makes them easy to unit test.
/// </summary>
public static class DisplayHelper
{
    /// <summary>Status of an EVENT: "Upcoming", "Today" or "Completed".</summary>
    public static string EventStatus(DateTime eventDate, DateTime now)
    {
        if (eventDate.Date == now.Date)
        {
            return "Today";
        }

        return eventDate.Date > now.Date ? "Upcoming" : "Completed";
    }

    /// <summary>
    /// Status of a participant's ENROLMENT:
    ///   "Completed"       - the organiser has captured a result,
    ///   "Awaiting result" - the event is over but there is no result yet,
    ///   "Confirmed"       - the event has not happened yet.
    /// </summary>
    public static string EnrolmentStatus(DateTime eventDate, bool hasResult, DateTime now)
    {
        if (hasResult)
        {
            return "Completed";
        }

        return eventDate.Date < now.Date ? "Awaiting result" : "Confirmed";
    }

    /// <summary>Maps a status to the CSS class (colour) defined in site.css.</summary>
    public static string StatusBadgeClass(string status)
    {
        return status switch
        {
            "Upcoming" => "badge-upcoming",
            "Confirmed" => "badge-upcoming",
            "Today" => "badge-today",
            "Awaiting result" => "badge-awaiting",
            "Completed" => "badge-completed",
            _ => "badge-default"
        };
    }

    /// <summary>Maps an event type (Run / Walk / Cycle) to its colour class.</summary>
    public static string EventTypeBadgeClass(string eventType)
    {
        return eventType switch
        {
            "Run" => "type-run",
            "Walk" => "type-walk",
            "Cycle" => "type-cycle",
            _ => "badge-default"
        };
    }

    /// <summary>Turns 1 into "1st", 2 into "2nd", 47 into "47th", 112 into "112th" ...</summary>
    public static string Ordinal(int number)
    {
        int lastTwoDigits = number % 100;

        // 11th, 12th and 13th are special: they do not follow the 1st/2nd/3rd rule.
        if (lastTwoDigits >= 11 && lastTwoDigits <= 13)
        {
            return number + "th";
        }

        switch (number % 10)
        {
            case 1: return number + "st";
            case 2: return number + "nd";
            case 3: return number + "rd";
            default: return number + "th";
        }
    }

    /// <summary>Formats a finish time as hh:mm:ss, e.g. 00:45:30.</summary>
    public static string FormatFinishTime(TimeSpan time)
    {
        int hours = (int)time.TotalHours;
        return $"{hours:00}:{time.Minutes:00}:{time.Seconds:00}";
    }
}
