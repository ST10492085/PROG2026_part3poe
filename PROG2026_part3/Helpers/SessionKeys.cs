namespace PROG2026_part3.Helpers;

/// <summary>
/// The names of the values we keep in the MVC session.
/// Using constants avoids typing mistakes ("UserID" vs "UserId").
/// </summary>
public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string FullName = "FullName";

    /// <summary>The API's own session cookie, e.g. "PROG2026.Session=abc123". Sent with every API call.</summary>
    public const string ApiCookie = "ApiCookie";
}
