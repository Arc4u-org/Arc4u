using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arc4u;

/// <summary>
/// The include paths of an aggregate of type <typeparamref name="T"/>: the references and collections to load with it, such as <c>Customer.Address</c> or <c>Lines.Product</c>.
/// </summary>
/// <typeparam name="T">The aggregate root: a class (or an interface) of the domain model.</typeparam>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[JsonConverter(typeof(GraphJsonConverterFactory))]
public class Graph<T> where T : class
{
    private const string stringTypeNotAllowed = "Including a type of string is not allowed when you try to define a graph path.";
    private const string StructTypeNotAllowed = "Including a struct type is not allowed when you try to define a graph path.";
    private const string EntityTypeRequired = "Only a class, or a generic collection of one class, can be included when you try to define a graph path.";
    private const string OnlyLevelOneIsAllowed = "It is not allowed to check more than one level!";
    private const string PathsUseReflection = "The include paths are checked by looking up the properties by name with reflection: trimming can remove them.";
    private const string ParseUsesReflection = "Parse reads the properties of the instance with reflection: trimming can remove them.";

    // Serialized as "Includes" by IncludesJsonConverter: deserialization does not validate the paths, which needs reflection on T.
    private List<string> _includes;

    /// <summary>
    /// Creates an empty graph.
    /// </summary>
    public Graph()
    {
        _includes = [];
    }

    /// <summary>
    /// Creates a graph holding the given include paths, after checking them against the properties of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="paths">The include paths, such as <c>Customer.Address</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="paths"/> is <see langword="null"/>.</exception>
    /// <exception cref="MissingMemberException">A property of a path does not exist, or is not a class or a generic collection of one class.</exception>
    [RequiresUnreferencedCode(PathsUseReflection)]
    public Graph(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        // Read the paths once: the ones stored are the ones checked.
        var includes = new List<string>(paths);

        Graph<T>.ValidatePaths(includes);

        _includes = includes;
    }

    // For paths that follow the rules of ValidatePaths by construction: a subset or the sub paths of a graph, or the paths Parse builds.
    private static Graph<T> FromValidatedPaths(List<string> paths) => new() { _includes = paths };

    // An IEnumerable implemented by the type, not the non-generic IEnumerable interface itself.
    private static bool IsCollection(Type type) => type != typeof(IEnumerable) && typeof(IEnumerable).IsAssignableFrom(type);

    // The item type of an array, or of a generic collection with one type argument; null for any other type.
    private static Type? ElementTypeOf(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        return IsCollection(type) && type.IsGenericType && type.GenericTypeArguments.Length == 1 ? type.GenericTypeArguments[0] : null;
    }

    // A type a path can go through: a class that is not a string or a collection (a collection of collections is not a navigation).
    private static bool IsNavigable(Type type) => type.IsClass && !IsCollection(type);

    // True when the include is the path itself or one of its sub paths: "Departments.Manager" is under "Departments" but not under "Department".
    private static bool IsSameOrUnder(string include, string path)
    {
        if (path.Length == 0)
        {
            return true;
        }

        return include.StartsWith(path, StringComparison.Ordinal) && (include.Length == path.Length || include[path.Length] == '.');
    }

    // The path of an expression without the leading dot of EvaluateExpression: "Lines.Product".
    private static string IncludePathOf(Expression path)
    {
        var stringPath = EvaluateExpression(path);

        return stringPath.StartsWith('.') ? stringPath[1..] : stringPath;
    }

    // The public instance property of that name, from the most derived type declaring it: a property hidden with `new` is not ambiguous.
    [RequiresUnreferencedCode(PathsUseReflection)]
    private static PropertyInfo? FindProperty(Type type, string name)
    {
        for (var t = type; t is not null; t = t.BaseType)
        {
            var property = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (property is not null)
            {
                return property;
            }
        }

        return null;
    }

    // The properties a path can name, as FindProperty finds them: public instance properties with a public getter, without indexers and hidden base properties.
    [RequiresUnreferencedCode(ParseUsesReflection)]
    private static IEnumerable<PropertyInfo> PathProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetMethod is { IsPublic: true }
                        && p.GetIndexParameters().Length == 0
                        && FindProperty(type, p.Name)?.DeclaringType == p.DeclaringType);

    // The type a path starts from. A graph of a collection (like the one GetGraph returns for a collection property) describes its items,
    // unless the collection type declares the first property itself: an entity implementing IEnumerable<T> keeps its own navigation properties.
    [RequiresUnreferencedCode(PathsUseReflection)]
    private static Type RootTypeOf(string firstProperty) => FindProperty(typeof(T), firstProperty) is null ? ItemType(typeof(T)) : typeof(T);

    // Unwraps arrays and generic collections, also nested ones like IEnumerable<Employee[]>, down to their item type.
    private static Type ItemType(Type type) => ElementTypeOf(type) is { } elementType ? ItemType(elementType) : type;

    [RequiresUnreferencedCode(PathsUseReflection)]
    private static void ValidatePaths(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            var properties = path.Split('.');
            var type = RootTypeOf(properties[0]);
            foreach (var property in properties)
            {
                var propertyType = FindProperty(type, property)?.PropertyType
                                   ?? throw new MissingMemberException(string.Format(CultureInfo.InvariantCulture, "The property {0} does not exist!", property));

                // A string is a collection of chars, but a simple type here.
                if (propertyType != typeof(string) && IsCollection(propertyType))
                {
                    if (!propertyType.IsArray && !propertyType.IsGenericType)
                    {
                        throw new MissingMemberException(string.Format(CultureInfo.InvariantCulture, "The property {0} is not a generic collection!", property));
                    }

                    type = ElementTypeOf(propertyType)
                           ?? throw new MissingMemberException(string.Format(CultureInfo.InvariantCulture, "Simple Collection is allowed, property {0} has more than one type.", property));
                }
                else
                {
                    type = propertyType;
                }

                if (!IsNavigable(type))
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
    /// Adds the include path of an expression, such as <c>o =&gt; o.Customer.Address</c> or <c>o =&gt; o.Lines.Select(l =&gt; l.Product)</c>. A path already in the graph is not added again.
    /// </summary>
    /// <param name="path">The expression navigating from <typeparamref name="T"/> to the property to include.</param>
    /// <returns>This graph, to chain the calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="MemberAccessException">The path goes through a property that is not a class or a generic collection of one class.</exception>
    public Graph<T> Include<TProperty>(Expression<Func<T, TProperty>> path) where TProperty : class
    {
        var stringPath = IncludePathOf(path);

        if (!string.IsNullOrWhiteSpace(stringPath) && !_includes.Contains(stringPath))
        {
            _includes.Add(stringPath);
        }

        return this;
    }

    /// <summary>
    /// Converts an expression into the dotted path of the members it navigates from its parameter. <c>Select</c> and <c>SelectMany</c> go
    /// through a collection; the other operators, such as <c>Where</c>, <c>OrderBy</c>, <c>Take</c> or <c>First</c>, keep the path of their source.
    /// </summary>
    /// <param name="path">The expression to evaluate.</param>
    /// <returns>The path, for example <c>.Orders.Lines</c>, or an empty string when the expression does not navigate from its parameter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="MemberAccessException">The path goes through a <see cref="string"/>, a value type, or another type that is not a class or a generic collection of one class.</exception>
    public static string EvaluateExpression(Expression path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return PathOf(path) ?? string.Empty;
    }

    // The path of the members an expression navigates from a parameter, or null when it does not start from one (a constant, a captured variable).
    private static string? PathOf(Expression expression)
    {
        switch (expression)
        {
            case ParameterExpression:
                return string.Empty;

            case LambdaExpression lambda:
                return PathOf(lambda.Body);

            // The quoted lambda of a Queryable operator, or a cast.
            case UnaryExpression unary:
                return PathOf(unary.Operand);

            case MemberExpression member:
                var parent = member.Expression is null ? null : PathOf(member.Expression);
                if (parent is null)
                {
                    return null;
                }

                CheckIncludable(member.Type);

                return parent + "." + member.Member.Name;

            case MethodCallExpression call:
                var source = call.Object ?? (call.Arguments.Count > 0 ? call.Arguments[0] : null);
                var sourcePath = source is null ? null : PathOf(source);
                if (sourcePath is null)
                {
                    return null;
                }

                // The selector of Select and SelectMany navigates further; a filter, an order or a quantity does not change the path.
                if (call.Object is null && call.Arguments.Count >= 2 && call.Method.Name is nameof(Enumerable.Select) or nameof(Enumerable.SelectMany))
                {
                    return sourcePath + PathOf(call.Arguments[1]);
                }

                return sourcePath;

            default:
                return null;
        }
    }

    // The rules of ValidatePaths, for a member of an expression.
    private static void CheckIncludable(Type type)
    {
        if (type == typeof(string))
        {
            throw new MemberAccessException(stringTypeNotAllowed);
        }

        if (type.IsValueType)
        {
            throw new MemberAccessException(StructTypeNotAllowed);
        }

        if (!IsCollection(type))
        {
            if (!type.IsClass)
            {
                throw new MemberAccessException(EntityTypeRequired);
            }

            return;
        }

        var elementType = ElementTypeOf(type);
        if (elementType == typeof(string))
        {
            throw new MemberAccessException(stringTypeNotAllowed);
        }

        if (elementType is { IsValueType: true })
        {
            throw new MemberAccessException(StructTypeNotAllowed);
        }

        if (elementType is null || !IsNavigable(elementType))
        {
            throw new MemberAccessException(EntityTypeRequired);
        }
    }

    /// <summary>
    /// Checks whether the graph includes the path of an expression, or a path below it. Paths are compared by whole property names, with their casing.
    /// </summary>
    /// <param name="path">The expression navigating from <typeparamref name="T"/> to the property to check.</param>
    /// <returns><see langword="true"/> when the graph includes the path; with <c>Lines.Product</c>, <c>o =&gt; o.Lines</c> is included.</returns>
    public bool Exist<TProperty>(Expression<Func<T, TProperty>> path) where TProperty : class
    {
        var stringPath = IncludePathOf(path);

        return _includes.Exists(s => IsSameOrUnder(s, stringPath));
    }

    /// <summary>
    /// Returns the paths below a first-level property, as a graph of that property's type. For a collection property, the paths of the returned graph start at the item type.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="path">The expression selecting a property of <typeparamref name="T"/>, one level only.</param>
    /// <returns>A new graph holding the paths below the property.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="path"/> goes deeper than one property.</exception>
    public Graph<TProperty> GetGraph<TProperty>(Expression<Func<T, TProperty>> path) where TProperty : class
    {
        var stringPath = IncludePathOf(path);

        if (stringPath.Contains('.'))
        {
            throw new InvalidOperationException(OnlyLevelOneIsAllowed);
        }

        if (stringPath.Length == 0)
        {
            return new Graph<TProperty>();
        }

        var subPaths = _includes.Where(s => s.Length > stringPath.Length && IsSameOrUnder(s, stringPath))
                                .Select(s => s[(stringPath.Length + 1)..])
                                .ToList();

        // Sub paths of the paths of this graph: no need to check them again with reflection.
        return Graph<TProperty>.FromValidatedPaths(subPaths);
    }

    /// <summary>
    /// Builds the graph of what an instance holds: the paths of its references and collections that are not <see langword="null"/>.
    /// For a collection, the first item is read.
    /// </summary>
    /// <param name="instance">The object to parse.</param>
    /// <param name="shouldHaveElement"><see langword="true"/> to leave out the empty collections; by default an empty collection is included.</param>
    /// <returns>The graph of the instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    [RequiresUnreferencedCode(ParseUsesReflection)]
    public static Graph<T> Parse(T instance, bool shouldHaveElement = false)
    {
        ArgumentNullException.ThrowIfNull(instance);

        // The instances being parsed, from the root to the current one: a back reference to one of them is not followed, otherwise a cyclic graph never ends.
        var ancestors = new HashSet<object>(ReferenceEqualityComparer.Instance);

        // Parse follows the properties and types ValidatePaths accepts, so its paths need no second check.
        return FromValidatedPaths(ParseObject(instance, typeof(T), ancestors, shouldHaveElement));
    }

    /// <summary>
    /// Returns a copy of the graph without a property and the paths below it. This graph does not change.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="path">The expression navigating from <typeparamref name="T"/> to the property to remove.</param>
    /// <returns>A copy of the graph without the paths of the property.</returns>
    public Graph<T> RemoveGraph<TProperty>(Expression<Func<T, TProperty>> path) where TProperty : class
    {
        var stringPath = IncludePathOf(path);

        // A subset of paths already validated: no need to check them again with reflection.
        return FromValidatedPaths(_includes.FindAll(i => !IsSameOrUnder(i, stringPath)));
    }

    // The paths below an object, read through its declared type (the properties a subclass adds are not part of the paths the graph can validate).
    [RequiresUnreferencedCode(ParseUsesReflection)]
    private static List<string> ParseObject(object o, Type type, HashSet<object> ancestors, bool shouldHaveElement)
    {
        if (o is IEnumerable)
        {
            throw new InvalidOperationException("Collection is not allowed");
        }

        // The root is typeof(T), which can be an interface.
        if (!type.IsClass && !type.IsInterface)
        {
            throw new InvalidOperationException("Only a class or an interface can be parsed.");
        }

        var result = new List<string>();

        ancestors.Add(o);

        foreach (var property in PathProperties(type))
        {
            var propertyType = property.PropertyType;
            var isCollection = propertyType != typeof(string) && IsCollection(propertyType);
            var itemType = isCollection ? ElementTypeOf(propertyType) : propertyType;

            if (itemType is null || !IsNavigable(itemType))
            {
                continue;
            }

            var item = property.GetValue(o);
            if (item is null)
            {
                continue;
            }

            if (isCollection)
            {
                var enumerator = ((IEnumerable)item).GetEnumerator();
                try
                {
                    if (!enumerator.MoveNext())
                    {
                        // An empty collection is loaded: it is a path, unless the items are required.
                        if (!shouldHaveElement)
                        {
                            result.Add(property.Name);
                        }

                        continue;
                    }

                    item = enumerator.Current;
                }
                finally
                {
                    (enumerator as IDisposable)?.Dispose();
                }

                // A loaded collection whose first item is null: the collection is the path.
                if (item is null)
                {
                    result.Add(property.Name);
                    continue;
                }
            }

            if (ancestors.Contains(item))
            {
                continue;
            }

            var children = ParseObject(item, itemType, ancestors, shouldHaveElement);
            if (children.Count == 0)
            {
                result.Add(property.Name);
            }
            else
            {
                result.AddRange(children.Select(child => property.Name + "." + child));
            }
        }

        ancestors.Remove(o);

        return result;
    }

    /// <summary>
    /// The include paths of the graph. The getter returns a copy; the setter checks the paths like the constructor.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value set is <see langword="null"/>.</exception>
    /// <exception cref="MissingMemberException">A property of a path set does not exist, or is not a class or a generic collection of one class.</exception>
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

            var includes = new List<string>(value);

            Graph<T>.ValidatePaths(includes);

            _includes = includes;
        }
    }

    // Writes the graph as {"Includes":[...]} and reads it back without validating the paths. A converter, because the System.Text.Json
    // source generator runs in the assembly of the caller, which cannot see the private list.
    internal sealed class IncludesJsonConverter : JsonConverter<Graph<T>>
    {
        private const string IncludesName = "Includes";

        public override Graph<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"A graph is a JSON object, not a {reader.TokenType}.");
            }

            var includes = new List<string>();
            var comparison = options.PropertyNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                var name = reader.GetString();
                reader.Read();

                if (!string.Equals(name, IncludesName, comparison))
                {
                    reader.Skip();
                    continue;
                }

                // "Includes": null is an empty graph.
                if (reader.TokenType == JsonTokenType.Null)
                {
                    continue;
                }

                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException($"The includes of a graph are a JSON array, not a {reader.TokenType}.");
                }

                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    includes.Add(reader.TokenType == JsonTokenType.String
                        ? reader.GetString()!
                        : throw new JsonException($"An include path is a JSON string, not a {reader.TokenType}."));
                }
            }

            return FromValidatedPaths(includes);
        }

        public override void Write(Utf8JsonWriter writer, Graph<T> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteStartArray(IncludesName);
            foreach (var include in value._includes)
            {
                writer.WriteStringValue(include);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}

/// <summary>
/// Creates the <see cref="System.Text.Json"/> converter of a <see cref="Graph{T}"/>, which writes the graph as <c>{"Includes":[...]}</c>
/// and reads it back without checking the paths. It works with the reflection-based serializer and with the source generator.
/// </summary>
public sealed class GraphJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Graph<>);

    /// <inheritdoc/>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(Graph<>.IncludesJsonConverter))]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "The constructor of the converter is kept by the DynamicDependency.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Graph<T> constrains T to a reference type, so the converter runs on the code shared by all reference types, which native AOT generates.")]
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        return (JsonConverter)Activator.CreateInstance(typeof(Graph<>.IncludesJsonConverter).MakeGenericType(typeToConvert.GenericTypeArguments))!;
    }
}
