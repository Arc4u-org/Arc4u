using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.Configuration.Sql;
/// <summary>Extension methods to register the options of a SQL Server cache.</summary>
public static class SqlCacheExtension
{
    /// <summary>Registers the options of the SQL Server cache with the given name.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the cache, as used in <see cref="CachingCache.Name"/>.</param>
    /// <param name="options">The action that configures the options.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    /// <example>
    /// <code language="csharp">
    /// builder.Services.AddSqlCache("Shared", o => o.ConnectionString = "Server=.;Database=Cache;Integrated Security=true");
    /// </code>
    /// </example>
    public static IServiceCollection AddSqlCache(this IServiceCollection services, [DisallowNull] string name, Action<SqlCacheOption> options)
    {
        var validate = new SqlCacheOption();
        new Action<SqlCacheOption>(options).Invoke(validate);

        ArgumentException.ThrowIfNullOrEmpty(name);

        services.Configure<SqlCacheOption>(name, options);

        return services;
    }

    /// <summary>Registers the options of the SQL Server cache with the given name from a configuration section. Nothing is registered when the section does not exist.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the cache, as used in <see cref="CachingCache.Name"/>.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="sectionName">The path of the section that holds the <see cref="SqlCacheOption"/> values.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty and the section exists.</exception>
    public static IServiceCollection AddSqlCache(this IServiceCollection services, [DisallowNull] string name, [DisallowNull] IConfiguration configuration, [DisallowNull] string sectionName)
    {
        var section = configuration.GetSection(sectionName) as IConfigurationSection;

        if (section.Exists())
        {
            var option = configuration.GetSection(sectionName).Get<SqlCacheOption>();

            if (option is null)
            {
                throw new NullReferenceException(nameof(option));
            }

            void options(SqlCacheOption o)
            {
                o.SerializerName = option.SerializerName;
                o.SchemaName = option.SchemaName;
                o.TableName = option.TableName;
                o.ConnectionString = option.ConnectionString;
            }

            services.AddSqlCache(name, options);
        }

        return services;
    }

}
