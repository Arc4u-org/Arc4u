using System.Globalization;
using System.Runtime.Serialization;

namespace Arc4u.IdentityModel.Claims;

/// <summary>
/// A serializable representation of a claim: its type and its value.
/// </summary>
[DataContract]
public class ClaimDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClaimDto"/> class with an empty type and value.
    /// </summary>
    public ClaimDto()
    {
        ClaimType = string.Empty;
        Value = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaimDto"/> class.
    /// </summary>
    /// <param name="type">The claim type.</param>
    /// <param name="value">The claim value.</param>
    public ClaimDto(string type, string value)
    {
        ClaimType = type;

        Value = value;
    }

    /// <summary>
    /// Gets or sets the claim type. It is serialized as <c>claimType</c>.
    /// </summary>
    [DataMember(Name = "claimType")]
    public string ClaimType { get; set; }

    /// <summary>
    /// Gets or sets the claim value. It is serialized as <c>value</c>.
    /// </summary>
    [DataMember(Name = "value")]
    public string Value { get; set; }

    /// <summary>
    /// Returns the claim as <c>type : value</c>.
    /// </summary>
    /// <returns>The string representation of the claim.</returns>
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0} : {1}", ClaimType, Value);
    }
}
