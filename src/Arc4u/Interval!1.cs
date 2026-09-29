using System.Globalization;
using System.Runtime.Serialization;

namespace Arc4u;

/// <summary>
/// Represents an interval of values delimited by a <see cref="LowerBound"/> and an <see cref="UpperBound"/>,
/// giving support for set theory operations.
/// Provides the base class for types requiring support of set theory operations, such as the <see cref="Period"/> class.
/// </summary>
/// <typeparam name="T">The type of the interval.</typeparam>

[DataContract(Name = "IntervalOf{0}")]
[KnownType(typeof(Period))]
public class Interval<T>
    : IEquatable<Interval<T>>
    , IComparable<Interval<T>>
{
    #region Properties

    /// <summary>
    /// Gets the lower bound.
    /// </summary>
    /// <value>The lower bound.</value>
    /// <remarks>The set operation is not private to let the Silverlight runtime
    /// accessing the property during serializing/deserializing operations.</remarks>
    [DataMember(EmitDefaultValue = false)]
    public Bound<T> LowerBound { get; internal set; }

    /// <summary>
    /// Gets the upper bound.
    /// </summary>
    /// <value>The upper bound.</value>
    /// <remarks>The set operation is not private to let the Silverlight runtime
    /// accessing the property during serializing/deserializing operations.</remarks>
    [DataMember(EmitDefaultValue = false)]
    public Bound<T> UpperBound { get; internal set; }

    /// <summary>
    /// Gets the complement of this instance.
    /// </summary>
    /// <value>An <see cref="IntervalCollection&lt;T&gt;"/> that contains elements not in this instance.</value>
    /// <seealso href="http://en.wikipedia.org/wiki/Complement_(set_theory)">Complement (set theory)</seealso>
    public IntervalCollection<T> Complement
    {
        get { return Interval.ComplementOf(this); }
    }

    /// <summary>
    /// Gets a value indicating whether this instance represents a singleton.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance contains only one element; otherwise, <c>false</c>.
    /// </value>
    /// <seealso cref="IsSingletonOf"/>
    /// <seealso cref="Interval.SingletonOf"/>
    /// <seealso href="http://en.wikipedia.org/wiki/Singleton_(mathematics)">Singleton (mathematics)</seealso>
    public bool IsSingleton
    {
        get
        {
            return IsSingletonOf(LowerBound.Value);
        }
    }

    /// <summary>
    /// Gets a value indicating whether this instance represents a singleton of the specified element.
    /// </summary>
    /// <param name="value">The element value.</param>
    /// <returns>
    /// 	<c>true</c> if this instance contains only the specified <paramref name="value"/>; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsSingleton"/>
    /// <seealso cref="Interval.SingletonOf"/>
    /// <seealso href="http://en.wikipedia.org/wiki/Singleton_(mathematics)">Singleton (mathematics)</seealso>
    public bool IsSingletonOf(T value)
    {
        return object.Equals(LowerBound.Value, value)
            && object.Equals(UpperBound.Value, value)
            && object.Equals(LowerBound.Direction, BoundDirection.Closed)
            && object.Equals(UpperBound.Direction, BoundDirection.Closed)
            && !Bound.IsInfinity(LowerBound.Value);
    }

    /// <summary>
    /// Gets a value indicating whether this instance represents an empty <see cref="Interval&lt;T&gt;"/>.
    /// </summary>
    /// <value><c>true</c> if this instance contains no element; otherwise, <c>false</c>.</value>
    /// <seealso cref="Interval.Empty{T}"/>
    /// <seealso href="http://en.wikipedia.org/wiki/Empty_set">Empty Set (set theory)</seealso>
    public bool IsEmpty
    {
        get
        {
            return IsEmptyOf(LowerBound.Value);
        }
    }

    /// <summary>
    /// Gets a value indicating whether this instance represents an empty <see cref="Interval&lt;T&gt;"/> of the specified element.
    /// </summary>
    /// <param name="value">The element value.</param>
    /// <returns>
    /// 	<c>true</c> if this instance contains no element; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsEmpty"/>
    /// <seealso cref="Interval.Empty{T}"/>
    /// <seealso cref="Interval.EmptyOf"/>
    /// <seealso href="http://en.wikipedia.org/wiki/Empty_set">Empty Set (set theory)</seealso>
    protected internal bool IsEmptyOf(T value)
    {
        return (object.Equals(LowerBound.Value, value)
             && object.Equals(UpperBound.Value, value)
             && object.Equals(LowerBound.Direction, UpperBound.Direction)
             && (Bound.IsInfinity(LowerBound.Value)
                     ? object.Equals(LowerBound.Direction, BoundDirection.Closed)
                     : object.Equals(LowerBound.Direction, BoundDirection.Opened)));
    }

    /// <summary>
    /// Gets a value indicating whether this instance represents an universe.
    /// </summary>
    /// <value><c>true</c> if this instance represents an universe; otherwise, <c>false</c>.</value>
    /// <seealso href="http://en.wikipedia.org/wiki/Universe_(mathematics)">Universe (mathematics)</seealso>
    public bool IsUniverse
    {
        get
        {
            return object.Equals(LowerBound, Bound<T>.LowestBound)
                && object.Equals(UpperBound, Bound<T>.UpmostBound);
        }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="Interval&lt;T&gt;"/> class.
    /// </summary>
    protected internal Interval()
    {
        LowerBound = default!;
        UpperBound = default!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Interval&lt;T&gt;"/> class.
    /// </summary>
    /// <param name="lowerDirection">The lower direction.</param>
    /// <param name="lowerValue">The lower value.</param>
    /// <param name="upperValue">The upper value.</param>
    /// <param name="upperDirection">The upper direction.</param>
    public Interval(BoundDirection lowerDirection
        , T lowerValue
        , T upperValue
        , BoundDirection upperDirection)
        : this(new Bound<T>(BoundType.Lower, lowerDirection, lowerValue)
        , new Bound<T>(BoundType.Upper, upperDirection, upperValue))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Interval&lt;T&gt;"/> class.
    /// </summary>
    /// <param name="lowerBound">The lower bound.</param>
    /// <param name="upperBound">The upper bound.</param>
    protected internal Interval(Bound<T>? lowerBound, Bound<T>? upperBound)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(lowerBound);
        ArgumentNullException.ThrowIfNull(upperBound);
#else
        if (lowerBound is null)
        {
            throw new ArgumentNullException(nameof(lowerBound));
        }

        if (upperBound is null)
        {
            throw new ArgumentNullException(nameof(upperBound));
        }
#endif

        if (lowerBound > upperBound)
        {
            throw new ArgumentException("The lower bound must be lower than or equal the upper bound.");
        }

        if (object.Equals(lowerBound.Value, upperBound.Value)
            && !object.Equals(lowerBound.Direction, upperBound.Direction)
            && !Bound.IsInfinity(lowerBound.Value))
        {
            throw new ArgumentException("Singleton or empty interval must define the same boundary direction.");
        }

        LowerBound = lowerBound;
        UpperBound = upperBound;
    }

    #endregion

    #region Overriden Members

    /// <summary>
    /// Returns a <see cref="string"/> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string"/> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return IsEmpty
            ? Bound.EmptySet
            : IsSingleton
                ? LowerBound?.Value?.ToString() ?? string.Empty
                : string.Format(CultureInfo.InvariantCulture, "{0} ; {1}", LowerBound?.ToString() ?? string.Empty, UpperBound?.ToString() ?? string.Empty);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode()
    {
        var hash = 0x4043ed47;
        hash = (hash * -1521134295) + (object.Equals(LowerBound, default(Bound<T>)) ? 0 : LowerBound.GetHashCode());
        hash = (hash * -1521134295) + (object.Equals(UpperBound, default(Bound<T>)) ? 0 : UpperBound.GetHashCode());
        return hash;
    }

    /// <summary>
    /// Returns a value indicating whether this instance is equal to a specified object.
    /// </summary>
    /// <param name="obj">An object to compare to this instance.</param>
    /// <returns>
    ///     <b>true</b> if this instance equals the <paramref name="obj"/>; otherwise, <b>false</b>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        return obj is Interval<T> other && Equals(other);
    }

    #endregion

    #region IEquatable<Interval<T>> Members

    /// <summary>
    /// Returns a value indicating whether this instance is equal to a specified <see cref="Interval&lt;T&gt;"/>.
    /// </summary>
    /// <param name="other">A <see cref="Interval&lt;T&gt;"/> to compare to this instance.</param>
    /// <returns>
    ///     <b>true</b> if this instance equals the <paramref name="other"/>; otherwise, <b>false</b>.
    /// </returns>
    public bool Equals(Interval<T>? other)
    {
        return object.ReferenceEquals(this, other) ||
               (other is not null
                    && LowerBound.Equals(other.LowerBound)
                    && UpperBound.Equals(other.UpperBound));
    }

    #endregion

    #region IComparable<Interval<T>> Members

    /// <summary>
    /// Compares this instance to a specified <see cref="Interval&lt;T&gt;"/> and returns an indication of their relative values.
    /// </summary>
    /// <param name="other">A <see cref="Interval&lt;T&gt;"/> to compare to this instance.</param>
    /// <returns>
    /// A signed number indicating the relative values of this instance and the <paramref name="other"/>.
    /// <list type="table">
    /// <listheader>
    ///     <term>Value Type</term>
    ///     <description>Condition</description>
    ///  </listheader>
    /// <item>
    ///     <term>Less than zero</term>
    ///     <description>This instance is less than the <paramref name="other"/>.</description>
    /// </item>
    /// <item>
    ///     <term>Zero</term>
    ///     <description>This instance is equal to the <paramref name="other"/>.</description>
    /// </item>
    /// <item>
    ///     <term>Greater than zero</term>
    ///     <description>This instance is greater than the <paramref name="other"/>.</description>
    /// </item>
    ///</list>
    /// </returns>
    /// <remarks>
    /// Comparison does not rely on the number of elements contained in the <see cref="Interval&lt;T&gt;"/>
    /// but it is rather based on first the <see cref="LowerBound"/> and then the <see cref="UpperBound"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null.</exception>
    public int CompareTo(Interval<T>? other)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(other);
#else
        if (other is null)
        {
            throw new ArgumentNullException("other");
        }
#endif
        if (IsEmpty && !other.IsEmpty)
        {
            return -1;
        }

        if (!IsEmpty && other.IsEmpty)
        {
            return 1;
        }

        return (LowerBound.CompareTo(other.LowerBound) == 0)
                                ? UpperBound.CompareTo(other.UpperBound)
                                : LowerBound.CompareTo(other.LowerBound);
    }

    #endregion

    #region Operators

    /// <summary>
    /// Implements the operator ==.
    /// </summary>
    /// <param name="left">The left <see cref="Interval&lt;T&gt;"/>.</param>
    /// <param name="right">The right <see cref="Interval&lt;T&gt;"/>.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator ==(Interval<T>? left, Interval<T>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Implements the operator ==.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator ==(object? left, Interval<T>? right)
    {
        return (left as Interval<T>) == right;
    }

    /// <summary>
    /// Implements the operator ==.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator ==(Interval<T> left, object right)
    {
        return left == (right as Interval<T>);
    }

    /// <summary>
    /// Implements the operator !=.
    /// </summary>
    /// <param name="left">The left <see cref="Interval&lt;T&gt;"/>.</param>
    /// <param name="right">The right <see cref="Interval&lt;T&gt;"/>.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator !=(Interval<T>? left, Interval<T>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return false;
        }

        if (left is null || right is null)
        {
            return true;
        }

        return !left.Equals(right);
    }

    /// <summary>
    /// Implements the operator !=.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator !=(object left, Interval<T> right)
    {
        return (left as Interval<T>) != right;
    }

    /// <summary>
    /// Implements the operator ==.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator !=(Interval<T> left, object right)
    {
        return left != (right as Interval<T>);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Overrides the default bound values of <see cref="Interval&lt;T&gt;"/>.
    /// </summary>
    /// <param name="lowestValue">The lowest value.</param>
    /// <param name="upmostValue">The upmost value.</param>
    public static void OverrideBounds(T lowestValue, T upmostValue)
    {
        Bound<T>.OverrideBounds(lowestValue, upmostValue);
    }

    /// <summary>
    /// Determines whether this instance contains the specified <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value to search for.</param>
    /// <returns>
    /// 	<c>true</c> if this instance contains the specified <paramref name="value"/>; otherwise, <c>false</c>.
    /// </returns>
    public bool Contains(T value)
    {
        return Interval.Contains(this, value);
    }

    /// <summary>
    /// Determines whether this instance intersects the <paramref name="other"/> one.
    /// </summary>
    /// <param name="other">Another <see cref="Interval&lt;T&gt;"/>.</param>
    /// <returns><c>true</c> if this instance intersects the <paramref name="other"/> one; otherwise, <c>false</c>.</returns>
    public bool IntersectsWith(Interval<T> other)
    {
        return Interval.Intersect(this, other);
    }

    /// <summary>
    /// Determines the intersection of this instance with the <paramref name="other"/> one.
    /// </summary>
    /// <param name="other">Another <see cref="Interval&lt;T&gt;"/>.</param>
    /// <param name="intersection">When this method returns, contains the <see cref="Interval&lt;T&gt;"/>
    /// that contains all elements of this instance that also belong to the <paramref name="other"/> one;
    /// otherwise, an <see cref="Interval.Empty{T}"/> interval.</param>
    /// <returns><b>true</b> if the intersection is not empty; otherwise, <b>false</b>.</returns>
    public bool TryIntersectionWith(Interval<T> other, out Interval<T> intersection)
    {
        return Interval.TryIntersectionOf(this, other, out intersection);
    }

    /// <summary>
    /// Determines the intersection of this instance with the <paramref name="other"/> one.
    /// </summary>
    /// <param name="other">Another <see cref="Interval&lt;T&gt;"/>.</param>
    /// <returns>An <see cref="Interval&lt;T&gt;"/> that contains all elements of this instance
    /// that also belong to the <paramref name="other"/> one (or equivalently, all elements of the <paramref name="other"/> one
    /// that also belong to this instance), but no other elements.</returns>
    /// <seealso href="http://en.wikipedia.org/wiki/Intersection_(set_theory)">Intersection (set theory)</seealso>
    public Interval<T> IntersectionWith(Interval<T> other)
    {
        return Interval.IntersectionOf(this, other);
    }

    /// <summary>
    /// Determines the union of this instance with the <paramref name="other"/> one.
    /// </summary>
    /// <param name="other">Another <see cref="Interval&lt;T&gt;"/>.</param>
    /// <returns>An <see cref="IntervalCollection&lt;T&gt;"/> that contains all distinct elements of this instance and the <paramref name="other"/> one.</returns>
    /// <seealso href="http://en.wikipedia.org/wiki/Union_(set_theory)">Union (set theory)</seealso>
    public IntervalCollection<T> UnionWith(Interval<T> other)
    {
        return UnionWith(UnionDenominator.Highest, other);
    }

    /// <summary>
    /// Determines the union of this instance with the <paramref name="other"/> one according to the specified <paramref name="denominator"/>.
    /// </summary>
    /// <param name="denominator">The denominator considered while performing the union.</param>
    /// <param name="other">Another <see cref="Interval&lt;T&gt;"/>.</param>
    /// <returns>An <see cref="IntervalCollection&lt;T&gt;"/> that contains all distinct elements of this instance and the <paramref name="other"/> one.</returns>
    /// <remarks>
    /// While performing an <see cref="Interval.UnionOf&lt;T&gt;">UnionOf</see> intervals, depending of the specified <see cref="UnionDenominator"/>, the result will be different.
    /// If you consider the following intervals:
    /// <list type="table">
    /// <item>
    /// <term>a</term>
    /// <description>]∞ ; 3[</description>
    /// </item>
    /// <item>
    /// <term>b</term>
    /// <description>]0 ; 5[</description>
    /// </item>
    /// <item>
    /// <term>c</term>
    /// <description>[1 ; 2]</description>
    /// </item>
    /// </list>
    /// The union with the <see cref="UnionDenominator.Highest"/> denominator will return an <see cref="IntervalCollection&lt;T&gt;"/> of 1 element as shown below:
    /// <list type="table">
    /// <item>
    /// <term>i</term>
    /// <description>]∞ ; 5[</description>
    /// </item>
    /// </list>
    /// The union with the <see cref="UnionDenominator.Lowest"/> denominator will return an <see cref="IntervalCollection&lt;T&gt;"/> of 5 element as shown below:
    /// <list type="table">
    /// <item>
    /// <term>i</term>
    /// <description>]∞ ; 0]</description>
    /// </item>
    /// <item>
    /// <term>j</term>
    /// <description>]0 ; 1[</description>
    /// </item>
    /// <item>
    /// <term>k</term>
    /// <description>[1 ; 2]</description>
    /// </item>
    /// <item>
    /// <term>l</term>
    /// <description>]2 ; 3[</description>
    /// </item>
    /// <item>
    /// <term>m</term>
    /// <description>[3 ; 5[</description>
    /// </item>
    /// </list>
    /// </remarks>
    public IntervalCollection<T> UnionWith(UnionDenominator denominator, Interval<T> other)
    {
        return Interval.UnionOf(denominator, this, other);
    }

    /// <summary>
    /// Determines the difference this instance with the <paramref name="other"/> one.
    /// </summary>
    /// <param name="other">Another <see cref="Interval&lt;T&gt;"/>.</param>
    /// <returns>An <see cref="IntervalCollection&lt;T&gt;"/> that contains elements in this instance but not in <paramref name="other"/> one.</returns>
    /// <seealso href="http://en.wikipedia.org/wiki/Complement_(set_theory)">Relative complement (set theory)</seealso>
    public IntervalCollection<T> DifferenceWith(Interval<T> other)
    {
        return Interval.DifferenceOf(this, other);
    }

    #endregion
}
