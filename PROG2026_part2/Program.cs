using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PROG2026_part2.Authentication;
using PROG2026_part2.Data;
using PROG2026_part2.Middleware;
using PROG2026_part2.Services;
using PROG2026_part2.Swagger;

// Entry point of the RaceDay Web API.
// Everything below is split into two phases:
//   1. Service registration (builder.Services...)  -> what the app is made of
//   2. Middleware pipeline  (app.Use...)           -> how each request flows through it

var builder = WebApplication.CreateBuilder(args);

// -------------------------
// MVC controllers and validation
// -------------------------
builder.Services
    .AddControllers(options =>
    {
        // Every endpoint can fail in these ways, so the status codes are added to all
        // endpoints once here. This makes Swagger list them for every operation.
        var commonStatusCodes = new[]
        {
            StatusCodes.Status400BadRequest,
            StatusCodes.Status401Unauthorized,
            StatusCodes.Status403Forbidden,
            StatusCodes.Status404NotFound,
            StatusCodes.Status409Conflict,
            StatusCodes.Status500InternalServerError
        };

        foreach (var code in commonStatusCodes)
        {
            options.Filters.Add(new ProducesResponseTypeAttribute(code));
        }
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Replace the default model-validation response with a consistent
        // RFC 7807 "problem details" JSON body that also contains a trace ID.
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value != null && entry.Value.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "The supplied value is invalid."
                            : error.ErrorMessage)
                        .ToArray());

            var problemDetails = new ValidationProblemDetails(errors)
            {
                Type = "https://httpstatuses.com/400",
                Title = "Validation failed.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "One or more validation errors occurred.",
                Instance = context.HttpContext.Request.Path
            };

            problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            return new BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });

// -------------------------
// Database (Entity Framework Core, Code-First, SQL Server)
// -------------------------
// The connection string is read from appsettings.json ("DefaultConnection").
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// -------------------------
// Application services
// -------------------------
// Handles password hashing/verification so plain-text passwords are never stored.
builder.Services.AddScoped<PasswordService>();

// Saves uploaded images (event banners, profile pictures) in Azure Blob Storage.
// Registered through its interface so tests can swap in a fake.
builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();

// -------------------------
// Sessions
// -------------------------
// The session stores the logged-in user's ID and role on the server.
// The browser only receives an opaque session cookie.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);   // session expires after 2 hours of inactivity
    options.Cookie.HttpOnly = true;                // cookie cannot be read by JavaScript
    options.Cookie.IsEssential = true;             // required for the API to work, so always sent
    options.Cookie.Name = "PROG2026.Session";
});

// -------------------------
// Authentication / authorization
// -------------------------
// The "Session" scheme lets [Authorize] return 401 (anonymous) and 403 (wrong role).
// The identity itself is created from the session by SessionAuthenticationMiddleware.
builder.Services
    .AddAuthentication(SessionAuthenticationDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
        SessionAuthenticationDefaults.AuthenticationScheme,
        configureOptions: null);

builder.Services.AddAuthorization();

// -------------------------
// Health checks and Swagger
// -------------------------
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RESTful API for managing RaceDay events, categories, enrollments, participants, and results."
    });

    // Allow graders to set a Cookie header in Swagger for session testing.
    options.AddSecurityDefinition("session", new OpenApiSecurityScheme
    {
        Name = "Cookie",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "To test session endpoints using Swagger: set Cookie header to 'PROG2026.Session=<value>' after login."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "session"
                }
            },
            new List<string>()
        }
    });

    // Adds example request bodies so endpoints can be tested with one click.
    options.OperationFilter<SwaggerExamplesOperationFilter>();

    // Include the /// XML comments (summary, param, returns, response) in the Swagger UI.
    string xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// -------------------------
// Database migration + seeding
// -------------------------
// Creates/updates the database and inserts the sample users, event and categories.
try
{
    await DbInitializer.InitializeAsync(app.Services);
}
catch (Exception ex)
{
    Console.WriteLine("Database initialization failed: " + ex.Message);
    throw;
}

// -------------------------
// Global middleware
// -------------------------
// Order matters: the exception handler wraps everything that runs after it.
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

// -------------------------
// Swagger (development only)
// -------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
        options.DocumentTitle = "RaceDay API Documentation";
    });
}

// -------------------------
// HTTPS, sessions, auth
// -------------------------
// In Development the MVC web app calls the API over plain http (http://localhost:5065), and in Docker
// the API only listens on http, so redirection to https is only switched on for other environments.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseSession();                                       // 1. load the session for this request
app.UseMiddleware<SessionAuthenticationMiddleware>();   // 2. build the user (ID + role) from the session
app.UseAuthentication();                                // 3. run the "Session" authentication scheme
app.UseAuthorization();                                 // 4. enforce [Authorize] / [Authorize(Roles = ...)]

// -------------------------
// Endpoints
// -------------------------
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

// Makes the auto-generated Program class visible to the test project
// so WebApplicationFactory<Program> can start the API in memory.
public partial class Program
{
}
