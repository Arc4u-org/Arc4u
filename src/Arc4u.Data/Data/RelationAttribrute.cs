namespace Arc4u.Data;

/// <summary>
/// Describes, on an entity class, a relation between a navigation property and a property of the related (target) type.
/// </summary>
/// <remarks>The attribute only carries the two property names; it can be applied several times to a class.</remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RelationAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the navigation property.
    /// </summary>
    public string NavigationProperty { get; set; } = String.Empty;

    /// <summary>
    /// Gets or sets the name of the related property on the target type of the navigation property.
    /// </summary>
    public string TargetRelationProperty { get; set; } = String.Empty;
}
