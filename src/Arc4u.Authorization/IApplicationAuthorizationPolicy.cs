using System.Diagnostics.CodeAnalysis;

namespace Arc4u.Authorization;
/// <summary>Checks, from code, that the current user satisfies an authorization policy.</summary>
public interface IApplicationAuthorizationPolicy
{
    /// <summary>
    /// Check if the user is authorized to access the resource based on the policy name.
    /// </summary>
    /// <param name="policyName">The name of the policy</param>
    /// <param name="exceptionMessage"></param>
    /// <exception cref="UnauthorizedAccessException">If the user is not authorized.</exception>
    /// <returns>Nothing</returns>
    public Task AuthorizeAsync(string policyName, [AllowNull] string? exceptionMessage = null);

    /// <summary>
    /// Check if the user is authorized to access the resource based on the policy name.
    /// </summary>
    /// <param name="policyName">The name of the policy</param>
    /// <returns>true if authorized, otherwise false.</returns>
    public Task<bool> IsAuthorizeAsync(string policyName);
}
