using System.Diagnostics.CodeAnalysis;

namespace Arc4u.Authorization;
/// <summary>Checks, from code, that the current user satisfies an authorization policy.</summary>
public interface IApplicationAuthorizationPolicy
{
    /// <summary>
    /// Check if the user is authorized to access the resource based on the policy name.
    /// </summary>
    /// <param name="policyName">The name of the policy</param>
    /// <param name="exceptionMessage">The message of the exception thrown when the user is not authorized. When empty, a default message giving the policy name is used.</param>
    /// <exception cref="UnauthorizedAccessException">If the user is not authorized.</exception>
    /// <returns>A task that completes when the user is authorized.</returns>
    public Task AuthorizeAsync(string policyName, [AllowNull] string? exceptionMessage = null);

    /// <summary>
    /// Check if the user is authorized to access the resource based on the policy name.
    /// </summary>
    /// <param name="policyName">The name of the policy</param>
    /// <returns><see langword="true"/> if there is a current principal that satisfies the policy, otherwise <see langword="false"/>.</returns>
    public Task<bool> IsAuthorizeAsync(string policyName);
}
