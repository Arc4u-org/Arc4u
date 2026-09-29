using System.Security.Cryptography.X509Certificates;

namespace Arc4u.OAuth2.Net;

/// <summary>An abstraction of an HTTP client that sends requests with a client certificate and additional headers.</summary>
public interface IHttpClient
{
    /// <summary>Gets or sets the client certificate sent with the requests.</summary>
    X509Certificate2 Certificate { get; set; }

    /// <summary>Gets or sets the timeout of the requests.</summary>
    double TimeOut { get; set; }

    /// <summary>Sends a GET request.</summary>
    /// <param name="requestUri">The uri of the request.</param>
    /// <param name="headers">The headers added to the request.</param>
    /// <returns>The response.</returns>
    Task<HttpResponseMessage> GetAsync(Uri requestUri, IDictionary<string, string> headers);

    /// <summary>Sends a POST request whose content is a serialized value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="requestUri">The uri of the request.</param>
    /// <param name="value">The value sent as content.</param>
    /// <param name="headers">The headers added to the request.</param>
    /// <returns>The response.</returns>
    Task<HttpResponseMessage> PostAsync<T>(Uri requestUri, T value, IDictionary<string, string> headers);
    /// <summary>Sends a POST request with a string content.</summary>
    /// <param name="requestUri">The uri of the request.</param>
    /// <param name="content">The content.</param>
    /// <param name="headers">The headers added to the request.</param>
    /// <returns>The response.</returns>
    Task<HttpResponseMessage> PostAsync(Uri requestUri, string content, IDictionary<string, string> headers);

    /// <summary>Sends a PUT request whose content is a serialized value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="requestUri">The uri of the request.</param>
    /// <param name="value">The value sent as content.</param>
    /// <param name="headers">The headers added to the request.</param>
    /// <returns>The response.</returns>
    Task<HttpResponseMessage> PutAsync<T>(Uri requestUri, T value, IDictionary<string, string> headers);
    /// <summary>Sends a PUT request with a string content.</summary>
    /// <param name="requestUri">The uri of the request.</param>
    /// <param name="content">The content.</param>
    /// <param name="headers">The headers added to the request.</param>
    /// <returns>The response.</returns>
    Task<HttpResponseMessage> PutAsync(Uri requestUri, string content, IDictionary<string, string> headers);

    /// <summary>Sends a PATCH request whose content is a serialized value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="requestUri">The uri of the request.</param>
    /// <param name="value">The value sent as content.</param>
    /// <param name="headers">The headers added to the request.</param>
    /// <returns>The response.</returns>
    Task<HttpResponseMessage> PatchAsync<T>(Uri requestUri, T value, IDictionary<string, string> headers);
    /// <summary>Sends a PATCH request with a string content.</summary>
    /// <param name="requestUri">The uri of the request.</param>
    /// <param name="content">The content.</param>
    /// <param name="headers">The headers added to the request.</param>
    /// <returns>The response.</returns>
    Task<HttpResponseMessage> PatchAsync(Uri requestUri, string content, IDictionary<string, string> headers);
}
