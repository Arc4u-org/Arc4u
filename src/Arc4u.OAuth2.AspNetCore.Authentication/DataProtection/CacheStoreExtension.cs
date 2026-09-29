using System.Diagnostics.CodeAnalysis;
using Arc4u.Caching;
using Arc4u.Configuration;
using Arc4u.Serializer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.DataProtection
{
    /// <summary>Extension methods to persist the data protection keys in an Arc4u cache.</summary>
    public static class CacheStoreExtension
    {
        /// <summary>Persists the data protection keys in a cache, from code.</summary>
        /// <param name="builder">The data protection builder.</param>
        /// <param name="option">The action that configures the <see cref="CacheStoreOption"/>.</param>
        /// <returns>The data protection builder, to chain calls.</returns>
        /// <exception cref="ArgumentNullException"><see cref="CacheStoreOption.CacheKey"/> is <see langword="null"/>.</exception>
        /// <example>
        /// <code language="csharp">
        /// builder.Services.AddDataProtection()
        ///     .PersistKeysToCache(o => { o.CacheKey = "DataProtection"; o.CacheName = "Default"; });
        /// </code>
        /// </example>
        public static IDataProtectionBuilder PersistKeysToCache(this IDataProtectionBuilder builder, Action<CacheStoreOption> option)
        {
            var validate = new CacheStoreOption();
            option(validate);

            ArgumentNullException.ThrowIfNull(validate.CacheKey);

            builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
            {
                var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
                var cacheContext = services.GetRequiredService<ICacheContext>();
                var serializer = services.GetRequiredService<IObjectSerialization>();

                return new ConfigureOptions<KeyManagementOptions>(options =>
                {
                    options.XmlRepository = new CacheStore(cacheContext, loggerFactory, serializer, validate.CacheKey, validate.CacheName);
                });
            });

            return builder;
        }

        /// <summary>Persists the data protection keys in a cache described by a configuration section.</summary>
        /// <param name="builder">The data protection builder.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="configSectionName">The section holding the <see cref="CacheStoreOption"/>. The default is <c>DataProtectionStore</c>.</param>
        /// <returns>The data protection builder, to chain calls.</returns>
        /// <exception cref="ConfigurationException">The section does not exist or has no <c>CacheKey</c> or <c>CacheName</c>.</exception>
        public static IDataProtectionBuilder PersistKeysToCache(this IDataProtectionBuilder builder, [DisallowNull] IConfiguration configuration, [DisallowNull] string configSectionName = "DataProtectionStore")
        {
            ArgumentNullException.ThrowIfNull(configuration);

            return PersistKeysToCache(builder, PrepareAction(configuration, configSectionName));
        }

        internal static Action<CacheStoreOption> PrepareAction(IConfiguration configuration, string configSectionName)
        {
            var section = configuration.GetSection(configSectionName);
            if (!section.Exists())
            {
                throw new ConfigurationException($"A section with name {configSectionName} doesn't exist.");
            }

            var storeInfo = section.Get<CacheStoreOption>() ?? throw new ConfigurationException($"Retrieving the cache data protection store info from section {configSectionName} is impossible.");

            if (storeInfo.CacheKey is null || storeInfo.CacheName is null)
            {
                throw new ConfigurationException($"Retrieving the CacheKey or CacheName data protection store info from section {configSectionName} is impossible.");
            }

            void OptionsFiller(CacheStoreOption option)
            {
                ArgumentNullException.ThrowIfNull(option);

                option.CacheKey = storeInfo.CacheKey ?? throw new ConfigurationException($"CacheKey from section {configSectionName} is null.");
                option.CacheName = storeInfo.CacheName;
            }

            return OptionsFiller;
        }
    }
}
