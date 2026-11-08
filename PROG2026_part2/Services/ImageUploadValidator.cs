namespace PROG2026_part2.Services;

/// <summary>
/// Checks an uploaded file before it is sent to Azure Blob Storage:
/// it must exist, be at most 5 MB and be a JPG, PNG, GIF or WEBP image.
/// </summary>
public static class ImageUploadValidator
{
    /// <summary>Largest file we accept (5 megabytes).</summary>
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private static readonly string[] AllowedContentTypes =
        { "image/jpeg", "image/png", "image/gif", "image/webp" };

    /// <summary>
    /// Returns an error message when the file is not acceptable, or null when it is fine.
    /// </summary>
    public static string? Validate(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return "Please choose an image file to upload.";
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return "The image is too large. The maximum size is 5 MB.";
        }

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension) ||
            !AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return "Only JPG, PNG, GIF or WEBP images are allowed.";
        }

        return null;
    }
}
