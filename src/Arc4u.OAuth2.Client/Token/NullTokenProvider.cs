using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Client;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.Token;

/// <summary>An <see cref="ITokenProvider"/> that does not provide any token. It is used when a client must not send a token.</summary>
[Export(NullTokenProvider.ProviderName, typeof(ITokenProvider)), Shared]
public class NullTokenProvider : ITokenProvider
{
    /// <summary>Initializes a new instance of the <see cref="NullTokenProvider"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public NullTokenProvider(ILogger<NullTokenProvider> logger)
    {
        _logger = logger;
    }

    /// <summary>The key (<c>null</c>) under which the provider is registered.</summary>
    public const string ProviderName = "null";

    private readonly ILogger<NullTokenProvider> _logger;

    /// <inheritdoc/>
    public Task<Result<TokenInfo>> GetTokenAsync(IKeyValueSettings? settings, object? platformParameters)
    {
        _logger.Technical().LogCallNullTokenProvider();
        return Task.FromResult<Result<TokenInfo>>(new());
    }

    /// <inheritdoc/>
    public ValueTask SignOutAsync(IKeyValueSettings settings, CancellationToken cancellationToken)
    {
        _logger.Technical().LogCallSignOutNullTokenProvider();

        return ValueTask.CompletedTask;
    }
}

