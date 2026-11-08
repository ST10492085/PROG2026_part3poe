namespace PROG2026_part3.Helpers;

/// <summary>
/// Checks an image BEFORE it is sent to the API, so the user gets an instant, friendly message.
/// (The API checks again, because the API must never trust the web app.)
/// </summary>
public static class ImageFileChecker
{
    private const long MaxSizeInBytes = 5 * 1024 * 1024;   // 5 MB

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    /// <summary>Returns an error message, or null when the file is fine (or no file was chosen).</summary>
    public static string? Check(IFormFile? file)
    {
        // No file chosen is fine: the image is optional on the event form.
        if (file == null || file.Length == 0)
        {
            return null;
        }

        if (file.Length > MaxSizeInBytes)
        {
            return "The image is too large. The maximum size is 5 MB.";
        }

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            return "Only JPG, PNG, GIF or WEBP images are allowed.";
        }

        return null;
    }
}
