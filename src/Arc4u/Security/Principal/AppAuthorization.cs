using System.Globalization;

namespace Arc4u.Security.Principal;

/// <summary>
/// The default <see cref="IAuthorization"/> implementation, built from an <see cref="Authorization"/> data object.
/// Roles and operations are grouped by scope; the methods without a scope parameter use the empty scope (<see cref="string.Empty"/>).
/// </summary>
public class AppAuthorization : IAuthorization
{
    private readonly Dictionary<string, Dictionary<int, string>> _operations;
    private readonly Dictionary<string, Dictionary<string, int>> _operationsName;
    private readonly Dictionary<string, Dictionary<string, short>> _roles;
    private readonly List<string> _scopes;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppAuthorization"/> class and indexes the roles and operations of the authorization data for fast look-up.
    /// </summary>
    /// <param name="authorizationData">The authorization data (scopes, roles and operations) of the user.</param>
    public AppAuthorization(Authorization authorizationData)
    {
        // Fill the Dictionnary structure for fast retrieving the information.
        // Add Operations
        _operations = new Dictionary<string, Dictionary<int, string>>();
        _operationsName = new Dictionary<string, Dictionary<string, int>>();
        foreach (var scopedOperations in authorizationData.Operations)
        {
            var operations = new Dictionary<int, string>();
            var operationsName = new Dictionary<string, int>();

            foreach (var operationId in scopedOperations.Operations)
            {
                var operation = authorizationData.AllOperations.SingleOrDefault(o => o.ID == operationId);
                if (default(Operation) != operation)
                {
                    operations.Add(operation.ID, operation.Name);
                    operationsName.Add(operation.Name, operation.ID);
                }
            };
            _operations.Add(scopedOperations.Scope, operations);
            _operationsName.Add(scopedOperations.Scope, operationsName);
        }

        // Add Roles.
        _roles = new Dictionary<string, Dictionary<string, short>>();
        foreach (var scopedRoles in authorizationData.Roles)
        {
            var roles = new Dictionary<string, short>();
            foreach (var role in scopedRoles.Roles)
            {
                roles.Add(role, 0);
            }

            _roles.Add(scopedRoles.Scope, roles);
        }

        _scopes = authorizationData.Scopes;
    }

    #region IAuthorization Members

    /// <inheritdoc/>
    public string AuthorizationType
    {
        get { return "Arc4uAuthorization"; }
    }

    /// <inheritdoc/>
    public string[] Scopes()
    {
        return _scopes.ToArray();
    }

    /// <inheritdoc/>
    public string[] Roles()
    {
        return Roles(string.Empty);
    }

    /// <inheritdoc/>
    public string[] Roles(string scope)
    {
        try
        {
            var roles = new string[_roles[scope].Count];
            _roles[scope].Keys.CopyTo(roles, 0);

            return roles;
        }
        catch
        {
            return [];
        }
    }

    /// <inheritdoc/>
    public bool IsAuthorized(params int[] operations)
    {
        return IsAuthorized(string.Empty, operations);
    }

    /// <inheritdoc/>
    public bool IsAuthorized(params string[] operations)
    {
        return IsAuthorized(string.Empty, operations);
    }

    /// <inheritdoc/>
    public bool IsAuthorized(string scope, params int[] operations)
    {
        if (_operations.ContainsKey(scope))
        {
            foreach (var i in operations)
            {
                if (!_operations[scope].ContainsKey(i))
                {
                    return false;
                }
            }
        }
        else // No Scope no Operations.
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public bool IsAuthorized(string scope, params string[] operations)
    {
        if (_operationsName.ContainsKey(scope))
        {
            foreach (var o in operations)
            {
                if (!_operationsName[scope].ContainsKey(o))
                {
                    return false;
                }
            }

        }
        else // No Scope no Operations.
        {
            return false;
        }

        return true;
    }
    /// <summary>
    /// Determines whether all the specified operations, expressed with an enumeration, are authorized in the empty scope.
    /// The numeric value of each enumeration member is the operation identifier.
    /// </summary>
    /// <typeparam name="TAccess">The enumeration type whose members identify the operations.</typeparam>
    /// <param name="operations">The operations.</param>
    /// <returns><see langword="true"/> if all the operations are authorized; otherwise <see langword="false"/>.</returns>
    public bool IsAuthorized<TAccess>(params TAccess[] operations) where TAccess : struct, Enum
    {
        return IsAuthorized(string.Empty, operations);
    }

    /// <summary>
    /// Determines whether all the specified operations, expressed with an enumeration, are authorized in the specified scope.
    /// The numeric value of each enumeration member is the operation identifier.
    /// </summary>
    /// <typeparam name="TAccess">The enumeration type whose members identify the operations.</typeparam>
    /// <param name="scope">The scope.</param>
    /// <param name="operations">The operations.</param>
    /// <returns><see langword="true"/> if all the operations are authorized in the scope; otherwise <see langword="false"/>.</returns>
    public bool IsAuthorized<TAccess>(string scope, params TAccess[] operations)
           where TAccess : struct, Enum
    {
        var ids = new int[operations.Length];

        for (var index = 0; index < operations.Length; ++index)
        {
            ids[index] = Convert.ToInt32(operations[index], CultureInfo.InvariantCulture);
        }

        return IsAuthorized(scope, ids);
    }

    /// <inheritdoc/>
    public bool IsInRole(string role)
    {
        return IsInRole(string.Empty, role);
    }

    /// <inheritdoc/>
    public bool IsInRole(string scope, string role)
    {
        if (_roles.TryGetValue(scope, out var roles))
        {
            return roles.ContainsKey(role);
        }

        return false;
    }

    /// <inheritdoc/>
    public string[] Operations()
    {
        return Operations(string.Empty);
    }

    /// <inheritdoc/>
    public string[] Operations(string scope)
    {
        try
        {
            var operations = new string[_operations[scope].Count];
            _operations[scope].Values.CopyTo(operations, 0);

            return operations;
        }
        catch
        {
            return [];
        }
    }

    #endregion
}
