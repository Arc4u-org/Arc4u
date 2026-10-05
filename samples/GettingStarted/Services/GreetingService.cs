using Arc4u.Configuration;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Microsoft.Extensions.Options;

namespace GettingStarted.Services;

// <export>
public interface IGreetingService
{
    string Greet(string name);
}

[Export(typeof(IGreetingService)), Scoped]
public sealed class GreetingService(ILogger<GreetingService> logger, IOptions<ApplicationConfig> config) : IGreetingService
{
    public string Greet(string name)
    {
        logger.Business().LogInformation("Greeting {Name}", name);

        return $"Hello {name}, from {config.Value.ApplicationName} ({config.Value.Environment.Name}).";
    }
}
// </export>
