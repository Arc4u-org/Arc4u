namespace Arc4u.Core;

/// <summary>
/// Base class for value objects: two instances are equal when they have the same runtime type and when
/// the sequences returned by <see cref="GetEqualityComponents"/> are equal, element by element.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Null-safe equality used by the <c>==</c> operator.
    /// </summary>
    /// <param name="left">The first value object, or <see langword="null"/>.</param>
    /// <param name="right">The second value object, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when both are <see langword="null"/> or when they are the same instance or are equal; otherwise <see langword="false"/>.</returns>
    protected static bool EqualOperator(ValueObject? left, ValueObject? right)
    {
        if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
        {
            return false;
        }

        return ReferenceEquals(left, null) ? ReferenceEquals(left, right) : ReferenceEquals(left, right) || left.Equals(right);
    }

    /// <summary>
    /// Null-safe inequality used by the <c>!=</c> operator.
    /// </summary>
    /// <param name="left">The first value object, or <see langword="null"/>.</param>
    /// <param name="right">The second value object, or <see langword="null"/>.</param>
    /// <returns>The negation of <see cref="EqualOperator(ValueObject?, ValueObject?)"/>.</returns>
    protected static bool NotEqualOperator(ValueObject? left, ValueObject? right)
    {
        return !(EqualOperator(left, right));
    }

    /// <summary>
    /// Returns the values that define the identity of the value object. They are used by <see cref="Equals(object?)"/> and <see cref="GetHashCode"/>.
    /// </summary>
    /// <returns>The values to compare, always enumerated in the same order.</returns>
    protected abstract IEnumerable<object> GetEqualityComponents();

    /// <summary>
    /// Determines whether the specified object has the same type as this instance and the same equality components.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns><see langword="true"/> if the objects are equal; otherwise <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Returns a hash code computed by combining (exclusive or) the hash codes of the equality components.
    /// </summary>
    /// <returns>The hash code of this instance.</returns>
    /// <exception cref="InvalidOperationException"><see cref="GetEqualityComponents"/> returns an empty sequence.</exception>
    public override int GetHashCode()
    {
        return GetEqualityComponents()
                .Select(x => x != null ? x.GetHashCode() : 0)
                .Aggregate((x, y) => x ^ y);
    }
    /// <summary>
    /// Determines whether two value objects are equal.
    /// </summary>
    /// <param name="one">The first value object, or <see langword="null"/>.</param>
    /// <param name="two">The second value object, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if both are equal; otherwise <see langword="false"/>.</returns>
    public static bool operator ==(ValueObject? one, ValueObject? two)
    {
        return EqualOperator(one, two);
    }

    /// <summary>
    /// Determines whether two value objects are different.
    /// </summary>
    /// <param name="one">The first value object, or <see langword="null"/>.</param>
    /// <param name="two">The second value object, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the value objects are different; otherwise <see langword="false"/>.</returns>
    public static bool operator !=(ValueObject? one, ValueObject? two)
    {
        return NotEqualOperator(one, two);
    }
}

