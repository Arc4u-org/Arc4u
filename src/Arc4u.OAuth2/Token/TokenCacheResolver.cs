using System.Collections.Concurrent;
using Arc4u.Caching;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Token;

/// <summary>The cache selected by a <see cref="TokenCacheOptions"/> and the maximum time an entry is kept in it.</summary>
/// <param name="Cache">The cache.</param>
/// <param name="MaxTime">The <see cref="TokenCacheOptions.MaxTime"/>.</param>
/// <param name="CacheName">
/// The name of the cache, to find it again with <see cref="ICacheHelper.GetCache(string?)"/> even when the options change, or <see langword="null"/> for the token cache
/// (the cache of the default <see cref="TokenCacheOptions"/>).
/// </param>
public readonly record struct ResolvedTokenCache(ICache Cache, TimeSpan MaxTime, string? CacheName = null);

/// <summary>
/// Resolves the cache and the <see cref="TokenCacheOptions.MaxTime"/> of the default or named <see cref="TokenCacheOptions"/>.
/// It is used by <see cref="ApplicationCache"/> and by the token providers that use the <see cref="ICache"/> directly.
/// It is registered as a singleton by <c>AddTokenCache</c>.
/// </summary>
/// <remarks>
/// A name is used only when options are registered under it: by <c>AddNamedTokenCache</c>, <c>AddSharedTokenCache</c>, <c>AddClientTokens</c> or <c>AddOnBehalfOf</c>,
/// with <c>services.Configure&lt;TokenCacheOptions&gt;(name, …)</c> (an action or a configuration section), or by any other registration (for example
/// <c>AddOptions&lt;TokenCacheOptions&gt;(name).BindConfiguration(path)</c>) that gives a <see cref="TokenCacheOptions.CacheName"/> or a <see cref="TokenCacheOptions.MaxTime"/>
/// other than those of a name that is not registered (the class defaults, changed by <c>ConfigureAll</c> or <c>PostConfigureAll</c> when there are some).
/// Any other name, for example a typo, would give a new <see cref="TokenCacheOptions"/> with these values: a warning is logged once and the default options are used instead.
/// </remarks>
[Export(typeof(TokenCacheResolver)), Shared]
public sealed class TokenCacheResolver
{
    private readonly ICacheHelper _cacheHelper;
    private readonly IOptionsMonitor<TokenCacheOptions> _options;
    private readonly IConfigureOptions<TokenCacheOptions>[] _configureOptions;
    private readonly ILogger<TokenCacheResolver> _logger;

    // Whether options are registered under a name. The registrations do not change once the container is built.
    private readonly ConcurrentDictionary<string, bool> _registeredNames = new(StringComparer.Ordinal);

    /// <summary>Initializes a new instance of the <see cref="TokenCacheResolver"/> class.</summary>
    /// <param name="cacheHelper">Gives access to the caches.</param>
    /// <param name="options">The default and named token cache options.</param>
    /// <param name="configureOptions">The registered configurations of the <see cref="TokenCacheOptions"/>, used to know which names are registered.</param>
    /// <param name="logger">Logs a warning, once, for each name under which no options are registered.</param>
    public TokenCacheResolver(ICacheHelper cacheHelper,
                              IOptionsMonitor<TokenCacheOptions> options,
                              IEnumerable<IConfigureOptions<TokenCacheOptions>> configureOptions,
                              ILogger<TokenCacheResolver> logger)
    {
        ArgumentNullException.ThrowIfNull(cacheHelper);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configureOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _cacheHelper = cacheHelper;
        _options = options;
        _configureOptions = [.. configureOptions];
        _logger = logger;
        _unregisteredOptions = new Lazy<TokenCacheOptions>(() => options.Get(UnregisteredName));
    }

    // The options of a name that is not registered, read once.
    private readonly Lazy<TokenCacheOptions> _unregisteredOptions;

    /// <summary>Resolves the cache and the maximum time of the named options.</summary>
    /// <param name="optionsName">The name of the <see cref="TokenCacheOptions"/>, or <see langword="null"/> for the default ones.</param>
    /// <returns>The cache and its maximum time. The default options are used when no options are registered under <paramref name="optionsName"/>.</returns>
    public ResolvedTokenCache Resolve(string? optionsName)
    {
        var defaultOptions = _options.CurrentValue;
        if (string.IsNullOrWhiteSpace(optionsName) || !IsRegistered(optionsName))
        {
            return new ResolvedTokenCache(_cacheHelper.GetCache(), defaultOptions.MaxTime);
        }

        var namedOptions = _options.Get(optionsName);

        // The cache of the default options is the token cache: no name, as for the default options.
        var cacheName = string.IsNullOrWhiteSpace(namedOptions.CacheName) || string.Equals(namedOptions.CacheName, defaultOptions.CacheName, StringComparison.Ordinal)
            ? null
            : namedOptions.CacheName;

        // The token cache is read with GetCache(), as for the default options, so an ICacheHelper (or a mock) that only gives the token cache keeps working.
        var cache = cacheName is null ? _cacheHelper.GetCache() : _cacheHelper.GetCache(cacheName);

        return new ResolvedTokenCache(cache, namedOptions.MaxTime, cacheName);
    }

    private bool IsRegistered(string name)
    {
        if (_registeredNames.TryGetValue(name, out var known))
        {
            return known;
        }

        var registered = CheckRegistration(name);

        // Two first requests for a name may both check it: only the one that records the result logs the warning.
        if (_registeredNames.TryAdd(name, registered) && !registered)
        {
            _logger.Technical().LogUnregisteredTokenCacheOptions(name);
        }

        return _registeredNames[name];
    }

    private bool CheckRegistration(string name)
    {
        // The named configurations of Microsoft.Extensions.Options: Configure(name, action), Configure(name, section)
        // (a ConfigureNamedOptions<T>) and AddOptions(name).Configure<IServiceProvider>(…) used by AddNamedTokenCache.
        // ConfigureAll (a null name) is not a registration of the name.
        // The other registrations (BindConfiguration, Configure<TDep>(…) with another dependency) cannot be read without reflection:
        // their options are recognized by values that differ from those of a name that is not registered: the class defaults, changed by
        // ConfigureAll and PostConfigureAll, which apply to every name.
        return _configureOptions.Any(c => c switch
        {
            ConfigureNamedOptions<TokenCacheOptions> o => string.Equals(o.Name, name, StringComparison.Ordinal),
            ConfigureNamedOptions<TokenCacheOptions, IServiceProvider> o => string.Equals(o.Name, name, StringComparison.Ordinal),
            _ => false
        }) || DiffersFrom(_options.Get(name), _unregisteredOptions.Value);
    }

    // A name no registration can use: its options are those of every name that is not registered.
    private static readonly string UnregisteredName = $"Arc4u.TokenCacheResolver.Unregistered.{Guid.NewGuid():N}";

    private static bool DiffersFrom(TokenCacheOptions options, TokenCacheOptions unregistered)
        => !string.Equals(Normalize(options.CacheName), Normalize(unregistered.CacheName), StringComparison.Ordinal) || options.MaxTime != unregistered.MaxTime;

    private static string? Normalize(string? cacheName) => string.IsNullOrWhiteSpace(cacheName) ? null : cacheName;
}
