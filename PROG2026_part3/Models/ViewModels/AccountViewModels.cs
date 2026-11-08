using System.ComponentModel.DataAnnotations;

namespace PROG2026_part3.Models.ViewModels;

// View models are the classes the HTML forms are bound to.
// The [Required] / [EmailAddress] ... attributes give the user instant validation messages.

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>The page the user originally wanted (set when they were redirected to the login page).</summary>
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your first name.")]
    [StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your last name.")]
    [StringLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a password.")]
    [MinLength(6, ErrorMessage = "The password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare("Password", ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>"Organizer" or "Participant".</summary>
    [Required(ErrorMessage = "Please choose a role.")]
    [Display(Name = "I am a...")]
    public string Role { get; set; } = "Participant";
}

public class ProfileViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime MemberSince { get; set; }
    public string? ProfilePictureUrl { get; set; }

    [Required(ErrorMessage = "Please enter your first name.")]
    [StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your last name.")]
    [StringLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    public string? Phone { get; set; }

    public string? Address { get; set; }
}
