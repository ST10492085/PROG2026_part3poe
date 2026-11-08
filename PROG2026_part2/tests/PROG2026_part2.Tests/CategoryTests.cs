using System.Net;
using System.Net.Http.Json;
using PROG2026_part2.DTOs;

namespace PROG2026_part2.Tests;

/// <summary>
/// Category tests: Organizers define age/distance categories for their events,
/// everyone can view them, and invalid or unauthorised changes are rejected.
/// </summary>
public class CategoryTests : ApiTestBase
{
    public CategoryTests(RaceDayApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Organizer_CanCreateCategory_ForOwnEvent()
    {
        var (client, _, _) = await LoginAsOrganizerAsync();
        EventResponse raceEvent = await CreateEventAsync(client);

        CategoryResponse category = await CreateCategoryAsync(client, raceEvent.Id, "Under 20");

        Assert.Equal("Under 20", category.Name);
        Assert.Equal(raceEvent.Id, category.EventId);
    }

    [Fact]
    public async Task Anonymous_AndParticipants_CanViewCategories()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (participantClient, _, _) = await LoginAsParticipantAsync();
        HttpClient anonymous = CreateClient();
        EventResponse raceEvent = await CreateEventAsync(organizerClient);
        CategoryResponse category = await CreateCategoryAsync(organizerClient, raceEvent.Id);

        HttpResponseMessage anonymousList = await anonymous.GetAsync($"/api/events/{raceEvent.Id}/categories");
        HttpResponseMessage participantSingle = await participantClient.GetAsync(
            $"/api/events/{raceEvent.Id}/categories/{category.Id}");
        List<CategoryResponse>? list = await anonymousList.Content.ReadFromJsonAsync<List<CategoryResponse>>();

        Assert.Equal(HttpStatusCode.OK, anonymousList.StatusCode);
        Assert.Contains(list!, c => c.Id == category.Id);
        Assert.Equal(HttpStatusCode.OK, participantSingle.StatusCode);
    }

    [Fact]
    public async Task Organizer_CanUpdateAndDeleteCategory()
    {
        var (client, _, _) = await LoginAsOrganizerAsync();
        EventResponse raceEvent = await CreateEventAsync(client);
        CategoryResponse category = await CreateCategoryAsync(client, raceEvent.Id, "Old Name");

        HttpResponseMessage update = await client.PutAsJsonAsync(
            $"/api/events/{raceEvent.Id}/categories/{category.Id}",
            new UpdateCategoryRequest { Name = "New Name", Description = "Changed", MinAge = 20, MaxAge = 30, DistanceKm = 5 });
        CategoryResponse? updated = await update.Content.ReadFromJsonAsync<CategoryResponse>();

        HttpResponseMessage delete = await client.DeleteAsync(
            $"/api/events/{raceEvent.Id}/categories/{category.Id}");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("New Name", updated!.Name);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithDuplicateName_ReturnsConflict()
    {
        var (client, _, _) = await LoginAsOrganizerAsync();
        EventResponse raceEvent = await CreateEventAsync(client);
        await CreateCategoryAsync(client, raceEvent.Id, "Senior");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/events/{raceEvent.Id}/categories",
            new CreateCategoryRequest { Name = "senior" });   // same name, different letter case

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithMinAgeGreaterThanMaxAge_ReturnsBadRequest()
    {
        var (client, _, _) = await LoginAsOrganizerAsync();
        EventResponse raceEvent = await CreateEventAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/events/{raceEvent.Id}/categories",
            new CreateCategoryRequest { Name = "Backwards", MinAge = 60, MaxAge = 20 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithMissingName_ReturnsBadRequest()
    {
        var (client, _, _) = await LoginAsOrganizerAsync();
        EventResponse raceEvent = await CreateEventAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/events/{raceEvent.Id}/categories",
            new CreateCategoryRequest { Name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_ForEventThatDoesNotExist_ReturnsNotFound()
    {
        var (client, _, _) = await LoginAsOrganizerAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/events/999999/categories",
            new CreateCategoryRequest { Name = "Ghost" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organizer_CannotAddCategory_ToAnotherOrganizersEvent()
    {
        var (ownerClient, _, _) = await LoginAsOrganizerAsync();
        var (otherClient, _, _) = await LoginAsOrganizerAsync();
        EventResponse raceEvent = await CreateEventAsync(ownerClient);

        HttpResponseMessage response = await otherClient.PostAsJsonAsync(
            $"/api/events/{raceEvent.Id}/categories",
            new CreateCategoryRequest { Name = "Intruder" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_ThatHasEnrollments_ReturnsConflict()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (participantClient, _, _) = await LoginAsParticipantAsync();
        EventResponse raceEvent = await CreateEventAsync(organizerClient);
        CategoryResponse category = await CreateCategoryAsync(organizerClient, raceEvent.Id);
        await EnrolAsync(participantClient, raceEvent.Id, category.Id);

        HttpResponseMessage response = await organizerClient.DeleteAsync(
            $"/api/events/{raceEvent.Id}/categories/{category.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetCategories_ForEventThatDoesNotExist_ReturnsNotFound()
    {
        HttpClient anonymous = CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync("/api/events/999999/categories");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
