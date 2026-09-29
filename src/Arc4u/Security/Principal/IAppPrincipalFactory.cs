
using FluentResults;

namespace Arc4u.Security.Principal;

/// <summary>
/// Creates (and signs out) the <see cref="AppPrincipal"/> of the user, based on a token obtained with settings resolved from the dependency injection container.
/// </summary>
public interface IAppPrincipalFactory
{
    /// <summary>
    /// Creates the principal using the default settings of the implementation.
    /// </summary>
    /// <param name="parameter">An optional parameter passed to the token provider (for example platform specific parameters).</param>
    /// <returns>A result containing the principal, or the reasons why it could not be created.</returns>
    Task<Result<AppPrincipal>> CreatePrincipalAsync(object? parameter = null);
    /// <summary>
    /// Creates the principal using the specified settings.
    /// </summary>
    /// <param name="settings">The settings describing how the token is obtained.</param>
    /// <param name="parameter">An optional parameter passed to the token provider.</param>
    /// <returns>A result containing the principal, or the reasons why it could not be created.</returns>
    Task<Result<AppPrincipal>> CreatePrincipalAsync(IKeyValueSettings settings, object? parameter = null);
    /// <summary>
    /// Creates the principal using the settings registered under the specified name.
    /// </summary>
    /// <param name="settingsResolveName">The name (key) under which the <see cref="IKeyValueSettings"/> are registered.</param>
    /// <param name="parameter">An optional parameter passed to the token provider.</param>
    /// <returns>A result containing the principal, or the reasons why it could not be created (for example when no settings exist with that name).</returns>
    Task<Result<AppPrincipal>> CreatePrincipalAsync(string settingsResolveName, object? parameter = null);

    /// <summary>
    /// Signs out the user using the default settings of the implementation.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the user is signed out.</returns>
    ValueTask SignOutUserAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Signs out the user using the specified settings.
    /// </summary>
    /// <param name="settings">The settings describing the identity provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the user is signed out.</returns>
    ValueTask SignOutUserAsync(IKeyValueSettings settings, CancellationToken cancellationToken);
}
