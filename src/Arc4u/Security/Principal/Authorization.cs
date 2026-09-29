using System.Globalization;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Arc4u.Security.Principal;

/// <summary>
/// An operation is the atomic entity defining what a user can do.
/// </summary>
[DataContract(Namespace = "urn:arc4u.profile.operation")]
public class Operation
{
    /// <summary>
    /// Gets or sets the name of the operation.
    /// </summary>
    /// <value>The name.</value>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Name { get; set; } = default!;

    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    /// <value>The ID.</value>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int ID { get; set; }

    /// <summary>
    /// Returns the identifier and the name of the operation.
    /// </summary>
    /// <returns>A string of the form <c>ID, Name.</c></returns>
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}, {1}.", ID, Name);
    }
}

/// <summary>
/// The scopedRoles are the roles defined for a specific scope.
/// </summary>
[DataContract(Namespace = "urn:arc4u.profile.scopedroles")]
public class ScopedRoles
{
    /// <summary>
    /// A scope is a subset of the authority defined only for the specific scope
    /// </summary>
    /// <value>The scope.</value>
    [JsonPropertyName("scope")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Scope { get; set; } = default!;

    /// <summary>
    /// Gets or sets the roles defined in the scope.
    /// </summary>
    /// <value>The roles.</value>
    [JsonPropertyName("roles")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<string> Roles { get; set; } = default!;
}
/// <summary>
///  The ScopedOperations are the operations defined for a specific scope.
/// </summary>
[DataContract(Namespace = "urn:arc4u.profile.scopedoperations")]
public class ScopedOperations
{
    /// <summary>
    /// A scope is a subset of the authority defined only for the specific scope
    /// </summary>
    /// <value>The scope.</value>
    [JsonPropertyName("scope")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Scope { get; set; } = default!;

    /// <summary>
    /// Gets or sets the operations defined for the scope.
    /// </summary>
    /// <value>The operations.</value>
    [JsonPropertyName("operations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<int> Operations { get; set; } = default!;
}

/// <summary>
/// The Authorization class is the class designed to contains the authority information filled by a AuthorizationFiller and is serializable. 
/// </summary>
[DataContract(Namespace = "urn:arc4u.profile.authorization")]
public class Authorization
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Authorization"/> class.
    /// </summary>
    public Authorization()
    {
        Roles = new List<ScopedRoles>();
        Operations = new List<ScopedOperations>();
        Scopes = new List<string>();
        AllOperations = new List<Operation>();
    }

    /// <summary>
    /// Gets or sets the roles.
    /// </summary>
    /// <value>The roles.</value>
    [JsonPropertyName("roles")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<ScopedRoles> Roles { get; set; }

    /// <summary>
    /// Gets or sets the operations.
    /// </summary>
    /// <value>The operations.</value>
    [JsonPropertyName("operations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<ScopedOperations> Operations { get; set; }

    /// <summary>
    /// Gets or sets the scopes.
    /// </summary>
    /// <value>The scopes.</value>
    [JsonPropertyName("scopes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<string> Scopes { get; set; }

    /// <summary>
    /// Return the list of operations so it is possible to show the complete operations list!
    /// </summary>
    [JsonPropertyName("allOperations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<Operation> AllOperations { get; set; }
}
