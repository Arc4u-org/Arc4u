using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arc4u.OAuth2.TicketStore
{
    /// <summary>Registers the <see cref="FileTicketStore"/>.</summary>
    public static class FileTicketStoreExtension
    {
        /// <summary>Registers the <see cref="FileTicketStoreOptions"/> and the <see cref="FileTicketStore"/> as <see cref="ITicketStore"/>, unless another ticket store is already registered. The directory is created when it does not exist.</summary>
        /// <param name="services">The service collection.</param>
        /// <param name="action">The action that configures the options.</param>
        /// <exception cref="ArgumentNullException"><see cref="FileTicketStoreOptions.StorePath"/> is <see langword="null"/>.</exception>
        public static void AddFileTicketStore(this IServiceCollection services, Action<FileTicketStoreOptions> action)
        {

            var validate = new FileTicketStoreOptions();
            new Action<FileTicketStoreOptions>(action).Invoke(validate);

            ArgumentNullException.ThrowIfNull(validate.StorePath);

            if (!validate.StorePath.Exists)
            {
                // will throw an exception if this is not possible!
                validate.StorePath.Create();
            }

            services.Configure<FileTicketStoreOptions>(action);
            services.TryAddTransient<ITicketStore, FileTicketStore>();
        }

        /// <summary>Registers the <see cref="FileTicketStore"/> with the options of a configuration section. Nothing is registered when the section does not exist.</summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="sectionName">The section holding the <see cref="FileTicketStoreOptions"/>. The default is <c>AuthenticationFileTicketStore</c>.</param>
        public static void AddFileTicketStore(this IServiceCollection services, IConfiguration configuration, string sectionName = "AuthenticationFileTicketStore")
        {
            var section = configuration.GetSection(sectionName) as IConfigurationSection;

            if (section.Exists())
            {
                var option = configuration.GetSection(sectionName).Get<FileTicketStoreOptions>();

                if (option is null)
                {
                    throw new NullReferenceException(nameof(option));
                }

                void options(FileTicketStoreOptions o)
                {
                    o.StorePath = option.StorePath;
                }

                AddFileTicketStore(services, options);
            }
        }
    }
}

