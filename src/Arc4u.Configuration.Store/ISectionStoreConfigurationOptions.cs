using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace Arc4u.Configuration.Store;

/// <summary>
/// Configuration options for persisted configuration sections
/// </summary>
public interface ISectionStoreConfigurationOptions
{
    /// <summary>
    /// Add a section with a specific value to be persisted
    /// </summary>
    /// <typeparam name="TValue">The type of the value</typeparam>
    /// <param name="key">the name of the section</param>
    /// <param name="value">the value of the section</param>
    /// <returns></returns>
    ISectionStoreConfigurationOptions Add<TValue>(string key, TValue? value);

    /// <summary>
    /// Add a section from the existing configuration to be persisted.
    /// </summary>
    /// <remarks>The section is bound with reflection, which trimming and native AOT do not support: use <see cref="AddFromConfiguration{TValue}(string, Func{IConfigurationSection, TValue})"/> there.</remarks>
    /// <typeparam name="TValue">The type of the value in the existing configuration</typeparam>
    /// <param name="key">the name of the section</param>
    /// <returns></returns>
    [RequiresUnreferencedCode("The section is bound with reflection. Use AddFromConfiguration with a binding function, e.g. s => s.Get<MyOptions>(), so the binding is generated in your application.")]
    [RequiresDynamicCode("The section is bound with reflection. Use AddFromConfiguration with a binding function, e.g. s => s.Get<MyOptions>(), so the binding is generated in your application.")]
    ISectionStoreConfigurationOptions Add<TValue>(string key);

    /// <summary>
    /// Add a section from the existing configuration to be persisted, bound by <paramref name="bind"/>.
    /// Because the binding is written in the application, where <typeparamref name="TValue"/> is known, the configuration
    /// binding generator of the application generates it: unlike <see cref="Add{TValue}(string)"/>, it works with trimming and native AOT.
    /// </summary>
    /// <typeparam name="TValue">The type of the value in the existing configuration</typeparam>
    /// <param name="key">the name of the section</param>
    /// <param name="bind">The function that reads the section, typically <c>section =&gt; section.Get&lt;MyOptions&gt;()</c>.</param>
    /// <returns>The options, to chain calls.</returns>
    ISectionStoreConfigurationOptions AddFromConfiguration<TValue>(string key, Func<IConfigurationSection, TValue?> bind);
}
