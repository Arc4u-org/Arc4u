namespace Arc4u.Collections.Generic;

/// <summary>
/// Defines methods to support the comparison of objects for equality 
/// based on specified member(s) of those objects.
/// </summary>
/// <typeparam name="T">The type of objects to compare.</typeparam>
public sealed class MemberEqualityComparer<T> : IEqualityComparer<T>
{
    readonly Func<T, object> _selector;

    MemberEqualityComparer(Func<T, object> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        _selector = selector;
    }

    /// <summary>
    /// Creates a comparer that considers two objects equal when the values returned by <paramref name="selector"/> are equal.
    /// </summary>
    /// <param name="selector">Returns the member (or key) of an object that is used for the comparison.</param>
    /// <returns>A new <see cref="MemberEqualityComparer{T}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code language="csharp">
    /// var distinctById = people.Distinct(MemberEqualityComparer&lt;Person&gt;.Default(p =&gt; p.Id));
    /// </code>
    /// </example>
    public static MemberEqualityComparer<T> Default(Func<T, object> selector)
    {
        return new MemberEqualityComparer<T>(selector);
    }

    /// <summary>
    /// Determines whether two objects are the same reference or have equal selected members.
    /// </summary>
    /// <param name="x">The first object.</param>
    /// <param name="y">The second object.</param>
    /// <returns><see langword="true"/> if the objects are considered equal; otherwise <see langword="false"/>.</returns>
    public bool Equals(T? x, T? y)
    {
        return ReferenceEquals(x, y)
            || (x != null && y != null && Equals(_selector(x), _selector(y)));
    }

    /// <summary>
    /// Returns the hash code of the selected member of the object.
    /// </summary>
    /// <param name="obj">The object.</param>
    /// <returns>The hash code of the selected member, or 0 when the object or its selected member is <see langword="null"/>.</returns>
    public int GetHashCode(T? obj)
    {
        return (obj == null || _selector(obj) == null)
            ? 0
            : _selector(obj).GetHashCode();
    }
}
