using PROG2026_part3.Services;

// Entry point of the RaceDay MVC web application (Part 3).
// This app has NO database of its own: every piece of data comes from the Part 2 API.

var builder = WebApplication.CreateBuilder(args);

// MVC: controllers + Razor views.
builder.Services.AddControllersWithViews(options =>
{
    // By default ASP.NET treats every non-nullable string property as "required". Our forms mark
    // required fields explicitly with [Required], so optional text boxes can safely be left empty.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// ----- Session -----
// The MVC session remembers who is logged in (user ID, role, name) and the API's session cookie.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);   // logged out after 2 hours of inactivity
    options.Cookie.Name = ".RaceDay.Web.Session";  // different name from the API's cookie (PROG2026.Session)
    options.Cookie.HttpOnly = true;                // JavaScript cannot read the cookie
    options.Cookie.IsEssential = true;             // needed for the site to work
});

// ApiService needs to read the current user's session.
builder.Services.AddHttpContextAccessor();

// ----- API client -----
// "ApiSettings:BaseUrl" tells the MVC app where the Part 2 API lives.
//   * running locally   -> http://localhost:5065/   (appsettings.json)
//   * running in Docker -> http://api:8080/         (set by docker-compose.yml)
string apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5065/";

builder.Services
    .AddHttpClient<ApiService>(client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    })
    // UseCookies = false: we add the API's session cookie ourselves (from the MVC session)
    // so every user has their own API session.
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false });

var app = builder.Build();

// Friendly error page outside Development.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();   // serves wwwroot (css, images)
app.UseRouting();
app.UseSession();       // must come before the controllers so they can use the session

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
