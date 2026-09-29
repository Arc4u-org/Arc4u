using System.Diagnostics.CodeAnalysis;
using Arc4u.Dependency.Attribute;
using Arc4u.Security.Principal;
using Microsoft.AspNetCore.Authorization;

namespace Arc4u.Authorization;

/// <summary>Default <see cref="IApplicationAuthorizationPolicy"/>: evaluates the policy with the <see cref="IAuthorizationService"/> for the principal of the current <see cref="IApplicationContext"/>.</summary>
[Export(typeof(IApplicationAuthorizationPolicy)), Scoped]
public class ApplicationAuthorizationPolicy : IApplicationAuthorizationPolicy
{
    /// <summary>Initializes a new instance of the <see cref="ApplicationAuthorizationPolicy"/> class.</summary>
    /// <param name="authorizationService">The service that evaluates the policies.</param>
    /// <param name="applicationContext">The application context giving the current principal.</param>
    public ApplicationAuthorizationPolicy(IAuthorizationService authorizationService, IApplicationContext applicationContext)
    {
        _applicationContext = applicationContext;
        _authorizationService = authorizationService;
    }

    private readonly IAuthorizationService _authorizationService;
    private readonly IApplicationContext _applicationContext;

    /// <inheritdoc/>
    public async Task AuthorizeAsync(string policyName, [AllowNull] string? exceptionMessage = null)
    {
        if (!await IsAuthorizeAsync(policyName).ConfigureAwait(false))
        {
            throw new UnauthorizedAccessException(string.IsNullOrEmpty(exceptionMessage)
                ? $"User is not authorized to access the resource. Policy: {policyName}" : exceptionMessage);
        }
    }

    /// <summary>Checks whether the current principal satisfies the policy.</summary>
    /// <param name="policyName">The name of the policy.</param>
    /// <returns><see langword="true"/> when there is a principal that satisfies the policy; <see langword="false"/> when there is no principal or the policy is not satisfied.</returns>
    public async Task<bool> IsAuthorizeAsync(string policyName)
    {
        if (null == _applicationContext.Principal)
        {
            return false;
        }

        var authResult = await _authorizationService.AuthorizeAsync(_applicationContext.Principal, policyName).ConfigureAwait(false);

        return authResult.Succeeded;
    }
}
