using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Arc4u;

/// <summary>
/// Helper class to specify and apply includes on a IQueryable&gt;T&lt; object.
/// </summary>
/// <typeparam name="T">A class of the domain model.</typeparam>
[DataContract(Namespace = "uri://arc4u.graph", Name = "GraphOf{0}")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class Graph<T> where T : class
{
    private const string stringTypeNotAllowed = "Including a type of string is not allowed when you try to define a graph path.";
    private const string StructTypeNotAllowed = "Including a struct type is not allowed when you try to define a graph path.";
    private const string OnlyLevelOneIsAllowed = "It is not allowed to check more than one level!";
    private const string PathsUseReflection = "The include paths are checked by looking up the properties by name with reflection: trimming can remove them.";
    private const string ParseUsesReflection = "Parse reads the properties of the instance with reflection: trimming can remove them.";

    [DataMember(Name = "Includes")]
    private List<string> _includes;

    /// <summary>
    /// Create a Graph instance based on the typeof(T).
    /// </summary>
    public Graph()
    {
        _includes = [];
    }

    /// <summary>
    /// Create a Graph instance based on the typeof(T).
    /// </summary>
    /// <param name="paths">The list of initials includes you want to predefined!</param>
    [RequiresUnreferencedCode(PathsUseReflection)]
    public Graph(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        Graph<T>.ValidatePaths(paths);

        _includes = new List<string>(paths);
    }

    // For a subset of the paths of this graph, which were validated when they were added.
    private static Graph<T> FromValidatedPaths(List<string> paths) => new() { _includes = paths };

    // An IEnumerable implemented by the type, not the non-generic IEnumerable interface itself.
    private static bool IsCollection(Type type) => type != typeof(IEnumerable) && typeof(IEnumerable).IsAssignableFrom(type);

    // True when the include is the path itself or one of its sub paths: "Departments.Manager" is under "Departments" but not under "Department".
    private static bool IsSameOrUnder(string include, string path, StringComparison comparison)
    {
        if (path.Length == 0)
        {
            return true;
        }

        return include.StartsWith(path, comparison) && (include.Length == path.Length || include[path.Length] == '.');
    }

    // The type the paths start from: the item type for a graph of a collection (like the one GetGraph returns for a collection property), otherwise T.
    private static Type RootType => ItemType(typeof(T));

    // Unwraps arrays and generic collections, also nested ones like IEnumerable<Employee[]>, down to their item type.
    private static Type ItemType(Type type)
    {
        if (type.IsArray)
        {
            return ItemType(type.GetElementType()!);
        }

        if (IsCollection(type) && type.IsGenericType && type.GenericTypeArguments.Length == 1)
        {
            return ItemType(type.GenericTypeArguments[0]);
        }

        return type;
    }

    [RequiresUnreferencedCode(PathsUseReflection)]
    private static void ValidatePaths(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            var type = RootType;
            var properties = path.Split('.');
            foreach (var property in properties)
            {
                var propertyInfo = type.GetRuntimeProperty(property);
                if (null == propertyInfo)
                {
                    throw new MissingMemberException(string.Format(CultureInfo.InvariantCulture, "The property {0} does not exist!", property));
                }

                if (propertyInfo.PropertyType.IsArray)
                {
                    type = propertyInfo.PropertyType.GetElementType();

                    if (null == type)
                    {
                        throw new MissingMemberException(string.Format(CultureInfo.InvariantCulture, "The property {0} doesn't exist!", property));
                    }
                    if (type == typeof(string))
                    {
                        throw new MissingMemberException(string.Format(CultureInfo.InvariantCulture, "The property {0} is a simple type!", property));
                    }

                    if (!type.GetTypeInfo().IsClass)
                    {
                        throw new MissingMemberException(string.Format(CultureInfo.InvariantCulture, "The property {0} is a simple type!", property));
                    }

                    continue;
                }

                if (IsCollection(propertyInfo.PropertyType))
                {
                    if (!propertyInfo.PropertyType.GetTypeInfo().IsGenericType)
                    {
                        throw new MissingMemberException(string.Format(CultureInfo.InvariantCulture, "The property {0} is not a generic collection!", property));
                    }

                    var types = propertyInfo.PropertyType.GenericTypeArguments;
                    foreach (var t in types)
                    {
                        if (t == typeof(string))
                        {
                            throw new MissingMemberException(string.Format(CultureInfo.InvariantCulture, "The property {0} is a simple type!", property));
                        }

                        if (!t.GetTypeInfo().IsClass)
                        {
                            throw new MissingMemberException(string.Format(CultureInfo.InvariantCulture, "The property {0} is a simple type!", property));
                        }
                    }

                    if (types.Length > 1)
                    {
                        throw new MissingMemberException(string.Format(CultureInfo.InvariantCulture, "Simple Collection is allowed, property {0} has more than one type.", property));
                    }

                    type = types[0];
                    continue;
                }

                type = propertyInfo.PropertyType;

                if (type == typeof(string))
                {
                    throw new MissingMemberException(string.Format(CultureInfo.InvariantCulture, "The property {0} is a simple type!", property));
                }

                if (!type.GetTypeInfo().IsClass)
                {
                    throw new MissingMemberException(string.Format(CultureInfo.InvariantCulture, "The property {0} is a simple type!", property));
                }
            }
        }
    }

    private string DebuggerDisplay
    {
        get
        {
            var info = new StringBuilder();
            info.Append(string.Format(CultureInfo.InvariantCulture, "{0}: ", typeof(T).Name));
            var includes = Includes;
            if (includes.Count == 0)
            {
                info.Append("- No records.");
                return info.ToString();
            }

            if (Includes.Count == 1)
            {
                info.Append(includes[0]);
            }
            else
            {
                info.Append(includes[0]);
                info.Append(", ");
                for (var i = 1; i < includes.Count - 1; i++)
                {
                    info.Append(string.Format(CultureInfo.InvariantCulture, "{0}, ", includes[i]));
                }
                info.Append(includes[includes.Count - 1]);
            }
            return info.ToString();
        }
    }

    /// <summary>
    /// Let you add new <see cref="MemberExpression"/> you want to include in the object's graph.
    /// </summary>
    /// <param name="path">The <see cref="MemberExpression"/> you want to check.</param>
    /// <returns>The </returns>
    public Graph<T> Include<TProperty>(Expression<Func<T, TProperty>> path) where TProperty : class
    {
        var stringPath = EvaluateExpression(path);

        if (!string.IsNullOrWhiteSpace(stringPath) && stringPath[0] == '.')
        {
            stringPath = stringPath[1..];
        }

        if (!string.IsNullOrWhiteSpace(stringPath) && !_includes.Exists(i => i == stringPath))
        {
            _includes.Add(stringPath);
        }

        return this;

    }

    /// <summary>
    /// Converts an expression (member access, method call such as <c>Select</c>, or lambda) into the dotted path of the members it references.
    /// </summary>
    /// <param name="path">The expression to evaluate.</param>
    /// <returns>The path, for example <c>.Orders.Lines</c>, or an empty string when the expression is not supported.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="MemberAccessException">The path targets a <see cref="string"/>, or a value type, which cannot be included.</exception>
    public static string EvaluateExpression(Expression path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var stringPath = string.Empty;
        if (path is MemberExpression memberExpression)
        {
            // Check if type is System.string. because string is of type Class and the include allows to parse member of type Class. But we are not interested to see the string type.
            if (memberExpression.Type == typeof(string))
            {
                throw new MemberAccessException(stringTypeNotAllowed);
            }

            if (memberExpression.Type.IsArray)
            {
                if (memberExpression.Type.GetElementType() == typeof(string))
                {
                    throw new MemberAccessException(stringTypeNotAllowed);
                }

                if (!memberExpression.Type.GetElementType()!.GetTypeInfo().IsClass)
                {
                    throw new MemberAccessException(StructTypeNotAllowed);
                }
            }
            if (IsCollection(memberExpression.Type))
            {
                var types = memberExpression.Type.GenericTypeArguments;

                foreach (var type in types)
                {
                    if (type == typeof(string))
                    {
                        throw new MemberAccessException(stringTypeNotAllowed);
                    }

                    if (!type.GetTypeInfo().IsClass)
                    {
                        throw new MemberAccessException(StructTypeNotAllowed);
                    }
                }
            }

            var s = memberExpression.ToString();
            stringPath += s.Substring(s.IndexOf('.'));
        }
        else
        {
            var methodCallExpression = path as MethodCallExpression;

            if (null != methodCallExpression)
            {
                stringPath = methodCallExpression.Arguments.Aggregate(stringPath, (current, argument) => current + EvaluateExpression(argument));
            }
            else
            {
                var lambdaExpression = path as LambdaExpression;
                if (lambdaExpression != null)
                {
                    stringPath = EvaluateExpression(lambdaExpression.Body) + stringPath;
                }
            }
        }
        return stringPath;
    }

    /// <summary>
    /// Let you check if a member defined in the path expression was included in the object's graph.
    /// </summary>
    /// <param name="path">The <see cref="MemberExpression"/> you want to check.</param>
    /// <returns>True if the path defined was included in the object's graph.</returns>
    public bool Exist<TProperty>(Expression<Func<T, TProperty>> path) where TProperty : class
    {
        ArgumentNullException.ThrowIfNull(path);

        var stringPath = EvaluateExpression(path);

        if (!string.IsNullOrWhiteSpace(stringPath) && stringPath[0] == '.')
        {
            stringPath = stringPath.Substring(1);
        }

        return _includes.Exists(s => IsSameOrUnder(s, stringPath, StringComparison.Ordinal));
    }

    /// <summary>
    /// If you have a member with deep graphing, this method gives you a new graph for the specified member. As the delegation of graph is allowed only by one level step, only the direct member selection is allowed.
    /// </summary>
    /// <typeparam name="TProperty">The member where wee need to extract the graph.</typeparam>
    /// <param name="path">The <see cref="MemberExpression"/> used to retrieve the name and the type used for the new Graph.</param>
    /// <returns>A new Graph instance with the type of the property selected in the path parameter.</returns>
    [RequiresUnreferencedCode(PathsUseReflection)]
    public Graph<TProperty> GetGraph<TProperty>(Expression<Func<T, TProperty>> path) where TProperty : class
    {
        ArgumentNullException.ThrowIfNull(path);

        var stringPath = EvaluateExpression(path);

        if (!string.IsNullOrWhiteSpace(stringPath) && stringPath[0] == '.')
        {
            stringPath = stringPath.Substring(1);
        }

        if (stringPath.Contains('.'))
        {
            throw new InvalidOperationException(OnlyLevelOneIsAllowed);
        }

        var q = (from s in _includes
                 let paths = s.Split('.').ToList()
                 where paths[0] == stringPath && paths.Count > 1
                 select s.Substring(s.IndexOf('.') + 1)).ToList();

        return new Graph<TProperty>(q);
    }

    /// <summary>
    /// Parse an existing graph based on a object instance.
    /// </summary>
    /// <param name="instance">The object to parse.</param>
    /// <param name="shouldHaveElement"></param>
    /// <returns>The Graph for the dedicated object parsed.</returns>
    [RequiresUnreferencedCode(ParseUsesReflection)]
    public static Graph<T> Parse(T instance, bool shouldHaveElement = false)
    {
        ArgumentNullException.ThrowIfNull(instance);

        // The instances being parsed, from the root to the current one: a back reference to one of them is not followed, otherwise a cyclic graph never ends.
        var ancestors = new HashSet<object>(ReferenceEqualityComparer.Instance);

        return shouldHaveElement ? new Graph<T>(ParseObject2(instance, typeof(T), ancestors)) : new Graph<T>(ParseObject(instance, typeof(T), ancestors));
    }

    /// <summary>
    /// Create a new Graph object where the paths starting with the path expression are removed.
    /// </summary>
    /// <typeparam name="TProperty">The property to remove</typeparam>
    /// <param name="path">The expression property to remove.</param>
    /// <returns>A copy of the graph with the removed path.</returns>
    public Graph<T> RemoveGraph<TProperty>(Expression<Func<T, TProperty>> path) where TProperty : class
    {
        ArgumentNullException.ThrowIfNull(path);

        var stringPath = EvaluateExpression(path);

        if (!string.IsNullOrWhiteSpace(stringPath) && stringPath[0] == '.')
        {
            stringPath = stringPath.Substring(1);
        }

        // remove from the Includes, all the includes containig the path.
        var l = _includes.Where(i => !IsSameOrUnder(i, stringPath, StringComparison.Ordinal)).ToList();

        // A subset of paths already validated: no need to check them again with reflection.
        return FromValidatedPaths(l);
    }

    [RequiresUnreferencedCode(ParseUsesReflection)]
    private static IEnumerable<string> ParseObject(object o, Type type, HashSet<object> ancestors)
    {
        var result = new List<string>();

        ArgumentNullException.ThrowIfNull(o);

        if (o is IEnumerable)
        {
            throw new InvalidOperationException("Collection is not allowed");
        }

        // The declared type, not the runtime one: the properties a subclass adds are not part of the paths the graph can validate.

        if (!type.GetTypeInfo().IsClass)
        {
            throw new InvalidOperationException("Class is not allowed");
        }

        if (type.IsArray)
        {
            throw new InvalidOperationException("Array is not allowed");
        }

        var properties = type.GetRuntimeProperties();

        ancestors.Add(o);

        foreach (var property in properties)
        {

            object? oProperty;
            if (property.PropertyType.IsArray)
            {
                if (property.PropertyType.GetElementType() == typeof(string))
                {
                    continue;
                }

                if (!property.PropertyType.GetElementType()!.GetTypeInfo().IsClass)
                {
                    continue;
                }

                // Check if type instance is not null.
                oProperty = property.GetValue(o, null);

                var array = oProperty as Array;

                if (null == array)
                {
                    continue;
                }

                if (array.Length == 0)
                {
                    result.Add(property.Name);
                    continue;
                }

                var item = array.GetValue(0)!;
                if (ancestors.Contains(item))
                {
                    continue;
                }

                var children = ParseObject(item, property.PropertyType.GetElementType()!, ancestors);
                if (null == children || !children.Any())
                {
                    result.Add(property.Name);
                }
                else
                {
                    foreach (var child in children)
                    {
                        result.Add(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", property.Name, child));
                    }
                }

                continue;
            }

            if (IsCollection(property.PropertyType))
            {
                if (!property.PropertyType.GetTypeInfo().IsGenericType || property.PropertyType.GenericTypeArguments.Length != 1)
                {
                    continue;
                }

                var itemType = property.PropertyType.GenericTypeArguments[0];
                if (itemType == typeof(string) || !itemType.GetTypeInfo().IsClass)
                {
                    continue;
                }

                oProperty = property.GetValue(o, null);
                var enumerable = oProperty as IEnumerable;
                if (null != enumerable)
                {
                    var enumerator = enumerable.GetEnumerator();
                    if (enumerator.MoveNext() && !ancestors.Contains(enumerator.Current))
                    {
                        var children = ParseObject(enumerator.Current, itemType, ancestors);
                        if (!children.Any())
                        {
                            result.Add(property.Name);
                        }
                        else
                        {
                            foreach (var child in children)
                            {
                                result.Add(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", property.Name, child));
                            }
                        }
                    }
                    else
                    {
                        result.Add(property.Name);
                    }
                }
                continue;
            }

            if (property.PropertyType == typeof(string))
            {
                continue;
            }

            if (!property.PropertyType.GetTypeInfo().IsClass)
            {
                continue;
            }

            // Check if type instance is not null.
            oProperty = property.GetValue(o, null);

            if (null != oProperty && !ancestors.Contains(oProperty))
            {
                var children = ParseObject(oProperty, property.PropertyType, ancestors);
                if (!children.Any())
                {
                    result.Add(property.Name);
                }
                else
                {
                    foreach (var child in children)
                    {
                        result.Add(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", property.Name, child));
                    }
                }
            }
        }

        ancestors.Remove(o);

        return result;
    }

    [RequiresUnreferencedCode(ParseUsesReflection)]
    private static IEnumerable<string> ParseObject2(object o, Type type, HashSet<object> ancestors)
    {
        var result = new List<string>();

        ArgumentNullException.ThrowIfNull(o);

        if (o is IEnumerable)
        {
            throw new InvalidOperationException("Collection is not allowed");
        }

        // The declared type, not the runtime one: the properties a subclass adds are not part of the paths the graph can validate.

        if (!type.GetTypeInfo().IsClass)
        {
            throw new InvalidOperationException("Only class type is allowed");
        }

        if (type.IsArray)
        {
            throw new InvalidOperationException("Array type is not allowed");
        }

        var properties = type.GetRuntimeProperties();

        ancestors.Add(o);

        foreach (var property in properties)
        {
            object? oProperty;
            if (property.PropertyType.IsArray)
            {
                if (property.PropertyType.GetElementType() == typeof(string))
                {
                    continue;
                }

                if (!property.PropertyType.GetElementType()!.GetTypeInfo().IsClass)
                {
                    continue;
                }

                // Check if type instance is not null.
                oProperty = property.GetValue(o, null);

                var array = oProperty as Array;

                if (null == array)
                {
                    continue;
                }

                if (array.Length == 0) // we graph here if an item is added to the list. If not we consider this is the same as null!
                {
                    continue;
                }

                var item = array.GetValue(0)!;
                if (ancestors.Contains(item))
                {
                    continue;
                }

                var children = ParseObject2(item, property.PropertyType.GetElementType()!, ancestors);
                if (null == children || !children.Any())
                {
                    result.Add(property.Name);
                }
                else
                {
                    foreach (var child in children)
                    {
                        result.Add(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", property.Name, child));
                    }
                }

                continue;
            }

            if (IsCollection(property.PropertyType))
            {
                if (!property.PropertyType.GetTypeInfo().IsGenericType || property.PropertyType.GenericTypeArguments.Length != 1)
                {
                    continue;
                }

                var itemType = property.PropertyType.GenericTypeArguments[0];
                if (itemType == typeof(string) || !itemType.GetTypeInfo().IsClass)
                {
                    continue;
                }

                oProperty = property.GetValue(o, null);
                var enumerable = oProperty as IEnumerable;
                if (null != enumerable)
                {
                    var enumerator = enumerable.GetEnumerator();
                    if (enumerator.MoveNext() && !ancestors.Contains(enumerator.Current))
                    {
                        var children = ParseObject2(enumerator.Current, itemType, ancestors);
                        if (!children.Any())
                        {
                            result.Add(property.Name);
                        }
                        else
                        if (children.Any())
                        {
                            foreach (var child in children)
                            {
                                result.Add(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", property.Name, child));
                            }
                        }
                    }
                }
                continue;
            }

            if (property.PropertyType == typeof(string))
            {
                continue;
            }

            if (!property.PropertyType.GetTypeInfo().IsClass)
            {
                continue;
            }

            // Check if type instance is not null.
            oProperty = property.GetValue(o, null);

            if (null != oProperty && !ancestors.Contains(oProperty))
            {
                var children = ParseObject2(oProperty, property.PropertyType, ancestors);
                if (!children.Any())
                {
                    result.Add(property.Name);
                }
                else
                {
                    foreach (var child in children)
                    {
                        result.Add(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", property.Name, child));
                    }
                }
            }
        }

        ancestors.Remove(o);

        return result;
    }

    /// <summary>
    /// The read collection of includes defined.
    /// </summary>

    public IList<string> Includes
    {
        get
        {
            // Clone it before.
            return new List<string>(_includes);
        }
        [RequiresUnreferencedCode(PathsUseReflection)]
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            Graph<T>.ValidatePaths(value);

            _includes = new List<string>(value);
        }
    }

}
