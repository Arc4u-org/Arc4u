namespace Arc4u.gRPC;

/// <summary>
/// Declares the scope and the operations that a gRPC service method is protected with.
/// </summary>
/// <remarks>Only applicable to methods. The attribute carries data only: it is up to the caller reading it (for example a server interceptor) to enforce it.</remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ServiceAspectAttribute : Attribute
{
    /// <summary>
    /// Creates the attribute with an empty scope.
    /// </summary>
    /// <param name="operations">The identifiers of the operations required to call the method.</param>
    public ServiceAspectAttribute(params int[] operations) : this(string.Empty, operations)
    {
    }

    /// <summary>
    /// Creates the attribute with a scope and operations.
    /// </summary>
    /// <param name="scope">The scope; trimmed, and an empty string when null or white space.</param>
    /// <param name="operations">The identifiers of the operations required to call the method.</param>
    public ServiceAspectAttribute(string scope, params int[] operations)
    {
        Scope = string.IsNullOrWhiteSpace(scope) ? string.Empty : scope.Trim();
        Operations = operations;
    }

    /// <summary>
    /// Gets or sets the scope; an empty string when none was given.
    /// </summary>
    public string Scope { get; set; }

    /// <summary>
    /// Gets or sets the identifiers of the operations required to call the method.
    /// </summary>
    public int[] Operations { get; set; }

    /// <summary>
    /// Creates an attribute with an empty scope and no operation.
    /// </summary>
    /// <returns>The empty attribute.</returns>
    public static ServiceAspectAttribute Empty() => new([]);
}
