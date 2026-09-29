using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arc4u.Configuration.Store;

/// <summary>
/// Extension methods to map the <see cref="SectionEntity"/> in an Entity Framework Core model.
/// </summary>
/// <example>
/// <code language="csharp">
/// protected override void OnModelCreating(ModelBuilder modelBuilder)
/// {
///     modelBuilder.Entity&lt;SectionEntity&gt;().Configure();
/// }
/// </code>
/// </example>
public static class EntityTypeBuilderExtensions
{
    /// <summary>
    /// Configure the <see cref="SectionEntity"/> model.
    /// </summary>
    /// <param name="builder">The entity type builder for <see cref="SectionEntity"/>.</param>
    /// <returns>The same builder, to chain calls.</returns>
    public static EntityTypeBuilder<SectionEntity> Configure(this EntityTypeBuilder<SectionEntity> builder)
    {
        // The string holding the section name should have a length limit, but that limit should be large enough to handle realistic section names.
        builder.Property(p => p.Key).HasMaxLength(1024);
        builder.HasKey(p => p.Key);
        builder.Property(p => p.Value);
        return builder;
    }
}

