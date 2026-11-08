using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PROG2026_part3.Helpers;

namespace PROG2026_part3.Filters;

/// <summary>
/// Protects a controller or action using the MVC session.
///
///   [SessionAuthorize]                 - any logged-in user
///   [SessionAuthorize("Organizer")]    - only Organizers
///   [SessionAuthorize("Participant")]  - only Participants
///
/// Not logged in  -> redirected to the Login page.
/// Wrong role     -> redirected to the "Access denied" page.
/// </summary>
public class SessionAuthorizeAttribute : ActionFilterAttribute
{
    private readonly string? _requiredRole;

    public SessionAuthorizeAttribute(string? requiredRole = null)
    {
        _requiredRole = requiredRole;
    }

    // Runs BEFORE the action method. Setting context.Result stops the action from running.
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ISession session = context.HttpContext.Session;

        if (!session.IsLoggedIn())
        {
            // Remember the page they wanted so we can send them back after login.
            string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;

            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (_requiredRole != null && session.GetRole() != _requiredRole)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
