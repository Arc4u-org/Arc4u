using System.Diagnostics.CodeAnalysis;

namespace Arc4u;

internal static class Bound
{
    #region Properties

    private static readonly string _infinity = ('\u221E').ToString();
    internal static string Infinity { get { return _infinity; } }

    private static readonly string _emptySet = ('\u00D8').ToString();
    internal static string EmptySet { get { return _emptySet; } }

    #endregion

    #region Methods

    internal static bool TryParse<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(BoundType type,
                                     BoundDirection direction,
                                     T value,
                                     out Bound<T> result)
    {
        if (Bound.IsInfinity(value) && direction == BoundDirection.Closed)
        {
            result = default!;
            return false;
        }

        result = new Bound<T>(type, direction, value);
        return true;
    }

    internal static Bound<T> Min<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(Bound<T> x, Bound<T> y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        return x.CompareTo(y) > 0 ? y : x;
    }

    internal static Bound<T> Max<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(Bound<T> x, Bound<T> y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        return x.CompareTo(y) < 0 ? y : x;
    }

    internal static bool IsInfinity<T>(T value)
    {
        return object.Equals(value, default);
    }

    internal static BoundDirection Min(BoundDirection x, BoundDirection y)
    {
        return x > y ? y : x;
    }

    internal static BoundDirection Max(BoundDirection x, BoundDirection y)
    {
        return x < y ? y : x;
    }

    internal static BoundDirection Reverse(BoundDirection value)
    {
        return value == BoundDirection.Closed ? BoundDirection.Opened : BoundDirection.Closed;
    }

    #endregion
}
