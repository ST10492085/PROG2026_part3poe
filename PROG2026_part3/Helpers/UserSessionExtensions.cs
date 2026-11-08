namespace PROG2026_part3.Helpers;

/// <summary>
/// Shortcuts for reading the logged-in user from the session.
/// </summary>
public static class UserSessionExtensions
{
    /// <summary>True when a user ID and role are stored in the session.</summary>
    public static bool IsLoggedIn(this ISession session)
    {
        return session.GetInt32(SessionKeys.UserId) != null
            && !string.IsNullOrEmpty(session.GetString(SessionKeys.Role));
    }

    /// <summary>The logged-in user's role ("Organizer" or "Participant"), or null.</summary>
    public static string? GetRole(this ISession session)
    {
        return session.GetString(SessionKeys.Role);
    }

    /// <summary>The logged-in user's ID, or 0 when nobody is logged in.</summary>
    public static int GetUserId(this ISession session)
    {
        return session.GetInt32(SessionKeys.UserId) ?? 0;
    }
}
