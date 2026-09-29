using System.Security.Claims;
using Arc4u.Dependency.Attribute;
using Arc4u.Security.Principal;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Arc4u.Blazor;

/// <summary>
/// Populates the circuit-scoped <see cref="IApplicationContext"/> for Interactive Server components.
///
/// An Interactive Server component runs in a SignalR circuit that owns its own DI scope, distinct from
/// the HTTP request scope where <c>AppPrincipalTransform</c> (and, during prerendering,
/// <see cref="AppPrincipalServerAuthenticationStateProvider"/>) populate the request-scoped
/// <see cref="IApplicationContext"/>. That request scope is disposed once the response is sent, so the
/// circuit starts with an empty <see cref="IApplicationContext"/> and component event handlers (e.g. a
/// button click on the Counter page) see a null <c>Principal</c>.
///
/// This handler resolves the circuit-scoped <see cref="AuthenticationStateProvider"/> once the SignalR
/// connection is up and builds the <see cref="AppPrincipal"/> into the circuit-scoped
/// <see cref="IApplicationContext"/>, so it is available for the whole lifetime of the circuit —
/// independent of whether/when the render pipeline re-invokes <c>GetAuthenticationStateAsync</c>.
/// </summary>
[Export(typeof(CircuitHandler)), Scoped]
public sealed class ApplicationContextCircuitHandler : CircuitHandler
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IClaimAuthorizationFiller _authorizationFiller;
    private readonly IClaimProfileFiller _profileFiller;
    private readonly IApplicationContext _applicationContext;

    /// <summary>Initializes a new instance of the <see cref="ApplicationContextCircuitHandler"/> class.</summary>
    /// <param name="authenticationStateProvider">The circuit-scoped authentication state provider.</param>
    /// <param name="authorizationFiller">The filler that builds the authorization from the claims.</param>
    /// <param name="profileFiller">The filler that builds the user profile from the claims.</param>
    /// <param name="applicationContext">The circuit-scoped application context that receives the principal.</param>
    public ApplicationContextCircuitHandler(
        AuthenticationStateProvider authenticationStateProvider,
        IClaimAuthorizationFiller authorizationFiller,
        IClaimProfileFiller profileFiller,
        IApplicationContext applicationContext)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _authorizationFiller = authorizationFiller;
        _profileFiller = profileFiller;
        _applicationContext = applicationContext;
    }

    /// <summary>Builds the <see cref="AppPrincipal"/> of the authenticated user and sets it in the circuit-scoped <see cref="IApplicationContext"/> when the circuit connection is up. An anonymous user leaves the context untouched.</summary>
    /// <param name="circuit">The circuit.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the principal is set.</returns>
    public override async Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // At this point the circuit's authentication state has been seeded, so this runs in the circuit
        // DI scope with the authenticated user and populates the IApplicationContext instance that the
        // interactive components (and their event handlers) will resolve.
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);

        ApplicationContextInitializer.SetPrincipal(
            state.User.Identity as ClaimsIdentity,
            _authorizationFiller,
            _profileFiller,
            _applicationContext);
    }
}
