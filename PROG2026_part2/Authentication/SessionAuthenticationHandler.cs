using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace PROG2026_part2.Authentication;

/// <summary>
/// Constants for the session-based authentication scheme used by the API.
/// </summary>
public static class SessionAuthenticationDefaults
{
    /// <summary>The name under which the scheme is registered in Program.cs.</summary>
    public const string AuthenticationScheme = "Session";
}

/// <summary>
/// Authentication handler that plugs the server-side session into ASP.NET Core's
/// [Authorize] pipeline.
///
/// The actual identity (user ID + role) is built from the session by
/// <see cref="PROG2026_part2.Middleware.SessionAuthenticationMiddleware"/>.
/// This handler only has two jobs:
///   1. Report that identity as "authenticated" when it exists.
///   2. Tell ASP.NET Core what to send back when access is refused:
///      HTTP 401 (not logged in) or HTTP 403 (logged in with the wrong role).
///
/// Without a registered scheme, ASP.NET Core cannot produce those two responses
/// and throws an exception instead, which would surface as a 500 error.
/// </summary>
public class SessionAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <summary>
    /// Succeeds when the session middleware already attached an authenticated user.
    /// </summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Context.User.Identity?.IsAuthenticated == true)
        {
            var ticket = new AuthenticationTicket(Context.User, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        // No session / not logged in: leave the request anonymous.
        return Task.FromResult(AuthenticateResult.NoResult());
    }

    /// <summary>
    /// Called when an anonymous user requests a protected endpoint -> 401 Unauthorized.
    /// </summary>
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a logged-in user lacks the required role -> 403 Forbidden.
    /// </summary>
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
