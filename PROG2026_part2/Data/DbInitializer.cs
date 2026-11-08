using Microsoft.EntityFrameworkCore;
using PROG2026_part2.Models;
using PROG2026_part2.Services;

namespace PROG2026_part2.Data;

/// <summary>
/// Creates the database schema and inserts sample data on application start-up.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services)
    {
        // A scope is needed because the DbContext and PasswordService are scoped services.
        using IServiceScope scope = services.CreateScope();

        ApplicationDbContext context =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        PasswordService passwordService =
            scope.ServiceProvider
                .GetRequiredService<PasswordService>();

        // Real SQL Server database: apply the EF Core migrations (Code-First).
        // The unit tests swap in the EF Core in-memory provider, which does not
        // support migrations, so the schema is created directly from the model there.
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        // Prevent duplicate seed records when the application restarts.
        if (await context.Users.AnyAsync())
        {
            return;
        }

        // Passwords are hashed + salted before being stored, never saved in plain text.
        PasswordResult organizerPassword =
            passwordService.CreatePasswordHash("Organizer123!");

        PasswordResult participantPassword =
            passwordService.CreatePasswordHash("Participant123!");

        var organizer = new ApplicationUser
        {
            FirstName = "Sample",
            LastName = "Organizer",
            Email = "organizer@raceday.local",
            PasswordHash = organizerPassword.Hash,
            PasswordSalt = organizerPassword.Salt,
            Role = "Organizer",
            CreatedAt = DateTime.UtcNow
        };

        var participant = new ApplicationUser
        {
            FirstName = "Sample",
            LastName = "Participant",
            Email = "participant@raceday.local",
            PasswordHash = participantPassword.Hash,
            PasswordSalt = participantPassword.Salt,
            Role = "Participant",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(organizer, participant);

        // Saved first so the organizer receives an ID that the event can reference.
        await context.SaveChangesAsync();

        var raceEvent = new Event
        {
            Name = "RaceDay Community Run",
            Description = "A sample event for development purposes.",
            EventDate = DateTime.UtcNow.AddMonths(1),
            Location = "Cape Town Stadium",
            DistanceKm = 10,
            EventType = "Run",
            OrganizerId = organizer.Id
        };

        context.Events.Add(raceEvent);

        // Saved so the event receives an ID that the categories can reference.
        await context.SaveChangesAsync();

        var categories = new List<Category>
        {
            new Category
            {
                EventId = raceEvent.Id,
                Name = "10K Open",
                Description = "Open category for the 10 kilometre event.",
                MinAge = 18,
                MaxAge = 59,
                DistanceKm = 10
            },
            new Category
            {
                EventId = raceEvent.Id,
                Name = "10K Masters",
                Description = "Masters category for participants aged 60 and above.",
                MinAge = 60,
                MaxAge = 120,
                DistanceKm = 10
            }
        };

        context.Categories.AddRange(categories);

        await context.SaveChangesAsync();
    }
}
