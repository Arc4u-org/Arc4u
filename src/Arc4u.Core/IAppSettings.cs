namespace Arc4u;

/// <summary>
/// A set of key/value settings describing the application (see <see cref="IKeyValueSettings"/>).
/// It is a marker interface that allows the application-wide settings to be resolved from the dependency injection container.
/// </summary>
public interface IAppSettings : IKeyValueSettings
{
}
