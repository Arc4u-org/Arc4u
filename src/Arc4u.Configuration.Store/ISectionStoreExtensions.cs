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
    public static async Task<(bool Valid, TValue? Value)> TryGetValueAsync<TValue>(this ISectionStore sectionStore, string key, CancellationToken cancellationToken)
    {
        var sectionEntity = await sectionStore.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (sectionEntity is null)
        {
            return (false, default);
        }

        return (true, sectionEntity.GetValue<TValue>());
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
    public static async Task<bool> TrySetValueAsync<TValue>(this ISectionStore sectionStore, string key, TValue? value, CancellationToken cancellationToken)
    {
        var sectionEntity = await sectionStore.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (sectionEntity is null)
        {
            return false;
        }

        sectionEntity.SetValue(value);
        await sectionStore.UpdateAsync(sectionEntity, cancellationToken).ConfigureAwait(false);
        return true;
    }

}
