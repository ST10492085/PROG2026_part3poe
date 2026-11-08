using System.Net;

namespace PROG2026_part3.Services;

/// <summary>
/// The result of one call to the API: did it work, what status code came back, and
/// (when it failed) a message that is safe to show to the user.
/// </summary>
public class ApiResponse
{
    public bool IsSuccess { get; set; }

    public HttpStatusCode StatusCode { get; set; }

    /// <summary>Friendly error text. Empty when the call succeeded.</summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Same as <see cref="ApiResponse"/> but also carries the data the API returned (when there was any).
/// </summary>
public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }
}
