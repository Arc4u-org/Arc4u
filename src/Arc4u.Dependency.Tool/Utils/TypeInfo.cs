namespace Arc4u.Dependency.Tool;

internal sealed class TypeInfo
{
    private const char NamespaceSeparator = '.';

    public string Name { get; }
    public string Namespace { get; }
    public IReadOnlyList<TypeInfo> GenericArguments { get; }

    public string FullName
    {
        get
        {
            var baseName = Namespace + NamespaceSeparator + Name;
            if (GenericArguments.Count == 0)
            {
                return baseName;
            }
            return baseName + "<" + string.Join(", ", GenericArguments.Select(a => a.FullName)) + ">";
        }
    }

    public TypeInfo(string @namespace, string name, IReadOnlyList<TypeInfo>? genericArguments = null)
    {
        if (string.IsNullOrEmpty(@namespace))
        {
            throw new ArgumentNullException(nameof(@namespace));
        }
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }
        Name = name;
        Namespace = @namespace;
        GenericArguments = genericArguments ?? Array.Empty<TypeInfo>();
    }

    public TypeInfo(string fullName)
    {
        if (string.IsNullOrEmpty(fullName))
        {
            throw new ArgumentNullException(nameof(fullName));
        }
        var lastSeparatorIndex = fullName.LastIndexOf(NamespaceSeparator);
        if (lastSeparatorIndex == -1)
        {
            throw new ArgumentException("The full name must contain a namespace and a name.", nameof(fullName));
        }

        Namespace = fullName.Substring(0, lastSeparatorIndex);
        Name = fullName.Substring(lastSeparatorIndex + 1);
        GenericArguments = Array.Empty<TypeInfo>();
    }

    public override int GetHashCode()
    {
        return FullName.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is TypeInfo other && FullName == other.FullName;
    }
}
