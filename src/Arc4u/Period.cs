using System.Globalization;
using System.Runtime.Serialization;

namespace Arc4u;

/// <summary>
/// Represents a period. This class cannot be inherited.
/// </summary>
[DataContract]
public sealed class Period
    : Interval<DateTimeOffset?>
    , IFormattable
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="Period"/> class 
    /// with an included <paramref name="lowerValue"/> and an excluded <paramref name="upperValue"/>.
    /// </summary>
    /// <param name="lowerValue">The lower value.</param>
    /// <param name="upperValue">The upper value.</param>        
    public Period(DateTimeOffset? lowerValue, DateTimeOffset? upperValue)
        : this(lowerValue, upperValue, true, false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Period"/> class
    /// with an included <paramref name="lowerValue"/>.
    /// </summary>
    /// <param name="lowerValue">The lower value.</param>
    /// <param name="upperValue">The upper value.</param>
    /// <param name="upperIncluded"><c>true</c> if the <paramref name="upperValue"/> is included in the <see cref="Period"/>; otherwise, <c>false</c>.</param>
    public Period(DateTimeOffset? lowerValue, DateTimeOffset? upperValue, bool upperIncluded)
        : this(lowerValue, upperValue, true, upperIncluded)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Period"/> class.
    /// </summary>        
    /// <param name="lowerValue">The lower value.</param>
    /// <param name="upperValue">The upper value.</param>
    /// <param name="lowerIncluded"><c>true</c> if the <paramref name="lowerValue"/> is included in the <see cref="Period"/>; otherwise, <c>false</c>.</param>
    /// <param name="upperIncluded"><c>true</c> if the <paramref name="upperValue"/> is included in the <see cref="Period"/>; otherwise, <c>false</c>.</param>
    public Period(DateTimeOffset? lowerValue
        , DateTimeOffset? upperValue
        , bool lowerIncluded
        , bool upperIncluded)
        : base(new Bound<DateTimeOffset?>
            (BoundType.Lower
            , lowerIncluded ? BoundDirection.Closed : BoundDirection.Opened
            , lowerValue)
        , new Bound<DateTimeOffset?>
            (BoundType.Upper
            , upperIncluded ? BoundDirection.Closed : BoundDirection.Opened
            , upperValue))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Period"/> class.
    /// </summary>
    /// <param name="lowerBound">The lower bound.</param>
    /// <param name="upperBound">The upper bound.</param>
    public Period(Bound<DateTimeOffset?> lowerBound, Bound<DateTimeOffset?> upperBound)
        : base(lowerBound, upperBound)
    {
    }

    #endregion        

    #region IFormattable Members

    /// <summary>
    /// Converts the value of the current <see cref="Period"/> object to its equivalent string representation using the specified format information.
    /// </summary>
    /// <param name="format">A format string.</param>
    /// <returns>
    /// A string representation of the value of the current <see cref="Period"/> object, as specified by <paramref name="format"/>.
    /// </returns>
    public string ToString(string format)
    {
        return ToString(format, DateTimeFormatInfo.CurrentInfo);
    }

    /// <summary>
    /// Converts the value of the current <see cref="Period"/> object to its equivalent string representation using the specified culture-specific format information.
    /// </summary>
    /// <param name="formatProvider">An <see cref="IFormatProvider"/> object that supplies culture-specific formatting information.</param>
    /// <returns>
    /// A string representation of the value of the current <see cref="Period"/> object, as specified by <paramref name="formatProvider"/>.
    /// </returns>
    public string ToString(IFormatProvider formatProvider)
    {
        return ToString(null, DateTimeFormatInfo.GetInstance(formatProvider));
    }

    /// <summary>
    /// Converts the value of the current <see cref="Period"/> object to its equivalent string representation using the specified format and culture-specific format information.
    /// </summary>
    /// <param name="format">A format string.</param>
    /// <param name="formatProvider">An <see cref="IFormatProvider"/> object that supplies culture-specific formatting information.</param>
    /// <returns>
    /// A string representation of the value of the current <see cref="Period"/> object, as specified by <paramref name="format"/> and <paramref name="formatProvider"/>.
    /// </returns>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        formatProvider ??= DateTimeFormatInfo.CurrentInfo;
        format ??= "G";

        return IsEmpty
            ? Bound.EmptySet
            : IsSingleton
                ? LowerBound.Value!.Value.ToString(format, formatProvider)
                : string.Format(formatProvider, "{0}{1} ; {2}{3}"
                                , LowerBound.Direction == BoundDirection.Closed ? "[" : "]"
                                , object.Equals(LowerBound.Value, default) ? Bound.Infinity : LowerBound.Value.Value.ToString(format, formatProvider)
                                , object.Equals(UpperBound.Value, default) ? Bound.Infinity : UpperBound.Value.Value.ToString(format, formatProvider)
                                , UpperBound.Direction == BoundDirection.Closed ? "]" : "[");
    }

    #endregion                           
}
