using System.Diagnostics.CodeAnalysis;
using Arc4u.Security.Principal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.Authorization;
/// <summary>Registers the authorization policies derived from the operations of an application.</summary>
public static class ScopedOperationsExtension
{
    /// <summary>
    /// Registers one authorization policy per operation, named as the operation, and one per scope and operation, named <c>{scope}:{operation name}</c>.
    /// A policy is satisfied when the current principal is granted the operation, in the default scope or in the given scope. The <see cref="AllScopedOperationsHandler"/> is registered to evaluate them.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="scopes">The scopes for which the scoped policies are created.</param>
    /// <param name="operations">The operations for which the policies are created.</param>
    /// <returns>A <see cref="PoliciesBuilder"/> to register additional policies.</returns>
    /// <example>
    /// <code language="csharp">
    /// var operations = new[]
    /// {
    ///     new Operation { Name = "ReadOrders", ID = 1 },
    ///     new Operation { Name = "WriteOrders", ID = 2 }
    /// };
    ///
    /// builder.Services.AddScopedOperationsPolicy(["Sales"], operations)
    ///                 .AddAnyOperations("OrdersAccess", 1, 2);
    /// </code>
    /// </example>
    public static PoliciesBuilder AddScopedOperationsPolicy(this IServiceCollection services, IEnumerable<string> scopes, IEnumerable<Operation> operations)
    {
        services.AddScoped<IAuthorizationHandler, AllScopedOperationsHandler>();

        services.AddAuthorizationCore(options =>
        {
            // Add the default policy.
            foreach (var operation in operations)
            {
                options.AddPolicy(operation.Name, policy =>
                policy.Requirements.Add(new AllScopedOperationsRequirement(operation.ID)));
            }

            // Add the scoped policy.
            foreach (var scope in scopes)
            {
                foreach (var operation in operations)
                {
                    options.AddPolicy($"{scope}:{operation.Name}", policy =>
                    policy.Requirements.Add(new AllScopedOperationsRequirement(scope, operation.ID)));
                }
            }
        });

        return new PoliciesBuilder(services);
    }
}
