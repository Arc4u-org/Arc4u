using Arc4u.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arc4u.OAuth2.TicketStore
{
    /// <summary>Registers the <see cref="CacheTicketStore"/>.</summary>
    public static class CacheTicketStoreExtension
    {
        /// <summary>Registers the <see cref="CacheTicketStoreOptions"/> and the <see cref="CacheTicketStore"/> as <see cref="ITicketStore"/>, unless another ticket store is already registered.</summary>
        /// <param name="services">The service collection.</param>
        /// <param name="action">The action that configures the options.</param>
        /// <example>
        /// <code language="csharp">
        /// builder.Services.AddCacheTicketStore(o =>
        /// {
        ///     o.CacheName = "Default";
        ///     o.KeyPrefix = "AuthSessionStore-";
        /// });
        /// </code>
        /// </example>
        public static void AddCacheTicketStore(this IServiceCollection services, Action<CacheTicketStoreOptions> action)
        {
            var validate = new CacheTicketStoreOptions();
            new Action<CacheTicketStoreOptions>(action).Invoke(validate);

            ArgumentNullException.ThrowIfNull(validate.CacheName);
            ArgumentNullException.ThrowIfNull(validate.KeyPrefix);

            services.Configure<CacheTicketStoreOptions>(action);
            // if another implementation isalready registered, it will not be replaced.
            // so a custom implementation can be used.
            services.TryAddTransient<ITicketStore, CacheTicketStore>();
        }

        /// <summary>Registers the <see cref="CacheTicketStore"/> with the options of a configuration section.</summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="sectionName">The section holding the <see cref="CacheTicketStoreOptions"/>. The default is <c>AuthenticationCacheTicketStore</c>. The default values of the options are used when the section does not exist.</param>
        /// <exception cref="ConfigurationException">The ticket store cannot be created.</exception>
        public static void AddCacheTicketStore(this IServiceCollection services, IConfiguration configuration, string sectionName = "AuthenticationCacheTicketStore")
        {
            var action = PrepareAction(configuration, sectionName);
            if (null == action)
            {
                throw new ConfigurationException("Ticket store cannot be created.");
            }

            AddCacheTicketStore(services, action);
        }

        internal static Action<CacheTicketStoreOptions>? PrepareAction(IConfiguration configuration, string? sectionName)
        {
            if (sectionName is null)
            {
                return null;
            }

            var section = configuration.GetSection(sectionName) as IConfigurationSection;
            var option = new CacheTicketStoreOptions();

            if (section.Exists())
            {
                option = configuration.GetSection(sectionName).Get<CacheTicketStoreOptions>() ?? option;
            }

            void options(CacheTicketStoreOptions o)
            {
                o.CacheName = option.CacheName;
                o.KeyPrefix = option.KeyPrefix;
            }

            return options;
        }
    }
}

