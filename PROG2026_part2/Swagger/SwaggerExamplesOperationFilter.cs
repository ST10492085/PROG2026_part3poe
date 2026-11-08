using System.Text.Json;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PROG2026_part2.Swagger;

/// <summary>
/// Adds a ready-made example request body to the main endpoints in Swagger UI,
/// so every POST can be tried with one click ("Try it out" -> "Execute").
/// </summary>
public class SwaggerExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // GET/DELETE endpoints have no request body, so there is nothing to give an example for.
        if (operation.RequestBody == null)
        {
            return;
        }

        // Helper to set a JSON example for the request body
        void SetExample(string json)
        {
            if (operation.RequestBody.Content.TryGetValue("application/json", out var mediaType))
            {
                mediaType.Example = new OpenApiString(json);
            }
        }

        // Pick the example by looking at the endpoint's URL.
        var path = context.ApiDescription.RelativePath?.ToLowerInvariant() ?? string.Empty;

        if (path.Contains("api/auth/register"))
        {
            SetExample(JsonSerializer.Serialize(new
            {
                firstName = "Demo",
                lastName = "Organizer",
                email = "demo.org@example.test",
                password = "Organizer123!",
                role = "Organizer"
            }));
        }
        else if (path.Contains("api/auth/login"))
        {
            SetExample(JsonSerializer.Serialize(new
            {
                email = "demo.org@example.test",
                password = "Organizer123!"
            }));
        }
        else if (path.EndsWith("api/events") && operation.RequestBody != null)
        {
            SetExample(JsonSerializer.Serialize(new
            {
                name = "Sample Event",
                description = "A short description",
                eventDate = DateTime.UtcNow.AddDays(10).ToString("o"),
                location = "Test Stadium",
                distanceKm = 10,
                eventType = "Run"
            }));
        }
        else if (path.Contains("/categories") && operation.RequestBody != null)
        {
            SetExample(JsonSerializer.Serialize(new
            {
                name = "10K Open",
                description = "Open category",
                minAge = 18,
                maxAge = 99,
                distanceKm = 10
            }));
        }
        else if (path.Contains("/enrollments") && operation.RequestBody != null)
        {
            SetExample(JsonSerializer.Serialize(new
            {
                categoryId = 1
            }));
        }
        else if (path.Contains("/results") && operation.RequestBody != null)
        {
            SetExample(JsonSerializer.Serialize(new
            {
                userId = 2,
                categoryId = 1,
                finishTime = "00:30:00",
                position = 1
            }));
        }
    }
}
