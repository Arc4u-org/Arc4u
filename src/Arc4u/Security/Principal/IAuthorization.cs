namespace Arc4u.Security.Principal;

/// <summary>
/// The interface defines the authorization pattern used to check if a user is able to do an operation or not.
/// The methods are based on Azman and Role base model See msdn for azman documentation.
/// </summary>
public interface IAuthorization
{
    /// <summary>
    /// Gets the type of the authorization.
    /// </summary>
    /// <value>The type of the authorization.</value>
    string AuthorizationType { get; }

    /// <summary>
    /// Scopeses for roles and operations.
    /// </summary>
    /// <returns></returns>
    string[] Scopes();

    /// <summary>
    /// Roleses defined.
    /// </summary>
    /// <returns></returns>
    string[] Roles();
    /// <summary>
    /// Roleses defined in the specified scope.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <returns></returns>
    string[] Roles(string scope);

    /// <summary>
    /// Operations defined.
    /// </summary>
    /// <returns></returns>
    string[] Operations();
    /// <summary>
    /// Operations defined in the specified scope.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <returns></returns>
    string[] Operations(string scope);

    /// <summary>
    /// Determines whether the specified operations are authorized.
    /// </summary>
    /// <param name="operations">The operations.</param>
    /// <returns>
    /// 	<c>true</c> if the specified operations is authorized; otherwise, <c>false</c>.
    /// </returns>
    bool IsAuthorized(params int[] operations);

    /// <summary>
    /// Determines whether the specified operations are authorized.
    /// </summary>
    /// <param name="operations">The operations.</param>
    /// <returns>
    /// 	<c>true</c> if the specified operations is authorized; otherwise, <c>false</c>.
    /// </returns>
    bool IsAuthorized(params string[] operations);

    /// <summary>
    /// Determines whether the specified operation is authorized in the default (empty) scope.
    /// </summary>
    /// <remarks>
    /// This overload exists so that <c>IsAuthorized("Name")</c> compiles: without it, a single string argument
    /// matches both <see cref="IsAuthorized(string, int[])"/> and <see cref="IsAuthorized(string, string[])"/>
    /// with an empty <c>operations</c> array, which is ambiguous.
    /// </remarks>
    /// <param name="operation">The operation name.</param>
    /// <returns>
    /// 	<c>true</c> if the specified operation is authorized; otherwise, <c>false</c>.
    /// </returns>
    bool IsAuthorized(string operation) => IsAuthorized(string.Empty, new[] { operation });

    /// <summary>
    /// Determines whether the specified operations are authorized in the specified scope.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="operations">The operations.</param>
    /// <returns>
    /// 	<c>true</c> if the specified scope is authorized; otherwise, <c>false</c>.
    /// </returns>
    bool IsAuthorized(string scope, params int[] operations);

    /// <summary>
    /// Determines whether the specified operations are authorized in the specified scope.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="operations">The operations.</param>
    /// <returns>
    /// 	<c>true</c> if the specified scope is authorized; otherwise, <c>false</c>.
    /// </returns>
    bool IsAuthorized(string scope, params string[] operations);

    /// <summary>
    /// Determines whether the user is defined in the specified role.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <returns>
    /// 	<c>true</c> if [is in role] [the specified role]; otherwise, <c>false</c>.
    /// </returns>
    bool IsInRole(string role);
    /// <summary>
    /// /// Determines whether the user is defined in the specified role for the specific scope.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="role">The role.</param>
    /// <returns>
    /// 	<c>true</c> if [is in role] [the specified scope]; otherwise, <c>false</c>.
    /// </returns>
    bool IsInRole(string scope, string role);
}
