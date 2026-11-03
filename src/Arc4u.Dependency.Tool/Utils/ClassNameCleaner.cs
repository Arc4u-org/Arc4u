namespace Arc4u.Dependency.Tool;

/// <summary>
/// Turns a name into a valid C# identifier.
/// </summary>
public static class ClassNameCleaner
{
    /// <summary>
    /// Removes the characters that are not letters, digits or underscores, and prefixes the result with an underscore when it starts with a digit.
    /// </summary>
    /// <param name="className">The name to clean.</param>
    /// <returns>A valid identifier.</returns>
    /// <exception cref="IndexOutOfRangeException">No valid character remains after the cleaning.</exception>
    public static string CleanClassName(string className)
    {
        // Keep only ASCII letters, digits and underscores
        var cleanedName = string.Concat(className.Where(c => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_'));

        // Ensure the first character is not a digit
        if (char.IsDigit(cleanedName[0]))
        {
            cleanedName = "_" + cleanedName;
        }

        return cleanedName;
    }
}
