using Microsoft.AspNetCore.Http;
using PROG2026_part2.Services;

namespace PROG2026_part2.Tests;

/// <summary>
/// Stand-in for Azure Blob Storage used by the tests. It "uploads" nothing; it just
/// returns a believable URL, so the tests can check the API saves and returns it.
/// </summary>
public class FakeBlobStorageService : IBlobStorageService
{
    public bool IsConfigured => true;

    public Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return Task.FromResult($"https://fake-storage.test/raceday-images/{folder}/{Guid.NewGuid()}{extension}");
    }

    public Task DeleteImageAsync(string? imageUrl)
    {
        return Task.CompletedTask;
    }
}
