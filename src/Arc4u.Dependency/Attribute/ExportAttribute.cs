namespace Arc4u.Dependency.Attribute;

/// <summary>
///     Represents an attribute that indicates a type provides a specific export.
///     This attribute can be used in combination with other attributes to define the export's lifetime:
///     <list type="bullet">
///         <item>
///             <term><see cref="SharedAttribute"/></term>
///             <description>Specifies that the export will have a singleton lifetime.</description>
///         </item>
///         <item>
///             <term><see cref="ScopedAttribute"/></term>
///             <description>Specifies that the export will have a scoped lifetime.</description>
///         </item>
///     </list>
///     If neither <see cref="SharedAttribute"/> nor <see cref="ScopedAttribute"/> is used, the export will have a transient lifetime.
/// </summary>
/// <example>
/// <code language="csharp">
/// public interface IClock { DateTime UtcNow { get; } }
///
/// // Registered as a singleton for the contract IClock.
/// [Export(typeof(IClock)), Shared]
/// public class SystemClock : IClock
/// {
///     public DateTime UtcNow =&gt; DateTime.UtcNow;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class ExportAttribute : System.Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ExportAttribute"/> class, exporting the
    ///     type or member marked with this attribute under the default contract name.
    /// </summary>
    public ExportAttribute() : this(null, null)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExportAttribute"/> class, exporting the
    ///     type or member marked with this attribute under a contract name derived from the
    ///     specified type.
    /// </summary>
    /// <param name="contractType">
    ///     A <see cref="Type"/> of which to derive the contract name to export the type or
    ///     member marked with this attribute, under; or <see langword="null"/> to use the
    ///     default contract name.
    /// </param>
    public ExportAttribute(Type contractType) : this(null, contractType)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExportAttribute"/> class, exporting the
    ///     type or member marked with this attribute under the specified contract name.
    /// </summary>
    /// <param name="contractName">
    ///      A <see cref="string"/> containing the contract name to export the type or member
    ///      marked with this attribute, under; or <see langword="null"/> or an empty string
    ///      ("") to use the default contract name.
    /// </param>
    public ExportAttribute(string contractName) : this(contractName, null)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExportAttribute"/> class, exporting the
    ///     type or member marked with this attribute under the specified contract name.
    /// </summary>
    /// <param name="contractName">
    ///      A <see cref="string"/> containing the contract name to export the type or member
    ///      marked with this attribute, under; or <see langword="null"/> or an empty string
    ///      ("") to use the default contract name.
    /// </param>
    /// <param name="contractType">
    ///     A <see cref="Type"/> of which to derive the contract name to export the type or
    ///     member marked with this attribute, under; or <see langword="null"/> to use the
    ///     default contract name.
    /// </param>
    public ExportAttribute(string? contractName, Type? contractType)
    {
        ContractName = contractName;
        ContractType = contractType;
    }

    /// <summary>
    ///     Gets the contract name to export the type or member under.
    /// </summary>
    /// <value>
    ///      A <see cref="string"/> containing the contract name to export the type or member
    ///      marked with this attribute, under. The default value is an empty string ("").
    /// </value>
    public string? ContractName { get; }

    /// <summary>
    ///     Get the contract type that is exported by the member that this attribute is attached to.
    /// </summary>
    /// <value>
    ///     A <see cref="Type"/> of the export that is be provided. The default value is
    ///     <see langword="null"/> which means that the type will be obtained by looking at the type on
    ///     the member that this export is attached to.
    /// </value>
    public Type? ContractType { get; }
}
