using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PROG2026_part2.Data;
using PROG2026_part2.DTOs;

namespace PROG2026_part2.Tests;

/// <summary>
/// User registration, login, logout, session handling and profile tests
/// (both success and failure scenarios).
/// </summary>
public class AuthTests : ApiTestBase
{
    public AuthTests(RaceDayApiFactory factory) : base(factory) { }

    // ------------------------------------------------------------------
    // Registration
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organizer")]
    public async Task Register_WithValidDetails_ReturnsCreatedWithChosenRole(string role)
    {
        HttpClient client = CreateClient();

        var request = new RegisterRequest
        {
            FirstName = "New",
            LastName = "User",
            Email = UniqueEmail("register"),
            Password = Password,
            Role = role
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        AuthResponse? body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.Equal(role, body!.Role);
        Assert.Equal(request.Email, body.Email);
    }

    [Fact]
    public async Task Register_WithLowerCaseRole_IsNormalised()
    {
        HttpClient client = CreateClient();

        var request = new RegisterRequest
        {
            FirstName = "Case",
            LastName = "Test",
            Email = UniqueEmail("case"),
            Password = Password,
            Role = "organizer"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", request);
        AuthResponse? body = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Organizer", body!.Role);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        HttpClient client = CreateClient();

        var request = new RegisterRequest
        {
            FirstName = "Dup",
            LastName = "User",
            Email = UniqueEmail("duplicate"),
            Password = Password,
            Role = "Participant"
        };

        HttpResponseMessage first = await client.PostAsJsonAsync("/api/auth/register", request);
        HttpResponseMessage second = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_ReturnsBadRequest()
    {
        HttpClient client = CreateClient();

        var request = new RegisterRequest
        {
            FirstName = "Bad",
            LastName = "Role",
            Email = UniqueEmail("badrole"),
            Password = Password,
            Role = "Admin"   // only Organizer and Participant are allowed
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        HttpClient client = CreateClient();

        var request = new RegisterRequest
        {
            FirstName = "Bad",
            LastName = "Email",
            Email = "not-an-email",
            Password = Password,
            Role = "Participant"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithTooShortPassword_ReturnsBadRequest()
    {
        HttpClient client = CreateClient();

        var request = new RegisterRequest
        {
            FirstName = "Short",
            LastName = "Password",
            Email = UniqueEmail("short"),
            Password = "12345",   // minimum length is 6
            Role = "Participant"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotThePlainText()
    {
        HttpClient client = CreateClient();
        string email = UniqueEmail("hashed");

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Hash",
            LastName = "Check",
            Email = email,
            Password = Password,
            Role = "Participant"
        });

        // Look in the database directly to prove the original password was not stored.
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await context.Users.SingleAsync(u => u.Email == email);

        Assert.NotEqual(Password, user.PasswordHash);
        Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
        Assert.False(string.IsNullOrWhiteSpace(user.PasswordSalt));
    }

    // ------------------------------------------------------------------
    // Login / session
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organizer")]
    public async Task Login_WithValidCredentials_ReturnsOkAndStartsSession(string role)
    {
        var (client, user, _) = await RegisterAndLoginAsync(role);

        // The session cookie now lets the client reach a protected endpoint.
        HttpResponseMessage profile = await client.GetAsync("/api/users/me");

        Assert.Equal(role, user.Role);
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var (_, _, email) = await LoginAsParticipantAsync();
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Email = email, Password = "WrongPassword!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Email = UniqueEmail("nobody"), Password = Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMissingPassword_ReturnsBadRequest()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Email = UniqueEmail("nopass"), Password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithSeededOrganizerAccount_Succeeds()
    {
        HttpClient client = CreateClient();

        // The sample organizer inserted by DbInitializer.
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Email = "organizer@raceday.local", Password = "Organizer123!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ClearsSession_SoProtectedEndpointsReturnUnauthorized()
    {
        var (client, _, _) = await LoginAsParticipantAsync();

        HttpResponseMessage logout = await client.PostAsync("/api/auth/logout", null);
        HttpResponseMessage profileAfterLogout = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, profileAfterLogout.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient anonymous = CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ------------------------------------------------------------------
    // User profile (both roles)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organizer")]
    public async Task Profile_CanBeViewedAndUpdated_ByBothRoles(string role)
    {
        var (client, user, email) = await RegisterAndLoginAsync(role);

        HttpResponseMessage get = await client.GetAsync("/api/users/me");
        UserProfileResponse? profile = await get.Content.ReadFromJsonAsync<UserProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(user.UserId, profile!.Id);
        Assert.Equal(email, profile.Email);

        HttpResponseMessage update = await client.PutAsJsonAsync("/api/users/me", new UpdateProfileRequest
        {
            FirstName = "Updated",
            Phone = "0821234567",
            Address = "1 Test Street"
        });
        UserProfileResponse? updated = await update.Content.ReadFromJsonAsync<UserProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Updated", updated!.FirstName);
        Assert.Equal("0821234567", updated.Phone);
        Assert.Equal(role, updated.Role);   // role can never be changed through the profile
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidPhone_ReturnsBadRequest()
    {
        var (client, _, _) = await LoginAsParticipantAsync();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            "/api/users/me",
            new UpdateProfileRequest { Phone = "abc" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithoutLogin_ReturnsUnauthorized()
    {
        HttpClient anonymous = CreateClient();

        HttpResponseMessage response = await anonymous.PutAsJsonAsync(
            "/api/users/me",
            new UpdateProfileRequest { FirstName = "Nope" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
