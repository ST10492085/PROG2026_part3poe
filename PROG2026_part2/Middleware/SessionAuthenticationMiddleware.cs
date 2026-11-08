using System.Security.Claims;

namespace PROG2026_part2.Middleware;

/// <summary>
/// Turns the server-side session into the current user for each request.
/// If the session contains a user ID and role (stored at login), a ClaimsPrincipal with those
/// claims is attached to HttpContext.User, which is what [Authorize(Roles = ...)] checks.
/// If there is no session the request simply stays anonymous.
/// </summary>
public class SessionAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public SessionAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        // Values written by AuthController.Login
        int? userId = httpContext.Session.GetInt32("UserId");
        string? role = httpContext.Session.GetString("Role");

        if (userId.HasValue && !string.IsNullOrWhiteSpace(role))
        {
            // The two claims the controllers rely on: who the user is and what role they have.
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    userId.Value.ToString()),

                new Claim(
                    ClaimTypes.Role,
                    role)
            };

            var identity = new ClaimsIdentity(
                claims,
                authenticationType: "Session");

            // Setting an authenticationType makes Identity.IsAuthenticated == true.
            httpContext.User = new ClaimsPrincipal(identity);
        }

        // Continue to the next middleware (authorization, controllers, ...).
        await _next(httpContext);
    }
}