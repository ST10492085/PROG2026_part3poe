using Microsoft.AspNetCore.Mvc;
using PROG2026_part3.Filters;
using PROG2026_part3.Helpers;
using PROG2026_part3.Models.Api;
using PROG2026_part3.Models.ViewModels;
using PROG2026_part3.Services;

namespace PROG2026_part3.Controllers;

/// <summary>
/// The profile page for BOTH roles: view and update personal information and upload a profile picture.
/// </summary>
[SessionAuthorize]
public class ProfileController : BaseController
{
    public ProfileController(ApiService api) : base(api)
    {
    }

    // ----- View the profile -----

    public async Task<IActionResult> Index()
    {
        ApiResponse<UserProfileModel> response = await Api.GetAsync<UserProfileModel>("api/users/me");

        if (!response.IsSuccess || response.Data == null)
        {
            ShowError(response.ErrorMessage);
            return RedirectToAction("Index", "Home");
        }

        return View(ToViewModel(response.Data));
    }

    // ----- Update personal information -----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // The read-only fields are not posted by the form, so reload them for the page.
            await FillReadOnlyFieldsAsync(model);
            return View("Index", model);
        }

        ApiResponse<UserProfileModel> response = await Api.PutAsync<UserProfileModel>(
            "api/users/me",
            new
            {
                firstName = model.FirstName.Trim(),
                lastName = model.LastName.Trim(),
                phone = model.Phone?.Trim(),
                address = model.Address?.Trim()
            });

        if (!response.IsSuccess || response.Data == null)
        {
            ShowError(response.ErrorMessage);
            return RedirectToAction("Index");
        }

        // Keep the name in the menu bar up to date.
        HttpContext.Session.SetString(
            SessionKeys.FullName,
            $"{response.Data.FirstName} {response.Data.LastName}");

        ShowSuccess("Your profile was updated.");
        return RedirectToAction("Index");
    }

    // ----- Upload a profile picture -----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture == null || picture.Length == 0)
        {
            ShowError("Please choose an image to upload.");
            return RedirectToAction("Index");
        }

        string? problem = ImageFileChecker.Check(picture);

        if (problem != null)
        {
            ShowError(problem);
            return RedirectToAction("Index");
        }

        // The file goes to the API, and the API stores it in Azure Blob Storage.
        ApiResponse<UserProfileModel> response =
            await Api.UploadFileAsync<UserProfileModel>("api/users/me/profile-picture", picture);

        if (response.IsSuccess)
        {
            ShowSuccess("Your profile picture was updated.");
        }
        else
        {
            ShowError(response.ErrorMessage);
        }

        return RedirectToAction("Index");
    }

    // ----- Helpers -----

    private static ProfileViewModel ToViewModel(UserProfileModel profile)
    {
        return new ProfileViewModel
        {
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Email = profile.Email,
            Role = profile.Role,
            Phone = profile.Phone,
            Address = profile.Address,
            ProfilePictureUrl = profile.ProfilePictureUrl,
            MemberSince = profile.CreatedAt
        };
    }

    private async Task FillReadOnlyFieldsAsync(ProfileViewModel model)
    {
        ApiResponse<UserProfileModel> response = await Api.GetAsync<UserProfileModel>("api/users/me");

        if (response.IsSuccess && response.Data != null)
        {
            model.Email = response.Data.Email;
            model.Role = response.Data.Role;
            model.ProfilePictureUrl = response.Data.ProfilePictureUrl;
            model.MemberSince = response.Data.CreatedAt;
        }
    }
}
