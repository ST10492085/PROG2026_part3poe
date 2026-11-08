using Microsoft.AspNetCore.Mvc;
using PROG2026_part3.Helpers;
using PROG2026_part3.Models.Api;
using PROG2026_part3.Models.ViewModels;
using PROG2026_part3.Services;

namespace PROG2026_part3.Controllers;

/// <summary>
/// Register, log in and log out. All the real work is done by the API;
/// this controller only keeps the result in the MVC session.
/// </summary>
public class AccountController : BaseController
{
    public AccountController(ApiService api) : base(api)
    {
    }

    // ----- Register -----

    [HttpGet]
    public IActionResult Register()
    {
        // Already logged in? No need to register again.
        if (HttpContext.Session.IsLoggedIn())
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Send the details to the API. The API hashes the password and saves the user.
        var request = new
        {
            firstName = model.FirstName.Trim(),
            lastName = model.LastName.Trim(),
            email = model.Email.Trim(),
            password = model.Password,
            role = model.Role
        };

        ApiResponse<AuthResponseModel> response =
            await Api.PostAsync<AuthResponseModel>("api/auth/register", request);

        if (!response.IsSuccess)
        {
            // For example: "An account with this email already exists."
            ModelState.AddModelError(string.Empty, response.ErrorMessage);
            return View(model);
        }

        ShowSuccess("Your account was created. Please log in.");
        return RedirectToAction("Login");
    }

    // ----- Login -----

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.IsLoggedIn())
        {
            return RedirectToAction("Index", "Home");
        }

        // returnUrl is only filled in when the user was sent here from a protected page.
        if (!string.IsNullOrEmpty(returnUrl))
        {
            ViewBag.Info = "Please log in to continue.";
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Start from a clean session, then ask the API to log in.
        HttpContext.Session.Clear();

        ApiResponse<AuthResponseModel> response =
            await Api.LoginAsync(model.Email.Trim(), model.Password);

        if (!response.IsSuccess || response.Data == null)
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage);
            return View(model);
        }

        // Remember WHO is logged in and WHAT role they have for all later pages.
        AuthResponseModel user = response.Data;
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);
        HttpContext.Session.SetString(SessionKeys.FullName, $"{user.FirstName} {user.LastName}");

        // Go back to the page they wanted (only if it is a page on THIS site).
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        // Each role starts on its own page.
        if (user.Role == "Organizer")
        {
            return RedirectToAction("Dashboard", "Organizer");
        }

        return RedirectToAction("Index", "Home");
    }

    // ----- Logout -----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Ask the API to end its session, then clear ours.
        await Api.PostAsync<object>("api/auth/logout", null);
        HttpContext.Session.Clear();

        ShowSuccess("You have been logged out.");
        return RedirectToAction("Login");
    }

    // ----- Access denied -----

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
