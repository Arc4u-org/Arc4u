namespace Arc4u.Diagnostics;

/// <summary>
/// Gives a name to the categories invented by the application.
/// A category is a <see cref="short"/> flag: the predefined ones are <see cref="MessageCategory.Technical"/> (1), <see cref="MessageCategory.Business"/> (2)
/// and <see cref="MessageCategory.Monitoring"/> (4); an application can use the other bits (8, 16, ...) for its own categories.
/// Once a name is registered, the logger writes the <see cref="LoggingConstants.Category"/> property with that name and the Arc4u sinks,
/// formatter and log stores read it back as the flag.
/// </summary>
/// <remarks>
/// Register the categories before the first entry is written, in every process that writes or reads them (a log reader in another process
/// that does not know the name falls back to <see cref="MessageCategory.Technical"/>). A static constructor on the class that holds
/// the logger extension methods does it at the right time, see <see cref="ILoggerExtensions.Category{T}(Microsoft.Extensions.Logging.ILogger{T}, MessageCategory, string)"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// public static class SecurityLoggerExtensions
/// {
///     public const MessageCategory SecurityCategory = (MessageCategory)8;
///
///     static SecurityLoggerExtensions() => MessageCategoryRegistry.Register("Security", SecurityCategory);
///
///     public static ILoggerWrapper&lt;T&gt; Security&lt;T&gt;(this ILogger&lt;T&gt; logger, [CallerMemberName] string methodName = "") =>
///         logger.Category(SecurityCategory, methodName);
/// }
/// </code>
/// </example>
public static class MessageCategoryRegistry
{
    private static readonly object _lock = new();
    private static Dictionary<string, MessageCategory> _byName = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<MessageCategory, string> _byValue = [];

    /// <summary>Registers the name of a category invented by the application. Registering the same name and value again does nothing.</summary>
    /// <param name="name">The name written in the <see cref="LoggingConstants.Category"/> property (compared without regard to case).</param>
    /// <param name="category">The flag of the category: a positive value that is not one of the predefined <see cref="MessageCategory"/> values.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is empty, a number or the name of a predefined category; <paramref name="category"/> is not positive or is a predefined value;
    /// or the name or the value is already registered with another value or name.
    /// </exception>
    public static void Register(string name, MessageCategory category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        name = name.Trim();

        if (category <= 0)
        {
            throw new ArgumentException($"The category {(short)category} must be a positive value.", nameof(category));
        }
        if (Enum.IsDefined(category))
        {
            throw new ArgumentException($"The category {category} is predefined and cannot be renamed.", nameof(category));
        }
        if (Enum.TryParse<MessageCategory>(name, true, out _))
        {
            throw new ArgumentException($"The name {name} is a predefined category or a number.", nameof(name));
        }

        lock (_lock)
        {
            var nameKnown = _byName.TryGetValue(name, out var registeredCategory);
            var valueKnown = _byValue.TryGetValue(category, out var registeredName);

            if (nameKnown && registeredCategory == category)
            {
                return;
            }
            if (nameKnown)
            {
                throw new ArgumentException($"The name {name} is already registered for the category {(short)registeredCategory}.", nameof(name));
            }
            if (valueKnown)
            {
                throw new ArgumentException($"The category {(short)category} is already registered with the name {registeredName}.", nameof(category));
            }

            // Copy on write: the readers never take the lock.
            _byName = new Dictionary<string, MessageCategory>(_byName, StringComparer.OrdinalIgnoreCase) { [name] = category };
            _byValue = new Dictionary<MessageCategory, string>(_byValue) { [category] = name };
        }
    }

    /// <summary>Gets the category of a name: a predefined name, a registered name or a number.</summary>
    /// <param name="name">The name, compared without regard to case.</param>
    /// <param name="category">The category when the name is known.</param>
    /// <returns><see langword="true"/> when the name is a predefined name, a registered name or a positive number.</returns>
    public static bool TryGetCategory(string? name, out MessageCategory category)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            if (Enum.TryParse(name, true, out category) || _byName.TryGetValue(name.Trim(), out category))
            {
                return category > 0;
            }
        }

        category = default;
        return false;
    }

    /// <summary>Gets the name of a category: its registered name, the name of a predefined category, or its number.</summary>
    /// <param name="category">The category.</param>
    /// <returns>The name written in the <see cref="LoggingConstants.Category"/> property.</returns>
    public static string GetName(MessageCategory category) =>
        _byValue.TryGetValue(category, out var name) ? name : category.ToString();
}
