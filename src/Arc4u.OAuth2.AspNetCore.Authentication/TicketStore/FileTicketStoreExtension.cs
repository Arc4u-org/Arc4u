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
            var dataProtectionPurpose = validate.DataProtectionPurpose;
            ArgumentException.ThrowIfNullOrWhiteSpace(dataProtectionPurpose);

            if (!validate.StorePath.Exists)
            {
                // will throw an exception if this is not possible!
                validate.StorePath.Create();
            }

            services.Configure<FileTicketStoreOptions>(action);
            // The store encrypts the tickets. AddDataProtection only adds what is missing, so the key ring configured by AddOidcAuthentication
            // (or by the application) is kept; without one, the tickets cannot be read by another instance or after a restart.
            services.AddDataProtection();
            services.TryAddTransient<ITicketStore, FileTicketStore>();
        }

        /// <summary>Registers the <see cref="FileTicketStore"/> with the options of a configuration section. Nothing is registered when the section does not exist.</summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="sectionName">The section holding the <see cref="FileTicketStoreOptions"/>. The default is <c>AuthenticationFileTicketStore</c>.</param>
        public static void AddFileTicketStore(this IServiceCollection services, IConfiguration configuration, string sectionName = "AuthenticationFileTicketStore")
        {
            var section = configuration.GetSection(sectionName);

            if (section.Exists())
            {
                // Read as strings: the configuration binder cannot build a DirectoryInfo, so binding the section always left StorePath null.
                var storePath = section[nameof(FileTicketStoreOptions.StorePath)];
                var purpose = section[nameof(FileTicketStoreOptions.DataProtectionPurpose)];

                void options(FileTicketStoreOptions o)
                {
                    o.StorePath = string.IsNullOrWhiteSpace(storePath) ? null : new DirectoryInfo(storePath);
                    if (purpose is not null)
                    {
                        o.DataProtectionPurpose = purpose;
                    }
                }

                AddFileTicketStore(services, options);
            }
        }
    }
}

