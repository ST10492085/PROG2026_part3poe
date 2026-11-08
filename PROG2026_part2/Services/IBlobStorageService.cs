namespace PROG2026_part2.Services;

/// <summary>
/// Stores image files in cloud storage (Azure Blob Storage).
/// The controllers only depend on this interface, so the unit tests can replace the real
/// Azure implementation with a fake one and never need an Azure account.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>True when an Azure Storage connection string has been configured.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Uploads an image to the blob container and returns its public URL.
    /// </summary>
    /// <param name="file">The uploaded image.</param>
    /// <param name="folder">Virtual folder inside the container, e.g. "events" or "profiles".</param>
    Task<string> UploadImageAsync(IFormFile file, string folder);

    /// <summary>
    /// Deletes a previously uploaded image. Does nothing when the URL is empty or the file is already gone.
    /// </summary>
    Task DeleteImageAsync(string? imageUrl);
}
