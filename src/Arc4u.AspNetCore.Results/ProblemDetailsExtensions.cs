using Arc4u.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Arc4u.AspNetCore.Results;
/// <summary>
/// Fluent helpers to fill the members and extensions of a <see cref="ProblemDetails"/>.
/// </summary>
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// Sets <see cref="ProblemDetails.Status"/>.
    /// </summary>
    /// <param name="problemDetails">The problem details to complete.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>The same <paramref name="problemDetails"/>, to chain calls.</returns>
    public static ProblemDetails WithStatusCode(this ProblemDetails problemDetails, int statusCode)
    {
        problemDetails.Status = statusCode;
        return problemDetails;
    }

    /// <summary>
    /// Sets <see cref="ProblemDetails.Title"/>.
    /// </summary>
    /// <param name="problemDetails">The problem details to complete.</param>
    /// <param name="title">The title.</param>
    /// <returns>The same <paramref name="problemDetails"/>, to chain calls.</returns>
    public static ProblemDetails WithTitle(this ProblemDetails problemDetails, string title)
    {
        problemDetails.Title = title;
        return problemDetails;
    }

    /// <summary>
    /// Sets <see cref="ProblemDetails.Type"/> from a URI.
    /// </summary>
    /// <param name="problemDetails">The problem details to complete.</param>
    /// <param name="type">The problem type URI.</param>
    /// <returns>The same <paramref name="problemDetails"/>, to chain calls.</returns>
    public static ProblemDetails WithType(this ProblemDetails problemDetails, Uri type)
    {
        problemDetails.Type = type.ToString();
        return problemDetails;
    }

    /// <summary>
    /// Sets <see cref="ProblemDetails.Instance"/> when <paramref name="instance"/> is not null or white space.
    /// </summary>
    /// <param name="problemDetails">The problem details to complete.</param>
    /// <param name="instance">The instance URI reference.</param>
    /// <returns>The same <paramref name="problemDetails"/>, to chain calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="problemDetails"/> is <see langword="null"/>.</exception>
    public static ProblemDetails WithInstance(this ProblemDetails problemDetails, string? instance)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);

        if (!string.IsNullOrWhiteSpace(instance))
        {
            problemDetails.Instance = instance;
        }

        return problemDetails;
    }

    /// <summary>
    /// Sets <see cref="ProblemDetails.Detail"/>.
    /// </summary>
    /// <param name="problemDetails">The problem details to complete.</param>
    /// <param name="detail">The human readable explanation.</param>
    /// <returns>The same <paramref name="problemDetails"/>, to chain calls.</returns>
    public static ProblemDetails WithDetail(this ProblemDetails problemDetails, string detail)
    {
        problemDetails.Detail = detail;
        return problemDetails;
    }

    /// <summary>
    /// Adds or replaces the <c>Code</c> entry of <see cref="ProblemDetails.Extensions"/> when <paramref name="code"/> is not null or white space.
    /// </summary>
    /// <param name="problemDetails">The problem details to complete.</param>
    /// <param name="code">The code to expose.</param>
    /// <returns>The same <paramref name="problemDetails"/>, to chain calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="problemDetails"/> is <see langword="null"/>.</exception>
    public static ProblemDetails WithCode(this ProblemDetails problemDetails, string code)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);

        if (!string.IsNullOrWhiteSpace(code) && null != problemDetails.Extensions)
        {
            problemDetails.Extensions!.AddOrReplace("Code", code);
        }

        return problemDetails;
    }

    /// <summary>
    /// Adds or replaces the <c>Severity</c> entry of <see cref="ProblemDetails.Extensions"/> when <paramref name="severity"/> is not null or white space.
    /// </summary>
    /// <param name="problemDetails">The problem details to complete.</param>
    /// <param name="severity">The severity to expose.</param>
    /// <returns>The same <paramref name="problemDetails"/>, to chain calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="problemDetails"/> is <see langword="null"/>.</exception>
    public static ProblemDetails WithSeverity(this ProblemDetails problemDetails, string severity)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);

        if (!string.IsNullOrWhiteSpace(severity) && null != problemDetails.Extensions)
        {
            problemDetails.Extensions!.AddOrReplace("Severity", severity);
        }

        return problemDetails;
    }

    /// <summary>
    /// Adds or replaces an entry of <see cref="ProblemDetails.Extensions"/> when <paramref name="key"/> is not null or white space and <paramref name="value"/> is not <see langword="null"/>.
    /// </summary>
    /// <param name="problemDetails">The problem details to complete.</param>
    /// <param name="key">The extension name.</param>
    /// <param name="value">The extension value.</param>
    /// <returns>The same <paramref name="problemDetails"/>, to chain calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="problemDetails"/> is <see langword="null"/>.</exception>
    public static ProblemDetails WithMetadata(this ProblemDetails problemDetails, string key, object value)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);

        if (!string.IsNullOrWhiteSpace(key) && null != problemDetails.Extensions && null != value)
        {
            problemDetails.Extensions!.AddOrReplace(key, value);
        }

        return problemDetails;
    }
}

