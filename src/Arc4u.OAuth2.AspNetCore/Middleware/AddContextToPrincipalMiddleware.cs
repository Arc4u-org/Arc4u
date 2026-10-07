using System.Diagnostics;
using System.Globalization;
using Arc4u.Diagnostics;
using Arc4u.Security.Principal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Arc4u.OAuth2.Middleware;
/// <summary>
/// A middleware that enriches the request of an authenticated <see cref="AppPrincipal"/>: it ensures the request carries a <c>traceparent</c> header
/// and sets the current culture of the principal profile from the <c>culture</c> request header.
/// </summary>
public class AddContextToPrincipalMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ActivitySource? _activitySource;

    /// <summary>Initializes a new instance of the <see cref="AddContextToPrincipalMiddleware"/> class.</summary>
    /// <param name="next">The next middleware of the pipeline.</param>
    /// <param name="activitySourceFactory">The factory of the activity source used for telemetry.</param>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public AddContextToPrincipalMiddleware(RequestDelegate next, IActivitySourceFactory activitySourceFactory)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _activitySource = activitySourceFactory.GetArc4u();
    }

    /// <summary>Processes the request.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>A task that completes when the rest of the pipeline is done.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public async Task InvokeAsync(HttpContext context, ILogger<AddContextToPrincipalMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.User is not null && context.User is AppPrincipal principal && principal.Identity is not null && principal.Identity.IsAuthenticated)
        {
            using var activity = _activitySource?.StartActivity("Add context to Arc4u Principal", ActivityKind.Producer);

            // Ensure we have a traceparent.
            if (!context.Request.Headers.ContainsKey("traceparent"))
            {
                // Activity.Current is not null just because we are in the context of an activity.
                context.Request.Headers.Append("traceparent", Activity.Current!.Id);
            }

            // Check for a culture.
            var cultureHeader = context.Request?.Headers?.FirstOrDefault(h => h.Key.Equals("culture", StringComparison.InvariantCultureIgnoreCase));
            if (cultureHeader.HasValue && StringValues.Empty != cultureHeader.Value.Value && cultureHeader.Value.Value.Any())
            {
                try
                {
                    principal.Profile.CurrentCulture = new CultureInfo(cultureHeader.Value.Value[0]!);
                }
                catch (Exception ex)
                {
                    logger.Technical().LogException(ex);
                }
            }
        }

        await _next(context).ConfigureAwait(false);
    }

}
