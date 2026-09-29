using System.Text.RegularExpressions;
using System.Text;
using Arc4u.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Extensions;
using AuthenticationProperties = Microsoft.AspNetCore.Authentication.AuthenticationProperties;
using Arc4u.OAuth2.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Middleware;

/// <summary>
/// A middleware that challenges the OpenID Connect scheme for unauthenticated requests whose path matches one of the
/// <see cref="ForceOpenIdMiddleWareOptions.ForceAuthenticationForPaths"/>. It is meant for the parts of a site used from a browser (Swagger, Hangfire, ...) where the user must be sent to the login page.
/// </summary>
public class ForceOpenIdMiddleWare
{
    private readonly RequestDelegate _next;
    private readonly ForceOpenIdMiddleWareOptions _options;
    private readonly Regex? _pathsRegex;

    /// <summary>Initializes a new instance of the <see cref="ForceOpenIdMiddleWare"/> class.</summary>
    /// <param name="next">The next middleware of the pipeline.</param>
    /// <param name="options">The options listing the paths.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public ForceOpenIdMiddleWare(RequestDelegate next, IOptions<ForceOpenIdMiddleWareOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
        _pathsRegex = PathsRegex(_options.ForceAuthenticationForPaths);
    }

    // Build a prefix-match regex from the configured paths ('*' => '.*'), or null when
    // no paths are configured so the middleware is a no-op. Done once, not per request.
    private static Regex? PathsRegex(IList<string> paths)
    {
        if (paths.Count == 0)
        {
            return null;
        }

        var sb = new StringBuilder();
        sb.Append("^(");

        for (var i = 0; i < paths.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('|');
            }

            // Escape the literal part of the path and translate the '*' wildcard into '.*'.
            var segments = paths[i].Split('*');
            for (var s = 0; s < segments.Length; s++)
            {
                if (s > 0)
                {
                    sb.Append(".*");
                }
                sb.Append(Regex.Escape(segments[s]));
            }
        }

        sb.Append(')');

        return new Regex(
            sb.ToString(),
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }

    /// <summary>Processes the request.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>A task that completes when the request is challenged or the rest of the pipeline is done.</returns>
    public async Task InvokeAsync(HttpContext context, ILogger<ForceOpenIdMiddleWare> logger)
    {
        // if we have some part of the site working like a web page (swagger, hangfire, etc.)
        // and it isn't authenticated, force a login based on the configured start-of-path list.
        if (_pathsRegex is not null &&
            context.User.Identity is { IsAuthenticated: false } &&
            context.Request.Path.HasValue &&
            _pathsRegex.IsMatch(context.Request.Path.Value))
        {
            logger.Technical().LogForceOpenIdConnect();

            // Keep the path and query string (drop only the fragment, which never reaches the
            // server) so the user lands back on the exact page after the login round-trip.
            var requestUri = new Uri(context.Request.GetEncodedUrl());
            var cleanUri = new Uri(requestUri.GetLeftPart(UriPartial.Query));
            if (Uri.TryCreate(_options.RedirectUrlForAuthority, UriKind.Absolute, out var authority))
            {
                cleanUri = new Uri(authority, requestUri.PathAndQuery);
            }

            var properties = new AuthenticationProperties { RedirectUri = cleanUri.ToString() };
            await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, properties).ConfigureAwait(false);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }
}
