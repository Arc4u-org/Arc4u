using System.Text.RegularExpressions;

namespace Arc4u.Dependency.Tool;

/// <summary>
/// Turns a name into a valid C# identifier.
/// </summary>
public static class ClassNameCleaner
{
    private static readonly Regex InvalidCharsRegex = new(@"[^a-zA-Z0-9_]", RegexOptions.Compiled);

    /// <summary>
    /// Removes the characters that are not letters, digits or underscores, and prefixes the result with an underscore when it starts with a digit.
    /// </summary>
    /// <param name="className">The name to clean.</param>
    /// <returns>A valid identifier.</returns>
    /// <exception cref="IndexOutOfRangeException">No valid character remains after the cleaning.</exception>
    public static string CleanClassName(string className)
    {
        // Remove invalid characters using the compiled regex
        var cleanedName = InvalidCharsRegex.Replace(className, "");

        // Ensure the first character is not a digit
        if (char.IsDigit(cleanedName[0]))
        {
            cleanedName = "_" + cleanedName;
        }

        return cleanedName;
    }
}
