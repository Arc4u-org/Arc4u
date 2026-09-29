namespace Arc4u.Configuration;

/// <summary>
/// The exception thrown when a configuration is missing or invalid.
/// </summary>
/// <param name="message">The message that describes the configuration error.</param>
public class ConfigurationException(string message) : Exception(message)
{
}
