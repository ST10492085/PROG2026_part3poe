using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PROG2026_part3.Helpers;
using PROG2026_part3.Models.Api;

namespace PROG2026_part3.Services;

/// <summary>
/// The ONLY class in the MVC app that talks to the Part 2 API.
/// Controllers call methods like GetAsync / PostAsync and never deal with HTTP details.
///
/// How the login stays alive:
///   1. On login the API answers with a session cookie ("PROG2026.Session=...").
///   2. We keep that cookie in the MVC session (SessionKeys.ApiCookie).
///   3. Every later API call sends that cookie back, so the API knows who is calling.
/// The browser never talks to the API (or to Azure) directly.
/// </summary>
public class ApiService
{
    private const string ApiSessionCookieName = "PROG2026.Session";

    private const string ApiNotReachableMessage =
        "Could not reach the RaceDay API. Please make sure the API is running.";

    // "Web" defaults = camelCase names and case-insensitive reading, exactly what the API uses.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    // The MVC session of the user who is making the current request.
    private ISession? Session => _httpContextAccessor.HttpContext?.Session;

    // ------------------------------------------------------------------
    // Simple methods used by the controllers
    // ------------------------------------------------------------------

    /// <summary>GET: read data. Example: GetAsync&lt;List&lt;EventModel&gt;&gt;("api/events")</summary>
    public Task<ApiResponse<T>> GetAsync<T>(string url)
    {
        return SendAsync<T>(HttpMethod.Get, url, null);
    }

    /// <summary>POST: create something. The body object is converted to JSON.</summary>
    public Task<ApiResponse<T>> PostAsync<T>(string url, object? body)
    {
        return SendAsync<T>(HttpMethod.Post, url, CreateJsonContent(body));
    }

    /// <summary>PUT: update something. The body object is converted to JSON.</summary>
    public Task<ApiResponse<T>> PutAsync<T>(string url, object body)
    {
        return SendAsync<T>(HttpMethod.Put, url, CreateJsonContent(body));
    }

    /// <summary>DELETE: remove something.</summary>
    public async Task<ApiResponse> DeleteAsync(string url)
    {
        return await SendAsync<object>(HttpMethod.Delete, url, null);
    }

    /// <summary>
    /// Sends an image to the API as multipart/form-data (field name "file").
    /// The API then stores it in Azure Blob Storage - the MVC app never contacts Azure.
    /// </summary>
    public async Task<ApiResponse<T>> UploadFileAsync<T>(string url, IFormFile file)
    {
        var form = new MultipartFormDataContent();

        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(fileContent, "file", file.FileName);

        return await SendAsync<T>(HttpMethod.Post, url, form);
    }

    // ------------------------------------------------------------------
    // Login needs special handling because we must capture the API's cookie
    // ------------------------------------------------------------------

    /// <summary>
    /// Logs in through the API. On success the API's session cookie is stored in the MVC session.
    /// </summary>
    public async Task<ApiResponse<AuthResponseModel>> LoginAsync(string email, string password)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
            {
                Content = CreateJsonContent(new { email, password })
            };

            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            ApiResponse<AuthResponseModel> result = await ReadResponseAsync<AuthResponseModel>(response);

            if (result.IsSuccess)
            {
                // Find the "Set-Cookie: PROG2026.Session=...; path=/; httponly" header.
                string? setCookie = null;

                if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookies))
                {
                    setCookie = cookies.FirstOrDefault(cookie => cookie.StartsWith(ApiSessionCookieName + "="));
                }

                if (setCookie == null)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "The API did not return a session cookie.";
                    return result;
                }

                // Keep only "PROG2026.Session=value" (everything before the first ';').
                string cookieValue = setCookie.Split(';')[0];
                Session?.SetString(SessionKeys.ApiCookie, cookieValue);
            }

            return result;
        }
        catch (HttpRequestException)
        {
            return Failure<AuthResponseModel>(ApiNotReachableMessage);
        }
        catch (TaskCanceledException)
        {
            return Failure<AuthResponseModel>(ApiNotReachableMessage);
        }
    }

    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    /// <summary>Turns a C# object into a JSON request body.</summary>
    private static StringContent? CreateJsonContent(object? body)
    {
        if (body == null)
        {
            return null;
        }

        string json = JsonSerializer.Serialize(body, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static ApiResponse<T> Failure<T>(string message)
    {
        return new ApiResponse<T> { IsSuccess = false, ErrorMessage = message };
    }

    /// <summary>Builds the request, attaches the API cookie, sends it and reads the answer.</summary>
    private async Task<ApiResponse<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url) { Content = content };

            // Prove to the API who the user is by sending the cookie it gave us at login.
            string? apiCookie = Session?.GetString(SessionKeys.ApiCookie);

            if (!string.IsNullOrEmpty(apiCookie))
            {
                request.Headers.Add("Cookie", apiCookie);
            }

            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            return await ReadResponseAsync<T>(response);
        }
        catch (HttpRequestException)
        {
            return Failure<T>(ApiNotReachableMessage);
        }
        catch (TaskCanceledException)
        {
            return Failure<T>(ApiNotReachableMessage);
        }
    }

    /// <summary>Converts the HTTP answer into an ApiResponse (data on success, message on failure).</summary>
    private async Task<ApiResponse<T>> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();

        var result = new ApiResponse<T>
        {
            StatusCode = response.StatusCode,
            IsSuccess = response.IsSuccessStatusCode
        };

        if (response.IsSuccessStatusCode)
        {
            // Some calls (DELETE) return no body, and ApiResponse<object> never needs data.
            if (!string.IsNullOrWhiteSpace(body) && typeof(T) != typeof(object))
            {
                try
                {
                    result.Data = JsonSerializer.Deserialize<T>(body, JsonOptions);
                }
                catch (JsonException)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "The API sent a response that could not be read.";
                }
            }

            return result;
        }

        // 401 while we HAD a cookie means the API session expired: log the user out of MVC as well.
        if (response.StatusCode == HttpStatusCode.Unauthorized
            && !string.IsNullOrEmpty(Session?.GetString(SessionKeys.ApiCookie)))
        {
            Session!.Clear();
            result.ErrorMessage = "Your session has expired. Please log in again.";
            return result;
        }

        result.ErrorMessage = BuildErrorMessage(response.StatusCode, body);
        return result;
    }

    /// <summary>
    /// Finds the best message in an error answer. The API sends either plain text
    /// ("Event was not found.") or a JSON "problem details" object with validation errors.
    /// </summary>
    private static string BuildErrorMessage(HttpStatusCode statusCode, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return statusCode switch
            {
                HttpStatusCode.Forbidden => "You do not have permission to do that.",
                HttpStatusCode.NotFound => "The item was not found.",
                HttpStatusCode.Unauthorized => "Please log in to continue.",
                _ => $"The API returned an error ({(int)statusCode})."
            };
        }

        // JSON problem details, e.g. {"title":"Validation failed.","errors":{"Name":["The Name field is required."]}}
        if (body.TrimStart().StartsWith("{"))
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(body);
                JsonElement root = document.RootElement;

                if (root.TryGetProperty("errors", out JsonElement errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = new List<string>();

                    foreach (JsonProperty field in errors.EnumerateObject())
                    {
                        foreach (JsonElement message in field.Value.EnumerateArray())
                        {
                            messages.Add(message.GetString() ?? string.Empty);
                        }
                    }

                    if (messages.Count > 0)
                    {
                        return string.Join(" ", messages);
                    }
                }

                if (root.TryGetProperty("detail", out JsonElement detail) && detail.ValueKind == JsonValueKind.String)
                {
                    return detail.GetString() ?? "The API returned an error.";
                }

                if (root.TryGetProperty("title", out JsonElement title) && title.ValueKind == JsonValueKind.String)
                {
                    return title.GetString() ?? "The API returned an error.";
                }
            }
            catch (JsonException)
            {
                // Not valid JSON after all: fall through and show the raw text.
            }
        }

        // Plain text message (strip the quotes if it was sent as a JSON string).
        return body.Trim().Trim('"');
    }
}
