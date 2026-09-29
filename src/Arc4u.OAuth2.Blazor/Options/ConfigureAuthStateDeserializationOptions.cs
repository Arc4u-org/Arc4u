#if NET9_0_OR_GREATER
using Arc4u.Dependency.Attribute;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;

namespace Arc4u.Blazor.Options;

/// <summary>Configures the deserialization of the authentication state received from the server so that it produces an <see cref="Arc4u.Security.Principal.AppPrincipal"/>.</summary>
[Export(typeof(IConfigureOptions<AuthenticationStateDeserializationOptions>)), Shared]
public sealed class ConfigureAuthStateDeserializationOptions
    : IConfigureOptions<AuthenticationStateDeserializationOptions>
{
    private readonly IAppPrincipalAuthenticationStateProvider _mapper;

    /// <summary>Initializes a new instance of the <see cref="ConfigureAuthStateDeserializationOptions"/> class.</summary>
    /// <param name="mapper">The provider that builds the principal from the deserialized authentication state.</param>
    public ConfigureAuthStateDeserializationOptions(IAppPrincipalAuthenticationStateProvider mapper)
    {
        _mapper = mapper;
    }

    /// <summary>Sets the <see cref="AuthenticationStateDeserializationOptions.DeserializationCallback"/> to build the <see cref="Arc4u.Security.Principal.AppPrincipal"/>. An anonymous state is returned when there is no data.</summary>
    /// <param name="options">The options to configure.</param>
    public void Configure(AuthenticationStateDeserializationOptions options)
    {
        options.DeserializationCallback = async data =>
        {
            if (data is null)
            {
                return AppPrincipalFromAuthenticationState.DefaultAuthenticationState;
            }

            return await _mapper.DeserializeAuthenticationStateAsync(data).ConfigureAwait(false);
        };
    }
}
#endif
