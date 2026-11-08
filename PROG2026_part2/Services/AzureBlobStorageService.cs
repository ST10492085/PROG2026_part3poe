using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace PROG2026_part2.Services;

/// <summary>
/// Real implementation that saves images in an Azure Blob Storage container.
///
/// Settings (appsettings.json / environment variables / user-secrets):
///   AzureBlobStorage:ConnectionString  - the storage account connection string (keep it secret!)
///   AzureBlobStorage:ContainerName     - container to use (default "raceday-images")
///
/// The MVC web app never talks to Azure. It sends the file to the API and the API uploads it here.
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient? _container;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        IConfiguration configuration,
        ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;

        string? connectionString = configuration["AzureBlobStorage:ConnectionString"];
        string containerName = configuration["AzureBlobStorage:ContainerName"] ?? "raceday-images";

        // Without a connection string the API still starts; the upload endpoints answer 503 instead.
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _container = new BlobContainerClient(connectionString, containerName);
        }
    }

    public bool IsConfigured => _container != null;

    public async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        if (_container == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured.");
        }

        // Create the container the first time. "Blob" access = anyone with the URL can view the
        // image (needed so the browser can show it), but nobody can list the container.
        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob);

        // A random name stops two uploads with the same file name overwriting each other.
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string blobName = $"{folder}/{Guid.NewGuid()}{extension}";

        BlobClient blob = _container.GetBlobClient(blobName);

        var options = new BlobUploadOptions
        {
            // The content type makes the browser display the file as an image.
            HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
        };

        using Stream stream = file.OpenReadStream();
        await blob.UploadAsync(stream, options);

        return blob.Uri.ToString();
    }

    public async Task DeleteImageAsync(string? imageUrl)
    {
        if (_container == null || string.IsNullOrWhiteSpace(imageUrl))
        {
            return;
        }

        try
        {
            // BlobUriBuilder splits the URL so we get just the blob's name (e.g. "events/abc.png").
            var blobName = new BlobUriBuilder(new Uri(imageUrl)).BlobName;
            await _container.GetBlobClient(blobName).DeleteIfExistsAsync();
        }
        catch (Exception exception)
        {
            // Failing to clean up an old image must never break the request.
            _logger.LogWarning(exception, "Could not delete blob {Url}", imageUrl);
        }
    }
}
