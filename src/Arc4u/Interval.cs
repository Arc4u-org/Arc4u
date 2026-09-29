using System.Diagnostics.CodeAnalysis;

namespace Arc4u;

/// <summary>
/// Gathers operations performed on <see cref="Interval&lt;T&gt;"/>. This class cannot be inherited.
/// </summary>
public static class Interval
{
    /// <summary>
    /// Gets the universe <see cref="Interval&lt;T&gt;"/>.
    /// </summary>
    /// <typeparam name="T">The value type of the intervals.</typeparam>
    /// <returns></returns>
    /// <value>An <see cref="Interval&lt;T&gt;"/> containing all elements.</value>
    /// <remarks>
    /// An universe <see cref="Interval&lt;T&gt;"/> is composed of the lowest and upmost values of <typeparamref name="T"/>
    /// with <see cref="BoundDirection.Opened"/> boundaries when <typeparamref name="T"/> represents a reference type
    /// with <see cref="BoundDirection.Closed"/> boundaries when <typeparamref name="T"/> represents a value type.
    /// The lowest and upmost values are determined in the following order:
    /// <list type="number">
    /// 		<item>if <typeparamref name="T"/> is an <see cref="Enum"/>, the lowest and upmost values are represented respectively by the lowest and upmost underlying values of the <see cref="Enum"/>.
    /// In case of <see cref="Enum"/> decorated with the <see cref="FlagsAttribute"/>, the upmost value is then represented by the combination of all underlying values of the <see cref="Enum"/>.</item>
    /// 		<item>if <typeparamref name="T"/> is exposing the NegativeInfinity and PositiveInfinity fields, they represent respectively the lowest and upmost values, as for <see cref="float"/>, <see cref="double"/>.</item>
    /// 		<item>if <typeparamref name="T"/> is exposing the MinValue and MaxValue fields, they represent respectively the lowest and upmost values, as for <see cref="byte"/>, <see cref="int"/>, <see cref="DateTime"/> or <see cref="char"/>… </item>
    /// 		<item>otherwise, <c>default(T)</c> represents the lowest and upmost values.</item>
    /// 	</list>
    /// As a consequence <c>default(T)</c> could be used to represent the universe <see cref="Interval&lt;T&gt;"/> and the default <see cref="Empty{T}"/> interval.
    /// In this case only and by convention, the default empty <see cref="Interval&lt;T&gt;"/> is composed of <seealso cref="BoundDirection.Closed"/> boudaries.
    /// As another consequence, <c>default(T)</c> could be used to represent the universe <see cref="Interval&lt;T&gt;"/> and the <see cref="SingletonOf"/> the default value.
    /// In such case the <see cref="Interval&lt;T&gt;"/> represents then universe and a singleton.
    /// </remarks>
    /// <seealso href="http://en.wikipedia.org/wiki/Universe_(mathematics)">Universe (mathematics)</seealso>
    public static Interval<T> Universe<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>()
    {
        return new Interval<T>(Bound<T>.LowestBound, Bound<T>.UpmostBound);
    }

    /// <summary>
    /// Gets a singleton of the specified element.
    /// </summary>
    /// <typeparam name="T">The value type of the intervals.</typeparam>
    /// <param name="value">The element value.</param>
    /// <returns>
    /// An <see cref="Interval&lt;T&gt;"/> containing only the specified <paramref name="value"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <seealso cref="Interval&lt;T&gt;.IsSingleton"/>
    /// <seealso cref="Interval&lt;T&gt;.IsSingletonOf"/>
    /// <seealso href="http://en.wikipedia.org/wiki/Singleton_(mathematics)">Singleton (mathematics)</seealso>
    public static Interval<T> SingletonOf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new Interval<T>(BoundDirection.Closed
            , value
            , value
            , BoundDirection.Closed);
    }

    /// <summary>
    /// Gets the default empty <see cref="Interval&lt;T&gt;"/>.
    /// </summary>
    /// <typeparam name="T">The value type of the intervals.</typeparam>
    /// <returns>An <see cref="Interval&lt;T&gt;"/> containing no element.</returns>
    /// <remarks>
    /// An empty <see cref="Interval&lt;T&gt;"/> is composed of one element 
    /// (usually <c>default(T)</c> but not exclusively) 
    /// with <see cref="BoundDirection.Opened"/> boundaries.
    /// Except when <typeparamref name="T"/> represents a reference type, 
    /// the default empty interval is composed of <see cref="BoundDirection.Closed"/> boundaries.
    /// It enables the distinction between the default <see cref="Empty{T}"/> interval and 
    /// the <see cref="Universe"/> interval.
    /// </remarks>
    /// <seealso cref="Interval&lt;T&gt;.IsEmpty"/>        
    /// <seealso href="http://en.wikipedia.org/wiki/Empty_set">Empty Set (set theory)</seealso>
    public static Interval<T> Empty<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>()
    {
        return new Interval<T>(
            new Bound<T>(BoundType.Lower
                , object.Equals(default(T), null)
                    ? BoundDirection.Closed
                    : BoundDirection.Opened
                , default!
                , false),
            new Bound<T>(BoundType.Upper
                , object.Equals(default(T), null)
                    ? BoundDirection.Closed
                    : BoundDirection.Opened
                , default!
                , false));
    }

    /// <summary>
    /// Gets an empty <see cref="Interval&lt;T&gt;"/> of the specified element.
    /// </summary>
    /// <typeparam name="T">The value type of the intervals.</typeparam>
    /// <param name="value">The element value.</param>
    /// <returns>
    /// An interval <see cref="Interval&lt;T&gt;"/> containing no element.
    /// </returns>
    /// <remarks>
    /// An empty <see cref="Interval&lt;T&gt;"/> is composed of one element
    /// (usually <c>default(T)</c> but not exclusively)
    /// with <see cref="BoundDirection.Opened"/> boundaries;
    /// except when <typeparamref name="T"/> represents a reference type,
    /// the default empty interval is composed of <see cref="BoundDirection.Closed"/> boundaries.
    /// It enables the distinction between the default <see cref="Empty{T}"/> interval and
    /// the <see cref="Universe"/> interval.
    /// </remarks>
    /// <seealso cref="Empty"/>
    /// <seealso cref="Interval&lt;T&gt;.IsEmpty"/>
    /// <seealso cref="Interval&lt;T&gt;.IsEmptyOf"/>
    /// <seealso href="http://en.wikipedia.org/wiki/Empty_set">Empty Set (set theory)</seealso>
    internal static Interval<T> EmptyOf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(T value)
    {
        return Bound.IsInfinity(value)
            ? new Interval<T>(BoundDirection.Closed
                , value
                , value
                , BoundDirection.Closed)
            : new Interval<T>(BoundDirection.Opened
                , value
                , value
                , BoundDirection.Opened);
    }

    /// <summary>
    /// Indicates whether the specified <see cref="Interval&lt;T&gt;"/> object is <c>null</c> or an <see cref="Empty{T}"/> interval.
    /// </summary>
    /// <typeparam name="T">The value type of the interval.</typeparam>
    /// <param name="value">An <see cref="Interval&lt;T&gt;"/> reference.</param>
    /// <returns>
    /// 	<c>true</c> if the <paramref name="value"/> parameter is <c>null</c> or an emty interval; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsNullOrEmpty<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(Interval<T> value)
    {
        return (object.Equals(value, default(Interval<T>)) || value.IsEmpty);
    }

    /// <summary>
    /// Determines whether an <see cref="Interval&lt;T&gt;"/> contains a specified <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">The value type of the interval.</typeparam>
    /// <param name="interval">An <see cref="Interval&lt;T&gt;"/> reference.</param>
    /// <param name="value">The value to search for.</param>
    /// <returns>
    /// 	<c>true</c> if the <paramref name="interval"/> contains the <paramref name="value"/>; otherwise, <c>false</c>.
    /// </returns>
    public static bool Contains<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(Interval<T> interval, T value)
    {
        if (IsNullOrEmpty(interval))
        {
            return false;
        }

        var lowerBound = interval.LowerBound;
        var upperBound = interval.UpperBound;

        if (Bound.IsInfinity(value))
        {
            //consider [null ; null[ OR ]null ; null] cases
            if (object.Equals(lowerBound.Value, upperBound.Value)
                && Bound.IsInfinity(lowerBound.Value)
                && lowerBound.Direction != upperBound.Direction)
            {
                return false;
            }
            else
            {
                return (lowerBound.Direction == BoundDirection.Closed && Comparer<T>.Default.Compare(lowerBound.Value, value) == 0) || (upperBound.Direction == BoundDirection.Closed && Comparer<T>.Default.Compare(upperBound.Value, value) == 0);
            }
        }
        else
        {
            return lowerBound.Contains(value) && upperBound.Contains(value);
        }
    }

    #region Complement

    /// <summary>
    /// Determines the complement of an <see cref="Interval&lt;T&gt;"/>.
    /// </summary>
    /// <typeparam name="T">The value type of the interval.</typeparam>
    /// <param name="value">An <see cref="Interval&lt;T&gt;"/> reference.</param>
    /// <returns>An <see cref="IntervalCollection&lt;T&gt;"/> that contains elements not in the specified interval.</returns>
    /// <seealso href="http://en.wikipedia.org/wiki/Complement_(set_theory)">Complement (set theory)</seealso>
    public static IntervalCollection<T> ComplementOf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(Interval<T> value)
    {
        if (Interval.IsNullOrEmpty(value))
        {
            return new IntervalCollection<T>
            (
                new Interval<T>(Bound<T>.LowestBound, Bound<T>.UpmostBound)
            );
        }

        var lowerBound = value.LowerBound;
        var upperBound = value.UpperBound;
        var result = new List<Interval<T>>(2);

        if (object.Equals(lowerBound, Bound<T>.LowestBound) && object.Equals(upperBound, Bound<T>.UpmostBound))
        {

        }
        else if (object.Equals(lowerBound, Bound<T>.LowestBound))
        {
            result.Add(new Interval<T>
                (new Bound<T>(BoundType.Lower, Bound.Reverse(upperBound.Direction), upperBound.Value)
                , Bound<T>.UpmostBound));
        }
        else if (object.Equals(upperBound, Bound<T>.UpmostBound))
        {
            result.Add(new Interval<T>
                (Bound<T>.LowestBound
                , new Bound<T>(BoundType.Upper, Bound.Reverse(lowerBound.Direction), lowerBound.Value)));
        }
        else
        {
            var iLower = Bound<T>.LowestBound;
            var iUpper = new Bound<T>(BoundType.Upper, Bound.Reverse(lowerBound.Direction), lowerBound.Value);

            var jLower = new Bound<T>(BoundType.Lower, Bound.Reverse(upperBound.Direction), upperBound.Value);
            var jUpper = Bound<T>.UpmostBound;

            if (Interval.TryParse(iLower, iUpper, out var i))
            {
                result.Add(i);
            }

            if (Interval.TryParse(jLower, jUpper, out var j))
            {
                result.Add(j);
            }
        }

        return new IntervalCollection<T>(result);
    }

    /// <summary>
    /// Determines the complement of several intervals.
    /// </summary>
    /// <typeparam name="T">The value type of the intervals.</typeparam>
    /// <param name="args">The <see cref="Interval&lt;T&gt;"/> arguments.</param>
    /// <returns>An <see cref="IntervalCollection&lt;T&gt;"/> that contains elements not in the specified intervals.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="args"/> is <c>null</c>.</exception>
    public static IntervalCollection<T> ComplementOf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(params Interval<T>[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        return Interval.ComplementOf(new IntervalCollection<T>(args));

    }

    /// <summary>
    /// Determines the complement of an <see cref="IntervalCollection&lt;T&gt;"/>.
    /// </summary>
    /// <typeparam name="T">The value type of the intervals.</typeparam>
    /// <param name="collection">A collection of <see cref="Interval&lt;T&gt;"/>.</param>
    /// <returns>An <see cref="IntervalCollection&lt;T&gt;"/> that contains elements not in the specified <paramref name="collection"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
    internal static IntervalCollection<T> ComplementOf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(IntervalCollection<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        if (collection.Count == 0)
        {
            return collection;
        }

        var result = new List<Interval<T>>();

        foreach (var item in collection)
        {
            if (item == null)
            {
                continue;
            }

            foreach (var c in item.Complement)
            {
                var others = new IntervalCollection<T>((from i in collection
                                                        where i != item
                                                        select i).ToList());
                result.AddRange(DifferenceOf(c, others));
            }
        }

        return new IntervalCollection<T>(result.Distinct().ToList());
    }

    #endregion

    #region Difference

    /// <summary>
    /// Determines the difference of two intervals.
    /// </summary>
    /// <typeparam name="T">The value type of the interval.</typeparam>
    /// <param name="a">The first interval.</param>
    /// <param name="b">The second interval.</param>
    /// <returns>An <see cref="IntervalCollection&lt;T&gt;"/> that contains elements in <paramref name="a"/> but not in <paramref name="b"/>.</returns>
    /// <seealso href="http://en.wikipedia.org/wiki/Complement_(set_theory)">Relative complement (set theory)</seealso>
    public static IntervalCollection<T> DifferenceOf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(Interval<T> a, Interval<T> b)
    {
        if (Interval.IsNullOrEmpty(a))
        {
            return new IntervalCollection<T>(a);
        }

        if (Interval.IsNullOrEmpty(b))
        {
            return new IntervalCollection<T>(a);
        }

        // A - B = A ∩ Bc
        if (object.Equals(a, b))
        {
            return new IntervalCollection<T>();
        }

        var c = b.Complement;
        var args = new Interval<T>[c.Count + 1];
        args[0] = a;
        c.CopyTo(args, 1);

        var result = new List<Interval<T>>(2);

        foreach (var i in c)
        {
            var value = a.IntersectionWith(i);
            if (!Interval.IsNullOrEmpty(value))
            {
                result.Add(value);
            }
        }

        return new IntervalCollection<T>(result);
    }

    private static IntervalCollection<T> DifferenceOf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(Interval<T> value, IntervalCollection<T> others)
    {
        var result = new List<Interval<T>> { value };

        foreach (var other in others)
        {
            var col = new IntervalCollection<T>(result);
            result = [];
            foreach (var item in col)
            {
                result.AddRange(item.DifferenceWith(other));
            }
        }

        return new IntervalCollection<T>(result);
    }

    #endregion

    #region Intersection

    /// <summary>
    /// Determines whether two intervals intersect each other.
    /// </summary>
    /// <typeparam name="T">The value type of the intervals.</typeparam>
    /// <param name="a">The first interval.</param>
    /// <param name="b">The second interval.</param>
    /// <returns><c>true</c> if the <see cref="Interval&lt;T&gt;"/> <paramref name="a"/> intersects the interval <paramref name="b"/>; otherwise, <c>false</c>.</returns>
    public static bool Intersect<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(Interval<T> a, Interval<T> b)
    {
        return !IsNullOrEmpty(IntersectionOf(a, b));
    }

    /// <summary>
    /// Determines the intersection of two intervals.
    /// </summary>
    /// <typeparam name="T">The value type of the intervals.</typeparam>
    /// <param name="a">The first interval.</param>
    /// <param name="b">The second interval.</param>
    /// <param name="intersection">When this method returns, contains the <see cref="Interval&lt;T&gt;"/> 
    /// that contains all elements of <paramref name="a"/> that also belong to <paramref name="b"/>; 
    /// otherwise, an empty interval.</param>
    /// <returns><b>true</b> if the intersection is not empty; otherwise, <b>false</b>.</returns>
    public static bool TryIntersectionOf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(Interval<T> a, Interval<T> b, out Interval<T> intersection)
    {
        intersection = IntersectionOf(a, b);
        return !IsNullOrEmpty(intersection);
    }

    /// <summary>
    /// Determines the intersection of several intervals.
    /// </summary>
    /// <typeparam name="T">The value type of the intervals.</typeparam>
    /// <param name="args">The <see cref="Interval&lt;T&gt;"/> arguments.</param>
    /// <returns>An <see cref="Interval&lt;T&gt;"/> that contains elements belonging to all specified intervals.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="args"/> is <c>null</c>.</exception>
    public static Interval<T> IntersectionOf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(params Interval<T>[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        return (args.Length == 2)
                  ? IntersectionOf(args[0], args[1])
                  : IntersectionOf(new IntervalCollection<T>(args));
    }

    /// <summary>
    /// Determines the intersection of two intervals.
    /// </summary>
    /// <typeparam name="T">The value type of the intervals.</typeparam>
    /// <param name="a">The first interval.</param>
    /// <param name="b">The second interval.</param>
    /// <returns>An <see cref="Interval&lt;T&gt;"/> that contains all elements of <paramref name="a"/> 
    /// that also belong to <paramref name="b"/> (or equivalently, all elements of <paramref name="b"/> 
    /// that also belong to <paramref name="a"/>), but no other elements.</returns>
    /// <seealso href="http://en.wikipedia.org/wiki/Intersection_(set_theory)">Intersection (set theory)</seealso>
    private static Interval<T> IntersectionOf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(Interval<T> a, Interval<T> b)
    {
        if (Interval.IsNullOrEmpty(a))
        {
            return a;
        }

        if (Interval.IsNullOrEmpty(b))
        {
            return b;
        }

        var lowestBound = Bound.Min(a.LowerBound, b.LowerBound);
        var upmostValue = Bound.Max(a.UpperBound, b.UpperBound);

        Bound<T> lowerBound;
        Bound<T> upperBound;

        //      a
        //-------------
        //      b
        //  ---------
        if (object.Equals(lowestBound, a.LowerBound)
            && object.Equals(upmostValue, a.UpperBound))
        {
            lowerBound = new Bound<T>(BoundType.Lower
                , object.Equals(a.LowerBound.Value, b.LowerBound.Value)
                    ? Bound.Min(a.LowerBound.Direction, b.LowerBound.Direction)
                    : b.LowerBound.Direction
                , b.LowerBound.Value);
            upperBound = new Bound<T>(BoundType.Upper
                , object.Equals(a.UpperBound.Value, b.UpperBound.Value)
                    ? Bound.Min(a.UpperBound.Direction, b.UpperBound.Direction)
                    : b.UpperBound.Direction
                , b.UpperBound.Value);
        }

        //      a
        //  ---------
        //      b
        //-------------
        else if (object.Equals(lowestBound, b.LowerBound)
            && object.Equals(upmostValue, b.UpperBound))
        {
            lowerBound = new Bound<T>(BoundType.Lower
                , object.Equals(a.LowerBound.Value, b.LowerBound.Value)
                    ? Bound.Min(a.LowerBound.Direction, b.LowerBound.Direction)
                    : a.LowerBound.Direction
                , a.LowerBound.Value);
            upperBound = new Bound<T>(BoundType.Upper
                , object.Equals(a.UpperBound.Value, b.UpperBound.Value)
                    ? Bound.Min(a.UpperBound.Direction, b.UpperBound.Direction)
                    : a.UpperBound.Direction
                , a.UpperBound.Value);
        }

        //    a
        //---------
        //        b
        //  -------------
        else if (object.Equals(lowestBound, a.LowerBound)
            && object.Equals(upmostValue, b.UpperBound))
        {
            lowerBound = new Bound<T>(BoundType.Lower
                , object.Equals(a.LowerBound.Value, b.LowerBound.Value)
                    ? Bound.Min(a.LowerBound.Direction, b.LowerBound.Direction)
                    : b.LowerBound.Direction
                , b.LowerBound.Value);

            upperBound = new Bound<T>(BoundType.Upper
                , object.Equals(a.UpperBound.Value, b.UpperBound.Value)
                    ? Bound.Min(a.UpperBound.Direction, b.UpperBound.Direction)
                    : a.UpperBound.Direction
                , a.UpperBound.Value);
        }

        //        a
        //  -------------            
        //    b
        //---------
        else if (object.Equals(lowestBound, b.LowerBound)
            && object.Equals(upmostValue, a.UpperBound))
        {
            lowerBound = new Bound<T>(BoundType.Lower
                , object.Equals(a.LowerBound.Value, b.LowerBound.Value)
                    ? Bound.Min(a.LowerBound.Direction, b.LowerBound.Direction)
                    : a.LowerBound.Direction
                , a.LowerBound.Value);

            upperBound = new Bound<T>(BoundType.Upper
                , object.Equals(a.UpperBound.Value, b.UpperBound.Value)
                    ? Bound.Min(a.UpperBound.Direction, b.UpperBound.Direction)
                    : b.UpperBound.Direction
                , b.UpperBound.Value);
        }
        else
        {
            return Interval.Empty<T>();
        }

        return TryParse(lowerBound, upperBound, out var result)
            ? result
            : Interval.Empty<T>();
    }

    /// <summary>
    /// Determines the intersection of an <see cref="IntervalCollection&lt;T&gt;"/>.
    /// </summary>
    /// <typeparam name="T">The value type of the intervals.</typeparam>
    /// <param name="collection">A collection of <see cref="Interval&lt;T&gt;"/>.</param>
    /// <returns>An <see cref="Interval&lt;T&gt;"/> that contains elements belonging to all intervals in the <paramref name="collection"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
    internal static Interval<T> IntersectionOf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(IntervalCollection<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        if (collection.Count == 0)
        {
            return Interval.Empty<T>();
        }

        var a = collection[0];
        for (var i = 1; i < collection.Count; i++)
        {
            var b = collection[i];
            a = Interval.IntersectionOf(a, b);
        }
        return a;
    }

    #endregion

    #region Union

    /// <summary>
    /// Determines the union of several intervals.
    /// </summary>
    /// <typeparam name="T">The value type of the intervals.</typeparam>
    /// <param name="args">The <see cref="Interval&lt;T&gt;"/> arguments.</param>
    /// <returns>An <see cref="IntervalCollection&lt;T&gt;"/> that contains all distinct elements of the specified intervals.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="args"/> is <c>null</c>.</exception>
    public static IntervalCollection<T> UnionOf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(params Interval<T>[] args)
    {
        return UnionOf(UnionDenominator.Highest, args);
    }

    /// <summary>
    /// Determines the union of several intervals according to the specified <paramref name="denominator"/>.
    /// </summary>
    /// <typeparam name="T">The value type of the intervals.</typeparam>
    /// <param name="denominator">The denominator considered while performing the union.</param>
    /// <param name="args">The <see cref="Interval&lt;T&gt;"/> arguments.</param>
    /// <returns>An <see cref="IntervalCollection&lt;T&gt;"/> that contains all distinct elements of the specified intervals.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="args"/> is <c>null</c>.</exception>
    /// <remarks>
    /// While performing an <see cref="Interval.UnionOf{T}(Interval{T}[])">UnionOf</see> intervals, depending of the specified <see cref="UnionDenominator"/>, the result will be different.
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
    public static IntervalCollection<T> UnionOf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(UnionDenominator denominator, params Interval<T>[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        return (args.Length == 2)
            ? (denominator == UnionDenominator.Lowest)
                ? LowUnionOf(args[0], args[1])
                : HighUnionOf(args[0], args[1])
            : (denominator == UnionDenominator.Lowest)
                ? LowUnionOf(new IntervalCollection<T>(args))
                : HighUnionOf(new IntervalCollection<T>(args));
    }

    private static IntervalCollection<T> LowUnionOf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(Interval<T> a, Interval<T> b)
    {
        if (Interval.IsNullOrEmpty(a))
        {
            return new IntervalCollection<T>(b);
        }

        if (Interval.IsNullOrEmpty(b))
        {
            return new IntervalCollection<T>(a);
        }

        var result = new List<Interval<T>>(3);

        result.AddRange(a.DifferenceWith(b));
        result.AddRange(b.DifferenceWith(a));

        if (TryIntersectionOf(a, b, out var intersection))
        {
            result.Add(intersection);
        }

        result.Sort();

        return new IntervalCollection<T>(result);
    }

    private static IntervalCollection<T> HighUnionOf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(Interval<T> a, Interval<T> b)
    {
        if (Interval.IsNullOrEmpty(a))
        {
            return new IntervalCollection<T>(b);
        }

        if (Interval.IsNullOrEmpty(b))
        {
            return new IntervalCollection<T>(a);
        }

        var lowestBound = Bound.Min(a.LowerBound, b.LowerBound);
        var upmostValue = Bound.Max(a.UpperBound, b.UpperBound);

        Bound<T> lowerBound;
        Bound<T> upperBound;

        //      a
        //-------------
        //      b
        //  ---------
        if (object.Equals(lowestBound, a.LowerBound)
            && object.Equals(upmostValue, a.UpperBound))
        {
            lowerBound = new Bound<T>(BoundType.Lower
                , object.Equals(a.LowerBound.Value, b.LowerBound.Value)
                    ? Bound.Max(a.LowerBound.Direction, b.LowerBound.Direction)
                    : a.LowerBound.Direction
                , a.LowerBound.Value);
            upperBound = new Bound<T>(BoundType.Upper
                , object.Equals(a.UpperBound.Value, b.UpperBound.Value)
                    ? Bound.Max(a.UpperBound.Direction, b.UpperBound.Direction)
                    : a.UpperBound.Direction
                , a.UpperBound.Value);
        }

        //      a
        //  ---------
        //      b
        //-------------
        else if (object.Equals(lowestBound, b.LowerBound)
            && object.Equals(upmostValue, b.UpperBound))
        {
            lowerBound = new Bound<T>(BoundType.Lower
                , object.Equals(a.LowerBound.Value, b.LowerBound.Value)
                    ? Bound.Max(a.LowerBound.Direction, b.LowerBound.Direction)
                    : b.LowerBound.Direction
                , b.LowerBound.Value);
            upperBound = new Bound<T>(BoundType.Upper
                , object.Equals(a.UpperBound.Value, b.UpperBound.Value)
                    ? Bound.Max(a.UpperBound.Direction, b.UpperBound.Direction)
                    : b.UpperBound.Direction
                , b.UpperBound.Value);
        }

        //    a
        //---------
        //        b
        //  -------------
        else if (object.Equals(lowestBound, a.LowerBound) &&
            object.Equals(upmostValue, b.UpperBound) &&
            a.UpperBound.UnionCompareTo(b.LowerBound) >= 0)
        {
            lowerBound = new Bound<T>(BoundType.Lower
                , object.Equals(a.LowerBound.Value, b.LowerBound.Value)
                    ? Bound.Max(a.LowerBound.Direction, b.LowerBound.Direction)
                    : a.LowerBound.Direction
                , a.LowerBound.Value);

            upperBound = new Bound<T>(BoundType.Upper
                , object.Equals(a.UpperBound.Value, b.UpperBound.Value)
                    ? Bound.Max(a.UpperBound.Direction, b.UpperBound.Direction)
                    : b.UpperBound.Direction
                , b.UpperBound.Value);
        }

        //        a
        //  -------------            
        //    b
        //---------
        else if (object.Equals(lowestBound, b.LowerBound) &&
            object.Equals(upmostValue, a.UpperBound) &&
            b.UpperBound.UnionCompareTo(a.LowerBound) >= 0)
        {
            lowerBound = new Bound<T>(BoundType.Lower
                , object.Equals(a.LowerBound.Value, b.LowerBound.Value)
                    ? Bound.Max(a.LowerBound.Direction, b.LowerBound.Direction)
                    : b.LowerBound.Direction
                , b.LowerBound.Value);

            upperBound = new Bound<T>(BoundType.Upper
                , object.Equals(a.UpperBound.Value, b.UpperBound.Value)
                    ? Bound.Max(a.UpperBound.Direction, b.UpperBound.Direction)
                    : a.UpperBound.Direction
                , a.UpperBound.Value);
        }
        else
        {
            return new IntervalCollection<T>(a, b);
        }

        return TryParse(lowerBound, upperBound, out var result)
            ? new IntervalCollection<T>(result)
            : new IntervalCollection<T>(a, b);
    }

    internal static IntervalCollection<T> LowUnionOf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(IntervalCollection<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        if (collection.Count == 0)
        {
            return collection;
        }

        var sortedCol = new List<Interval<T>>(collection);
        sortedCol.Sort();

        var result = new List<Interval<T>> { sortedCol[0] };
        for (var i = 1; i < sortedCol.Count; i++)
        {
            var interval = sortedCol[i];
            var col = new List<Interval<T>>(result);
            result = new List<Interval<T>>(sortedCol.Count);
            foreach (var item in col)
            {
                var u = Interval.LowUnionOf(item, interval);
                if (u.Count != 1)
                {
                    result.AddRange(u);
                }
                else
                {
                    result.RemoveAll(match => match == item || match == interval);
                    result.AddRange(u);
                    interval = u[0];
                }
            }
            result = [.. result.Distinct()];
        }

        result.Sort();
        return new IntervalCollection<T>(result);
    }

    internal static IntervalCollection<T> HighUnionOf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(IntervalCollection<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        if (collection.Count == 0)
        {
            return collection;
        }

        var result = new List<Interval<T>> { collection[0] };
        for (var i = 1; i < collection.Count; i++)
        {
            var interval = collection[i];
            var col = new List<Interval<T>>(result);
            result = new List<Interval<T>>(collection.Count);
            foreach (var item in col)
            {
                var u = Interval.HighUnionOf(item, interval);
                if (u.Count != 1)
                {
                    result.AddRange(u);
                }
                else
                {
                    result.RemoveAll(match => match == item || match == interval);
                    result.AddRange(u);
                    interval = u[0];
                }
            }
            result = [.. result.Distinct()];
        }

        result.Sort();
        return new IntervalCollection<T>(result);
    }

    #endregion

    internal static bool TryParse<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(Bound<T>? lowerBound, Bound<T>? upperBound, out Interval<T> result)
    {
        result = default!;

        if (lowerBound is null || upperBound is null || lowerBound > upperBound)
        {
            return false;
        }

        if (object.Equals(lowerBound.Value, upperBound.Value)
            && !object.Equals(lowerBound.Direction, upperBound.Direction)
            && !Bound.IsInfinity(lowerBound.Value))
        {
            return false;
        }

        result = new Interval<T>(lowerBound, upperBound);
        return true;
    }

    internal static bool TryParse<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(BoundDirection lowerDirection
        , T lowerValue
        , T upperValue
        , BoundDirection upperDirection
        , out Interval<T> result)
    {
        if (Bound.TryParse(BoundType.Lower, lowerDirection, lowerValue, out var lowerBound) &&
            Bound.TryParse(BoundType.Upper, upperDirection, upperValue, out var upperBound))
        {
            return Interval.TryParse(lowerBound, upperBound, out result);
        }

        result = default!;
        return false;
    }
}
