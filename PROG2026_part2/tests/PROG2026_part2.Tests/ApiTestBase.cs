using System.Net.Http.Json;
using PROG2026_part2.DTOs;

namespace PROG2026_part2.Tests;

/// <summary>
/// Shared helpers used by every test class: creating clients, registering/logging in users
/// and creating events/categories. Keeping them here keeps the individual tests short and readable.
/// </summary>
public abstract class ApiTestBase : IClassFixture<RaceDayApiFactory>
{
    protected const string Password = "Password123!";

    protected readonly RaceDayApiFactory Factory;

    protected ApiTestBase(RaceDayApiFactory factory)
    {
        Factory = factory;
    }

    /// <summary>
    /// Creates a new HttpClient. Each client has its OWN cookie jar, so it behaves like a separate
    /// browser: an anonymous client has no session, a logged-in client keeps its session cookie.
    /// </summary>
    protected HttpClient CreateClient() => Factory.CreateClient();

    /// <summary>
    /// Builds an email address that is unique per call so tests never clash on the unique-email rule.
    /// </summary>
    protected static string UniqueEmail(string prefix) =>
        $"{prefix}.{Guid.NewGuid():N}@example.test";

    /// <summary>
    /// Registers a new user with the given role and logs them in.
    /// Returns the client (holding the session cookie) and the user's details.
    /// </summary>
    protected async Task<(HttpClient Client, AuthResponse User, string Email)> RegisterAndLoginAsync(string role)
    {
        HttpClient client = CreateClient();
        string email = UniqueEmail(role.ToLowerInvariant());

        var register = new RegisterRequest
        {
            FirstName = "Test",
            LastName = role,
            Email = email,
            Password = Password,
            Role = role
        };

        HttpResponseMessage registerResponse =
            await client.PostAsJsonAsync("/api/auth/register", register);
        registerResponse.EnsureSuccessStatusCode();

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Email = email, Password = Password });
        loginResponse.EnsureSuccessStatusCode();

        AuthResponse? user = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(user);

        return (client, user!, email);
    }

    /// <summary>Registers and logs in an Organizer.</summary>
    protected Task<(HttpClient Client, AuthResponse User, string Email)> LoginAsOrganizerAsync() =>
        RegisterAndLoginAsync("Organizer");

    /// <summary>Registers and logs in a Participant.</summary>
    protected Task<(HttpClient Client, AuthResponse User, string Email)> LoginAsParticipantAsync() =>
        RegisterAndLoginAsync("Participant");

    /// <summary>
    /// Creates an event through the API using the supplied (Organizer) client.
    /// </summary>
    protected static async Task<EventResponse> CreateEventAsync(
        HttpClient organizerClient,
        string name = "Test Event",
        string eventType = "Run")
    {
        var request = new CreateEventRequest
        {
            Name = name,
            Description = "Created by a unit test.",
            EventDate = DateTime.UtcNow.AddDays(14),
            Location = "Test Stadium",
            DistanceKm = 10,
            EventType = eventType
        };

        HttpResponseMessage response = await organizerClient.PostAsJsonAsync("/api/events", request);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        EventResponse? created = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.NotNull(created);
        return created!;
    }

    /// <summary>
    /// Creates a category for an event through the API using the supplied (Organizer) client.
    /// </summary>
    protected static async Task<CategoryResponse> CreateCategoryAsync(
        HttpClient organizerClient,
        int eventId,
        string name = "10K Open")
    {
        var request = new CreateCategoryRequest
        {
            Name = name,
            Description = "Created by a unit test.",
            MinAge = 18,
            MaxAge = 99,
            DistanceKm = 10
        };

        HttpResponseMessage response = await organizerClient.PostAsJsonAsync(
            $"/api/events/{eventId}/categories", request);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        CategoryResponse? created = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(created);
        return created!;
    }

    /// <summary>
    /// Enrols the logged-in Participant client in an event/category through the API.
    /// </summary>
    protected static async Task<HttpResponseMessage> EnrolAsync(
        HttpClient participantClient,
        int eventId,
        int categoryId)
    {
        return await participantClient.PostAsJsonAsync(
            $"/api/events/{eventId}/enrollments",
            new CreateEnrollmentRequest { CategoryId = categoryId });
    }
}
