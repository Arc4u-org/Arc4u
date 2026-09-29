using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace Arc4u.Extensions;

/// <summary>
/// Extension methods to display enumeration values, based on the <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"/>.
/// </summary>
public static class EnumExtentions
{
    /// <summary>
    /// Gets the name defined by the <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"/> of an enumeration value.
    /// </summary>
    /// <param name="value">The enumeration value.</param>
    /// <returns>The display name of the value.</returns>
    /// <exception cref="ArgumentException">The value has no <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"/>.</exception>
    /// <exception cref="InvalidOperationException">The value is not a member of its enumeration.</exception>
    public static string GetDisplayName(this Enum value)
    {
        var type = value.GetType();
        var ti = type.GetTypeInfo();
        if (!ti.IsEnum)
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Type '{0}' is not Enum", type));
        }

        var member = type.GetRuntimeField(value.ToString());

        if (null == member)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "'{0}' is not a member of the enum '{1}'", value, type.Name));
        }

        var attributes = member.GetCustomAttributes(typeof(DisplayAttribute), false);
        if (attributes.Length == 0)
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "'{0}.{1}' doesn't have DisplayAttribute", type.Name, value));
        }

        var attribute = (DisplayAttribute)attributes.First();

        if (null == attribute)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "'{0}' doesn't have DisplayAttribute", value));
        }

        return attribute.GetName() ?? string.Empty;
    }

    /// <summary>
    /// Gets the name of an enumeration value (the result of <see cref="Enum.ToString()"/>).
    /// </summary>
    /// <param name="value">The enumeration value.</param>
    /// <returns>The name of the value.</returns>
    public static string GetValue(this Enum value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Lists the members of an enumeration together with their display name.
    /// </summary>
    /// <param name="enumType">The enumeration type.</param>
    /// <returns>A list of pairs (member name, display name).</returns>
    /// <exception cref="ArgumentException"><paramref name="enumType"/> is not an enumeration, or one of its members has no <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"/>.</exception>
    public static List<KeyValuePair<string, string>> ToTranslationList(this Type enumType)
    {
        var ti = enumType.GetTypeInfo();

        if (!ti.IsEnum)
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Type '{0}' is not Enum", enumType));
        }

        var t = Enum.GetNames(enumType)
                    .Select(enumName => Enum.Parse(enumType, enumName) as Enum)
                    .Where(e => e != null)
                    .ToDictionary(e => e!.ToString(), e => e!.GetDisplayName());

        return [.. t.Select(v => new KeyValuePair<string, string>(v.Key, v.Value))];
    }

    /// <summary>
    /// Maps the members of an enumeration to their display name.
    /// </summary>
    /// <param name="enumType">The enumeration type.</param>
    /// <returns>A dictionary whose keys are the members and whose values are the display names.</returns>
    /// <exception cref="ArgumentException"><paramref name="enumType"/> is not an enumeration, or one of its members has no <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"/>.</exception>
    public static Dictionary<Enum, string> ToTranslationDictionary(this Type enumType)
    {
        var ti = enumType.GetTypeInfo();

        if (!ti.IsEnum)
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Type '{0}' is not Enum", enumType));
        }

        return Enum.GetNames(enumType)
                   .Select(enumName => Enum.Parse(enumType, enumName) as Enum)
                   .Where(e => e != null)
                   .ToDictionary(e => e!, e => e!.GetDisplayName());
    }
}
