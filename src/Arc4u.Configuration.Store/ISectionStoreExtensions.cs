using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using Arc4u.Configuration.Store.Internals;

namespace Arc4u.Configuration.Store;

/// <summary>
/// Extension methods that simplify the use of an <see cref="ISectionStore"/>.
/// </summary>
public static class ISectionStoreExtensions
{
    /// <summary>
    /// Update an entity in the store
    /// </summary>
    /// <param name="sectionStore"></param>
    /// <param name="entity"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task UpdateAsync(this ISectionStore sectionStore, SectionEntity entity, CancellationToken cancellationToken) => sectionStore.UpdateAsync(new[] { entity }, cancellationToken);

    /// <summary>
    /// Try to get a value of type <typeparamref name="TValue"/> from the section named <paramref name="key"/>
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="sectionStore"></param>
    /// <param name="key"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A tuple: if the Valid member is true, the Value member contains the value. False if there is no section named <paramref name="key"/></returns>
    /// <remarks>The value is deserialized with reflection, which trimming and native AOT do not support: use the overload taking a <see cref="JsonTypeInfo{T}"/> there.</remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static Task<(bool Valid, TValue? Value)> TryGetValueAsync<TValue>(this ISectionStore sectionStore, string key, CancellationToken cancellationToken)
        => sectionStore.TryGetValueAsync(key, ValueHolder.GetReflectionTypeInfo<TValue>(), cancellationToken);

    /// <summary>
    /// Try to get a value of type <typeparamref name="TValue"/> from the section named <paramref name="key"/>, deserialized with <paramref name="typeInfo"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="sectionStore">The section store.</param>
    /// <param name="key">The name of the section.</param>
    /// <param name="typeInfo">The Json metadata of <typeparamref name="TValue"/>, typically <c>MyJsonContext.Default.MyOptions</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A tuple: if the Valid member is true, the Value member contains the value. False if there is no section named <paramref name="key"/></returns>
    public static async Task<(bool Valid, TValue? Value)> TryGetValueAsync<TValue>(this ISectionStore sectionStore, string key, JsonTypeInfo<TValue> typeInfo, CancellationToken cancellationToken)
    {
        var sectionEntity = await sectionStore.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (sectionEntity is null)
        {
            return (false, default);
        }

        return (true, sectionEntity.GetValue(typeInfo));
    }

    /// <summary>
    /// Try to set a value of type <typeparamref name="TValue"/> to the section named <paramref name="key"/>
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="sectionStore"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>True if the value was set successfully. False if there is no section named <paramref name="key"/></returns>
    /// <remarks>The value is serialized with reflection, which trimming and native AOT do not support: use the overload taking a <see cref="JsonTypeInfo{T}"/> there.</remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static Task<bool> TrySetValueAsync<TValue>(this ISectionStore sectionStore, string key, TValue? value, CancellationToken cancellationToken)
        => sectionStore.TrySetValueAsync(key, value, ValueHolder.GetReflectionTypeInfo<TValue>(), cancellationToken);

    /// <summary>
    /// Try to set a value of type <typeparamref name="TValue"/> to the section named <paramref name="key"/>, serialized with <paramref name="typeInfo"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="sectionStore">The section store.</param>
    /// <param name="key">The name of the section.</param>
    /// <param name="value">The value.</param>
    /// <param name="typeInfo">The Json metadata of <typeparamref name="TValue"/>, typically <c>MyJsonContext.Default.MyOptions</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if the value was set successfully. False if there is no section named <paramref name="key"/></returns>
    public static async Task<bool> TrySetValueAsync<TValue>(this ISectionStore sectionStore, string key, TValue? value, JsonTypeInfo<TValue> typeInfo, CancellationToken cancellationToken)
    {
        var sectionEntity = await sectionStore.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (sectionEntity is null)
        {
            return false;
        }

        sectionEntity.SetValue(value, typeInfo);
        await sectionStore.UpdateAsync(sectionEntity, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private const string ReflectionMessage = "The value is serialized with reflection. Use the overload taking a JsonTypeInfo<TValue>, generated by a JsonSerializerContext in your application.";
}
