using System.Net;
using System.Net.Http.Json;
using PROG2026_part2.DTOs;

namespace PROG2026_part2.Tests;

/// <summary>
/// Event management tests: Organizers can create, update and delete their events,
/// everyone can view events, and invalid or unauthorised requests are rejected.
/// </summary>
public class EventManagementTests : ApiTestBase
{
    public EventManagementTests(RaceDayApiFactory factory) : base(factory) { }

    // ------------------------------------------------------------------
    // Success scenarios
    // ------------------------------------------------------------------

    [Fact]
    public async Task Organizer_CanCreateEvent_AndIsRecordedAsOwner()
    {
        var (client, organizer, _) = await LoginAsOrganizerAsync();

        EventResponse created = await CreateEventAsync(client, "Spring Fun Run");

        Assert.Equal("Spring Fun Run", created.Name);
        Assert.Equal("Run", created.EventType);
        Assert.Equal(organizer.UserId, created.OrganizerId);
    }

    [Theory]
    [InlineData("run", "Run")]
    [InlineData("WALK", "Walk")]
    [InlineData("Cycle", "Cycle")]
    public async Task Organizer_CanCreateEvent_OfEachSupportedType(string sent, string expected)
    {
        var (client, _, _) = await LoginAsOrganizerAsync();

        EventResponse created = await CreateEventAsync(client, "Typed Event", sent);

        Assert.Equal(expected, created.EventType);
    }

    [Fact]
    public async Task Organizer_CanUpdateOwnEvent_AndOnlySuppliedFieldsChange()
    {
        var (client, _, _) = await LoginAsOrganizerAsync();
        EventResponse created = await CreateEventAsync(client, "Original Name");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/events/{created.Id}",
            new UpdateEventRequest { Name = "Renamed Event", Location = "New Venue" });
        EventResponse? updated = await response.Content.ReadFromJsonAsync<EventResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Renamed Event", updated!.Name);
        Assert.Equal("New Venue", updated.Location);
        Assert.Equal(created.Description, updated.Description);   // untouched field
    }

    [Fact]
    public async Task Organizer_CanDeleteOwnEvent()
    {
        var (client, _, _) = await LoginAsOrganizerAsync();
        EventResponse created = await CreateEventAsync(client);

        HttpResponseMessage delete = await client.DeleteAsync($"/api/events/{created.Id}");
        HttpResponseMessage getAfterDelete = await client.GetAsync($"/api/events/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CanViewEventList_AndSingleEvent()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        EventResponse created = await CreateEventAsync(organizerClient, "Public Event");
        HttpClient anonymous = CreateClient();

        HttpResponseMessage list = await anonymous.GetAsync("/api/events");
        List<EventResponse>? events = await list.Content.ReadFromJsonAsync<List<EventResponse>>();

        HttpResponseMessage single = await anonymous.GetAsync($"/api/events/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains(events!, e => e.Id == created.Id);
        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewEvents()
    {
        var (participantClient, _, _) = await LoginAsParticipantAsync();

        HttpResponseMessage response = await participantClient.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // Failure scenarios
    // ------------------------------------------------------------------

    [Fact]
    public async Task CreateEvent_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient anonymous = CreateClient();

        HttpResponseMessage response = await anonymous.PostAsJsonAsync("/api/events", new CreateEventRequest
        {
            Name = "Anon",
            Description = "x",
            EventDate = DateTime.UtcNow.AddDays(1),
            Location = "x",
            DistanceKm = 5,
            EventType = "Run"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidEventType_ReturnsBadRequest()
    {
        var (client, _, _) = await LoginAsOrganizerAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/events", new CreateEventRequest
        {
            Name = "Swim Event",
            Description = "x",
            EventDate = DateTime.UtcNow.AddDays(1),
            Location = "x",
            DistanceKm = 5,
            EventType = "Swim"   // only Run, Walk or Cycle are allowed
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithMissingName_ReturnsBadRequest()
    {
        var (client, _, _) = await LoginAsOrganizerAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/events", new CreateEventRequest
        {
            Name = "",
            Description = "x",
            EventDate = DateTime.UtcNow.AddDays(1),
            Location = "x",
            DistanceKm = 5,
            EventType = "Run"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithZeroDistance_ReturnsBadRequest()
    {
        var (client, _, _) = await LoginAsOrganizerAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/events", new CreateEventRequest
        {
            Name = "Zero",
            Description = "x",
            EventDate = DateTime.UtcNow.AddDays(1),
            Location = "x",
            DistanceKm = 0,   // must be at least 0.1
            EventType = "Run"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_ThatDoesNotExist_ReturnsNotFound()
    {
        HttpClient anonymous = CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_ThatDoesNotExist_ReturnsNotFound()
    {
        var (client, _, _) = await LoginAsOrganizerAsync();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            "/api/events/999999",
            new UpdateEventRequest { Name = "Ghost" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_WithInvalidEventType_ReturnsBadRequest()
    {
        var (client, _, _) = await LoginAsOrganizerAsync();
        EventResponse created = await CreateEventAsync(client);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/events/{created.Id}",
            new UpdateEventRequest { EventType = "Swim" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organizer_CannotUpdateAnotherOrganizersEvent()
    {
        var (ownerClient, _, _) = await LoginAsOrganizerAsync();
        var (otherClient, _, _) = await LoginAsOrganizerAsync();
        EventResponse created = await CreateEventAsync(ownerClient);

        HttpResponseMessage response = await otherClient.PutAsJsonAsync(
            $"/api/events/{created.Id}",
            new UpdateEventRequest { Name = "Hijacked" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organizer_CannotDeleteAnotherOrganizersEvent()
    {
        var (ownerClient, _, _) = await LoginAsOrganizerAsync();
        var (otherClient, _, _) = await LoginAsOrganizerAsync();
        EventResponse created = await CreateEventAsync(ownerClient);

        HttpResponseMessage response = await otherClient.DeleteAsync($"/api/events/{created.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_ThatHasEnrollments_ReturnsConflict()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (participantClient, _, _) = await LoginAsParticipantAsync();
        EventResponse created = await CreateEventAsync(organizerClient);
        CategoryResponse category = await CreateCategoryAsync(organizerClient, created.Id);
        await EnrolAsync(participantClient, created.Id, category.Id);

        HttpResponseMessage response = await organizerClient.DeleteAsync($"/api/events/{created.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
