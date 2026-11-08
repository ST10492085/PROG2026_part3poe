using System.Net;
using System.Net.Http.Json;
using PROG2026_part2.DTOs;

namespace PROG2026_part2.Tests;

/// <summary>
/// Role-based access control tests:
///   * anonymous users are rejected with 401 Unauthorized,
///   * Participants cannot use Organizer functionality (403 Forbidden),
///   * Organizers cannot use Participant functionality (403 Forbidden).
/// </summary>
public class RoleAccessTests : ApiTestBase
{
    public RoleAccessTests(RaceDayApiFactory factory) : base(factory) { }

    // ------------------------------------------------------------------
    // Anonymous (not logged in) -> 401
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("/api/users/me")]
    [InlineData("/api/enrollments/me")]
    [InlineData("/api/results/me")]
    [InlineData("/api/dashboard/participant")]
    [InlineData("/api/dashboard/organizer")]
    [InlineData("/api/organizer/events/1/enrollments")]
    public async Task Anonymous_IsRejected_OnProtectedGetEndpoints(string url)
    {
        HttpClient anonymous = CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotEnrol_ReturnsUnauthorized()
    {
        HttpClient anonymous = CreateClient();

        HttpResponseMessage response = await EnrolAsync(anonymous, 1, 1);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCreateCategory_ReturnsUnauthorized()
    {
        HttpClient anonymous = CreateClient();

        HttpResponseMessage response = await anonymous.PostAsJsonAsync(
            "/api/events/1/categories",
            new CreateCategoryRequest { Name = "Anon" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotRecordResult_ReturnsUnauthorized()
    {
        HttpClient anonymous = CreateClient();

        HttpResponseMessage response = await anonymous.PostAsJsonAsync(
            "/api/events/1/results",
            new CreateResultRequest { UserId = 1, CategoryId = 1, FinishTime = TimeSpan.FromMinutes(30), Position = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // Participant trying Organizer functionality -> 403
    // ------------------------------------------------------------------

    [Fact]
    public async Task Participant_CannotCreateEvent()
    {
        var (participantClient, _, _) = await LoginAsParticipantAsync();

        HttpResponseMessage response = await participantClient.PostAsJsonAsync("/api/events", new CreateEventRequest
        {
            Name = "Should Fail",
            Description = "x",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Nowhere",
            DistanceKm = 5,
            EventType = "Run"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUpdateOrDeleteEvent()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (participantClient, _, _) = await LoginAsParticipantAsync();
        EventResponse created = await CreateEventAsync(organizerClient);

        HttpResponseMessage update = await participantClient.PutAsJsonAsync(
            $"/api/events/{created.Id}",
            new UpdateEventRequest { Name = "Hijacked" });
        HttpResponseMessage delete = await participantClient.DeleteAsync($"/api/events/{created.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateCategory()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (participantClient, _, _) = await LoginAsParticipantAsync();
        EventResponse created = await CreateEventAsync(organizerClient);

        HttpResponseMessage response = await participantClient.PostAsJsonAsync(
            $"/api/events/{created.Id}/categories",
            new CreateCategoryRequest { Name = "Sneaky" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrollmentsAsOrganizer()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (participantClient, _, _) = await LoginAsParticipantAsync();
        EventResponse created = await CreateEventAsync(organizerClient);

        HttpResponseMessage response = await participantClient.GetAsync(
            $"/api/organizer/events/{created.Id}/enrollments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotRecordResults()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (participantClient, participant, _) = await LoginAsParticipantAsync();
        EventResponse created = await CreateEventAsync(organizerClient);
        CategoryResponse category = await CreateCategoryAsync(organizerClient, created.Id);

        HttpResponseMessage response = await participantClient.PostAsJsonAsync(
            $"/api/events/{created.Id}/results",
            new CreateResultRequest
            {
                UserId = participant.UserId,
                CategoryId = category.Id,
                FinishTime = TimeSpan.FromMinutes(20),
                Position = 1
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotOpenOrganizerDashboard()
    {
        var (participantClient, _, _) = await LoginAsParticipantAsync();

        HttpResponseMessage response = await participantClient.GetAsync("/api/dashboard/organizer");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // Organizer trying Participant functionality -> 403
    // ------------------------------------------------------------------

    [Fact]
    public async Task Organizer_CannotEnrolInAnEvent()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        EventResponse created = await CreateEventAsync(organizerClient);
        CategoryResponse category = await CreateCategoryAsync(organizerClient, created.Id);

        HttpResponseMessage response = await EnrolAsync(organizerClient, created.Id, category.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/enrollments/me")]
    [InlineData("/api/results/me")]
    [InlineData("/api/dashboard/participant")]
    public async Task Organizer_CannotUseParticipantOnlyEndpoints(string url)
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();

        HttpResponseMessage response = await organizerClient.GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // The correct role is allowed through
    // ------------------------------------------------------------------

    [Fact]
    public async Task Organizer_CanOpenOrganizerDashboard_AndParticipantCanOpenParticipantDashboard()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (participantClient, _, _) = await LoginAsParticipantAsync();

        HttpResponseMessage organizerDashboard = await organizerClient.GetAsync("/api/dashboard/organizer");
        HttpResponseMessage participantDashboard = await participantClient.GetAsync("/api/dashboard/participant");

        Assert.Equal(HttpStatusCode.OK, organizerDashboard.StatusCode);
        Assert.Equal(HttpStatusCode.OK, participantDashboard.StatusCode);
    }
}
