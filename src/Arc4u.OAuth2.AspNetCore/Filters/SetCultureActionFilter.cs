using Arc4u.Diagnostics;
using Arc4u.Security.Principal;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.AspNetCore.Filters;
/// <summary>An action filter that sets the UI culture of the current thread to the culture of the profile of the current principal.</summary>
public class SetCultureActionFilter : IAsyncActionFilter
{
    /// <summary>Initializes a new instance of the <see cref="SetCultureActionFilter"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="application">The application context giving the current principal.</param>
    public SetCultureActionFilter(ILogger logger, IApplicationContext application)
    {
        _logger = logger;
        _application = application;
    }

    private readonly ILogger _logger;
    private readonly IApplicationContext _application;

    /// <inheritdoc/>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (_application?.Principal?.Profile is not null)
        {
            Thread.CurrentThread.CurrentUICulture = _application.Principal.Profile.CurrentCulture;

            if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                if (null != descriptor.MethodInfo?.DeclaringType)
                {
                    _logger.Technical(descriptor.MethodInfo?.DeclaringType!, descriptor.MethodInfo?.Name ?? "MethodInfo Name")
                           .LogThreadCultureName(_application.Principal.Profile.CurrentCulture.Name);
                }
            }
        }

        await next().ConfigureAwait(false);
    }
}
