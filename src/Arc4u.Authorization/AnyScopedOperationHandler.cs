using Arc4u.Dependency.Attribute;
using Arc4u.Security.Principal;
using Microsoft.AspNetCore.Authorization;

namespace Arc4u.Authorization;

/// <summary>
/// Handles <see cref="AnyScopedOperationRequirement"/> by verifying that the current principal
/// is granted at least <b>one</b> of the required scoped operations (OR logic).
/// </summary>
/// <remarks>
/// Registered with scoped lifetime by the <c>AddAnyOperations</c> methods of <see cref="PoliciesBuilder"/> (it is not registered by <see cref="ScopedOperationsExtension.AddScopedOperationsPolicy"/>);
/// the <see cref="ExportAttribute"/>/<see cref="ScopedAttribute"/> pair additionally exposes the
/// handler to the Arc4u dependency-injection scanner. If the principal is <see langword="null"/>
/// (e.g. on Blazor WebAssembly before authentication), the requirement fails immediately.
/// </remarks>
[Export(typeof(IAuthorizationHandler)), Scoped]
public sealed class AnyScopedOperationHandler(IApplicationContext applicationContext)
    : AuthorizationHandler<AnyScopedOperationRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AnyScopedOperationRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (applicationContext.Principal is null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        foreach (var scopedOperation in requirement.Operations)
        {
            if (applicationContext.Principal.IsAuthorized(scopedOperation.scope, scopedOperation.operation))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }
        context.Fail();

        return Task.CompletedTask;
    }
}
