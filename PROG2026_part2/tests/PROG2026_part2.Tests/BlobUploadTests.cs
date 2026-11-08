using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PROG2026_part2.DTOs;

namespace PROG2026_part2.Tests;

/// <summary>
/// Part 3 tests: event banner and profile picture uploads (Azure Blob Storage is replaced by a fake).
/// </summary>
public class BlobUploadTests : ApiTestBase
{
    public BlobUploadTests(RaceDayApiFactory factory) : base(factory) { }

    /// <summary>Builds a multipart/form-data body containing one file in the form field "file".</summary>
    private static MultipartFormDataContent FileBody(string fileName, string contentType, int sizeInBytes = 16)
    {
        var fileContent = new ByteArrayContent(new byte[sizeInBytes]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var body = new MultipartFormDataContent();
        body.Add(fileContent, "file", fileName);
        return body;
    }

    // ------------------------------------------------------------------
    // Event banner
    // ------------------------------------------------------------------

    [Fact]
    public async Task Organizer_CanUploadBanner_AndUrlIsSavedOnTheEvent()
    {
        var (client, _, _) = await LoginAsOrganizerAsync();
        EventResponse raceEvent = await CreateEventAsync(client);

        HttpResponseMessage response = await client.PostAsync(
            $"/api/events/{raceEvent.Id}/banner", FileBody("banner.png", "image/png"));
        EventResponse? updated = await response.Content.ReadFromJsonAsync<EventResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("https://fake-storage.test/", updated!.BannerImageUrl);

        // The URL is stored, so it comes back when the event is loaded again.
        EventResponse? reloaded = await client.GetFromJsonAsync<EventResponse>($"/api/events/{raceEvent.Id}");
        Assert.Equal(updated.BannerImageUrl, reloaded!.BannerImageUrl);
    }

    [Fact]
    public async Task UploadBanner_WithNonImageFile_ReturnsBadRequest()
    {
        var (client, _, _) = await LoginAsOrganizerAsync();
        EventResponse raceEvent = await CreateEventAsync(client);

        HttpResponseMessage response = await client.PostAsync(
            $"/api/events/{raceEvent.Id}/banner", FileBody("notes.txt", "text/plain"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WithFileOver5MB_ReturnsBadRequest()
    {
        var (client, _, _) = await LoginAsOrganizerAsync();
        EventResponse raceEvent = await CreateEventAsync(client);

        HttpResponseMessage response = await client.PostAsync(
            $"/api/events/{raceEvent.Id}/banner",
            FileBody("huge.png", "image/png", 6 * 1024 * 1024));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_ForEventThatDoesNotExist_ReturnsNotFound()
    {
        var (client, _, _) = await LoginAsOrganizerAsync();

        HttpResponseMessage response = await client.PostAsync(
            "/api/events/999999/banner", FileBody("banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUploadBanner()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (participantClient, _, _) = await LoginAsParticipantAsync();
        EventResponse raceEvent = await CreateEventAsync(organizerClient);

        HttpResponseMessage response = await participantClient.PostAsync(
            $"/api/events/{raceEvent.Id}/banner", FileBody("banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organizer_CannotUploadBanner_ForAnotherOrganizersEvent()
    {
        var (ownerClient, _, _) = await LoginAsOrganizerAsync();
        var (otherClient, _, _) = await LoginAsOrganizerAsync();
        EventResponse raceEvent = await CreateEventAsync(ownerClient);

        HttpResponseMessage response = await otherClient.PostAsync(
            $"/api/events/{raceEvent.Id}/banner", FileBody("banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotUploadBanner()
    {
        HttpClient anonymous = CreateClient();

        HttpResponseMessage response = await anonymous.PostAsync(
            "/api/events/1/banner", FileBody("banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // Profile picture (both roles)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organizer")]
    public async Task User_CanUploadProfilePicture_AndUrlAppearsOnTheProfile(string role)
    {
        var (client, _, _) = await RegisterAndLoginAsync(role);

        HttpResponseMessage response = await client.PostAsync(
            "/api/users/me/profile-picture", FileBody("me.jpg", "image/jpeg"));
        UserProfileResponse? updated = await response.Content.ReadFromJsonAsync<UserProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("https://fake-storage.test/", updated!.ProfilePictureUrl);

        UserProfileResponse? reloaded = await client.GetFromJsonAsync<UserProfileResponse>("/api/users/me");
        Assert.Equal(updated.ProfilePictureUrl, reloaded!.ProfilePictureUrl);
    }

    [Fact]
    public async Task UploadProfilePicture_WithNonImageFile_ReturnsBadRequest()
    {
        var (client, _, _) = await LoginAsParticipantAsync();

        HttpResponseMessage response = await client.PostAsync(
            "/api/users/me/profile-picture", FileBody("cv.pdf", "application/pdf"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient anonymous = CreateClient();

        HttpResponseMessage response = await anonymous.PostAsync(
            "/api/users/me/profile-picture", FileBody("me.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
