using System.Collections;
using System.Reflection;

namespace System.Linq;

/// <summary>
/// A lazily evaluated "switch" over a sequence: each element of the source is tested against the registered cases, in registration order,
/// and the first matching case produces the result for that element. Elements that match no case are skipped.
/// </summary>
/// <typeparam name="TSource">The type of the elements of the source sequence.</typeparam>
/// <typeparam name="TResult">The type of the produced elements.</typeparam>
/// <param name="source">The sequence to project.</param>
/// <example>
/// <code language="csharp">
/// var labels = new object[] { 1, "text", 2.5 }
///     .Case&lt;object, string&gt;(x =&gt; x is int, x =&gt; "an int")
///     .Case(typeof(string), x =&gt; "a string")
///     .ToList(); // ["an int", "a string"]: 2.5 matches no case and is skipped.
/// </code>
/// </example>
public class Switch<TSource, TResult>(IEnumerable<TSource> source) : IEnumerable<TResult>
{
    #region nested classes
    private class CaseSelector<TSelectorSource, TSelectorResult>(Func<TSource, bool> predicate, Func<TSource, TResult> selector)
    {
        public bool CanSelect(TSource source) { return predicate(source); }

        public TResult Select(TSource source) { return selector(source); }
    }

    private sealed class CaseSelector<TSelectorSource, TCase, TSelectorResult>(Func<TCase, TResult> selector) : CaseSelector<TSelectorSource, TSelectorResult>(
        x => x is TCase,
        x => x is TCase tCase ? selector(tCase) : throw new InvalidOperationException("Invalid case type")
        )
    {
    }

    #endregion
    private readonly IList<CaseSelector<TSource, TResult>> casePredicates = [];

    /// <summary>
    /// Adds a case selected by a predicate.
    /// </summary>
    /// <param name="predicate">Returns <see langword="true"/> when the element belongs to this case.</param>
    /// <param name="selector">Produces the result for an element that belongs to this case.</param>
    /// <returns>The same <see cref="Switch{TSource, TResult}"/>, to chain calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> or <paramref name="selector"/> is <see langword="null"/>.</exception>
    public Switch<TSource, TResult> Case(Func<TSource, bool> predicate, Func<TSource, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));
        ArgumentNullException.ThrowIfNull(selector, nameof(selector));

        casePredicates.Add(new CaseSelector<TSource, TResult>(predicate, selector));

        return this;
    }

    /// <summary>
    /// Adds a case selected by type: the case applies to the elements whose runtime type is assignable to <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The type of the elements that belong to this case.</param>
    /// <param name="selector">Produces the result for an element that belongs to this case.</param>
    /// <returns>The same <see cref="Switch{TSource, TResult}"/>, to chain calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="selector"/> is <see langword="null"/>.</exception>
    public Switch<TSource, TResult> Case(Type type, Func<TSource, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(selector, nameof(selector));

        casePredicates.Add(new CaseSelector<TSource, TResult>(
            x => type?.GetTypeInfo().IsAssignableFrom(x?.GetType().GetTypeInfo()) ?? false,
            selector
        ));
        return this;
    }

    /// <summary>
    /// Adds a case selected by type: the case applies to the elements that are a <typeparamref name="TCase"/>, and the selector receives them already converted to that type.
    /// </summary>
    /// <typeparam name="TCase">The type of the elements that belong to this case.</typeparam>
    /// <param name="selector">Produces the result for an element that belongs to this case.</param>
    /// <returns>The same <see cref="Switch{TSource, TResult}"/>, to chain calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    public Switch<TSource, TResult> Case<TCase>(Func<TCase, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector, nameof(selector));

        casePredicates.Add(new CaseSelector<TSource, TCase, TResult>(selector));

        return this;
    }

    #region IEnumerable<TResult> Members
    /// <summary>
    /// Enumerates the source and returns, for each element matching a case, the result of the first matching case.
    /// </summary>
    /// <returns>An enumerator over the results.</returns>
    public IEnumerator<TResult> GetEnumerator()
    {
        foreach (var item in source)
        {
            var switchCase = casePredicates.FirstOrDefault(x => x.CanSelect(item));

            if (switchCase != null)
            {
                yield return switchCase.Select(item);
            }
        }
    }
    #endregion

    #region IEnumerable Members
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion
}

/// <summary>
/// Extension methods to start a <see cref="Switch{TSource, TResult}"/> on any sequence.
/// </summary>
public static class SwitchExtensions
{
    /// <summary>
    /// Starts a <see cref="Switch{TSource, TResult}"/> on a sequence with a first case selected by a predicate.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of the source sequence.</typeparam>
    /// <typeparam name="TResult">The type of the produced elements.</typeparam>
    /// <param name="source">The sequence to project.</param>
    /// <param name="predicate">Returns <see langword="true"/> when the element belongs to the case.</param>
    /// <param name="selector">Produces the result for an element that belongs to the case.</param>
    /// <returns>A <see cref="Switch{TSource, TResult}"/> to which other cases can be added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/>, <paramref name="predicate"/> or <paramref name="selector"/> is <see langword="null"/>.</exception>
    public static Switch<TSource, TResult> Case<TSource, TResult>(this IEnumerable<TSource> source,
        Func<TSource, bool> predicate,
        Func<TSource, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));
        ArgumentNullException.ThrowIfNull(selector, nameof(selector));

        return new Switch<TSource, TResult>(source).Case(predicate, selector);
    }

    /// <summary>
    /// Starts a <see cref="Switch{TSource, TResult}"/> on a sequence with a first case selected by type.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of the source sequence.</typeparam>
    /// <typeparam name="TResult">The type of the produced elements.</typeparam>
    /// <param name="source">The sequence to project.</param>
    /// <param name="type">The type of the elements that belong to the case.</param>
    /// <param name="selector">Produces the result for an element that belongs to the case.</param>
    /// <returns>A <see cref="Switch{TSource, TResult}"/> to which other cases can be added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/>, <paramref name="type"/> or <paramref name="selector"/> is <see langword="null"/>.</exception>
    public static Switch<TSource, TResult> Case<TSource, TResult>(this IEnumerable<TSource> source,
        Type type,
        Func<TSource, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(selector, nameof(selector));

        return new Switch<TSource, TResult>(source).Case(type, selector);
    }

    /// <summary>
    /// Starts a <see cref="Switch{TSource, TResult}"/> on a sequence, without any case.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of the source sequence.</typeparam>
    /// <typeparam name="TResult">The type of the produced elements.</typeparam>
    /// <param name="source">The sequence to project.</param>
    /// <returns>A <see cref="Switch{TSource, TResult}"/> to which cases can be added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Switch<TSource, TResult> AsSwitch<TSource, TResult>(this IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new Switch<TSource, TResult>(source);
    }
}

/// <summary>
/// Provides a set of static methods extending the <see cref="System.Collections.Generic.IEnumerable&lt;T&gt;"/> class.
/// </summary>
public static class LinqExtensions
{
    /// <summary>
    /// Filters recursively a sequence of values based on a predicate.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable&lt;T&gt;"/> to filter recursively.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="selector">A recursive function to apply to each element.</param>
    /// <returns>An <see cref="System.Collections.Generic.IEnumerable&lt;T&gt;"/> that contains elements from the input sequence that satisfy the condition.</returns>
    public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source
        , Func<TSource, bool> predicate
        , Func<TSource, IEnumerable<TSource>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(selector);

        foreach (var item in source)
        {
            if (predicate(item))
            {
                yield return item;
            }

            var innerSource = selector(item);
            if (innerSource != null)
            {
                foreach (var innerItem in Where(innerSource, predicate, selector))
                {
                    if (predicate(innerItem))
                    {
                        yield return innerItem;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Take a collection of an object of type TSource and use the getChildrenFunction to travel
    /// the structure of the collection. In return, we have a flatten collection of each nodes.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable&lt;T&gt;"/> to flatten recursively.</param>
    /// <param name="getChildrenFunction">Return the property to use to travel through the node collection.</param>
    /// <returns></returns>
    public static IEnumerable<TSource> Flatten<TSource>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TSource>> getChildrenFunction)
    {
        if (null == source)
        {
            return [];
        }
        // Add what we have to the stack
        var flattenedList = source;

        // Go through the input enumerable looking for children,
        // and add those if we have them
        foreach (var element in source)
        {
            flattenedList = flattenedList.Concat(
              getChildrenFunction(element).Flatten(getChildrenFunction)
            );
        }
        return flattenedList;
    }
}
