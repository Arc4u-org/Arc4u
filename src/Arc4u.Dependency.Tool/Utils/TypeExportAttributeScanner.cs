using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Arc4u.Dependency.Tool;

static class TypeExportAttributeScanner
{
    private static readonly TypeInfo ExportAttribute = new("Arc4u.Dependency.Attribute", "ExportAttribute");
    private static readonly TypeInfo ScopedAttribute = new("Arc4u.Dependency.Attribute", "ScopedAttribute");
    private static readonly TypeInfo SharedAttribute = new("Arc4u.Dependency.Attribute", "SharedAttribute");
    
    /// <summary>
    /// Return the export description of all injectable types.
    /// </summary>
    /// <param name="assemblyPath">The physical path to the assembly. It will be analyzed without requiring dependencies</param>
    /// <param name="types">the list of types to investigate, or null to find them all</param>
    /// <returns>A enumerable description of the export parameters for each injectable type</returns>
    public static IEnumerable<ExportDescription> GetExportDescriptions(string assemblyPath, IReadOnlyCollection<TypeInfo>? types = null)
    {
        using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new PEReader(stream);

        var metadata = reader.GetMetadataReader();
        var assembly = metadata.GetAssemblyDefinition();

        foreach (var type in metadata.TypeDefinitions.Select(metadata.GetTypeDefinition))
        {
            // we are only interested in types defined by the implementer, not by the system. The latter types have no namespace
            var @namespace = metadata.GetString(type.Namespace);
            if (string.IsNullOrEmpty(@namespace))
            {
                continue;
            }

            var typeInfo = new TypeInfo(@namespace, metadata.GetString(type.Name));

            if (types is null || types.Contains(typeInfo))
            {
                // Every Export attribute is a registration, as in 8.x: the attribute allows several.
                var exports = new List<(TypeInfo Service, string? ContractName)>();
                var isScoped = false;
                var isShared = false;

                foreach (var handle in type.GetCustomAttributes())
                {
                    var attribute = metadata.GetCustomAttribute(handle);
                    var attributeTypeInfo = GetCustomAttributeFullName(metadata, attribute);

                    if (ExportAttribute.Equals(attributeTypeInfo))
                    {
                        var serviceTypeInfo = typeInfo;
                        string? contractName = null;
                        var arguments = attribute.DecodeValue(DummyCustomAttributeProvider.Instance);
                        foreach (var argument in arguments.FixedArguments)
                        {
                            if (argument.Value is TypeInfo typeInfoArgument)
                            {
                                serviceTypeInfo = typeInfoArgument;
                            }
                            else if (argument.Value is string contractNameArgument)
                            {
                                contractName = contractNameArgument;
                            }
                        }
                        exports.Add((serviceTypeInfo, contractName));
                    }
                    else if (ScopedAttribute.Equals(attributeTypeInfo))
                    {
                        isScoped = true;
                    }
                    else if (SharedAttribute.Equals(attributeTypeInfo))
                    {
                        isShared = true;
                    }
                }

                foreach (var (service, contractName) in exports)
                {
                    yield return new ExportDescription(service, typeInfo, isScoped, isShared, contractName);
                }
            }
        }
    }

    private static TypeInfo? GetCustomAttributeFullName(MetadataReader metadata, CustomAttribute attribute)
    {
        if (attribute.Constructor.Kind == HandleKind.MemberReference)
        {
            var ctor = metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
            try
            {
                if (ctor.Parent.Kind == HandleKind.TypeReference)
                {
                    var type = metadata.GetTypeReference((TypeReferenceHandle)ctor.Parent);
                    return new TypeInfo(metadata.GetString(type.Namespace), metadata.GetString(type.Name));
                }
                else if (ctor.Parent.Kind == HandleKind.TypeSpecification)
                {
                    var type = metadata.GetTypeSpecification((TypeSpecificationHandle)ctor.Parent);
                    var blobReader = metadata.GetBlobReader(type.Signature);
                    var typeCode = blobReader.ReadSignatureTypeCode();
                    var typeHandle = blobReader.ReadTypeHandle();
                    var typeRef = metadata.GetTypeReference((TypeReferenceHandle)typeHandle);

                    return new TypeInfo(metadata.GetString(typeRef.Namespace), metadata.GetString(typeRef.Name));
                }
                else
                {
                    //Console.WriteLine($"Unsupported EntityHandle.Kind: {ctor.Parent.Kind}");
                    return null;
                }
            }
            catch (InvalidCastException)
            {
                //Console.WriteLine($"Unsupported EntityHandle.Kind: {ctor.Parent.Kind}");
                return null;
            }
        }
        else if (attribute.Constructor.Kind == HandleKind.MethodDefinition)
        {
            var ctor = metadata.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);
            var type = metadata.GetTypeDefinition(ctor.GetDeclaringType());
            return new TypeInfo(metadata.GetString(type.Namespace), metadata.GetString(type.Name));
        }
        return null;
    }
}

