using Arc4u.Configuration.Store.EFCore.Internals;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arc4u.Configuration.Store;

/// <summary>
/// Extension methods to register the Entity Framework Core implementation of the section store.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers, as a scoped service (if not already registered), an <see cref="ISectionStore"/> that persists the sections in the specified <see cref="DbContext"/>.
    /// The context must expose a <see cref="SectionEntity"/> entity (see <see cref="EntityTypeBuilderExtensions.Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder{SectionEntity})"/>).
    /// </summary>
    /// <typeparam name="TDbContext">The type of the <see cref="DbContext"/> holding the <see cref="SectionEntity"/> table.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, to chain calls.</returns>
    /// <example>
    /// <code language="csharp">
    /// services.AddDbContext&lt;MyDbContext&gt;(options =&gt; options.UseSqlServer(connectionString));
    /// services.AddDbContextSectionStore&lt;MyDbContext&gt;();
    /// services.AddSectionStoreService();
    /// </code>
    /// </example>
    public static IServiceCollection AddDbContextSectionStore<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
    {
        services.TryAddScoped<ISectionStore, DbContextSectionStore<TDbContext>>();
        return services;
    }
}

