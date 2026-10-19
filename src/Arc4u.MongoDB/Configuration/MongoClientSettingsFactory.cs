using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Arc4u.MongoDB.Configuration;

/// <summary>
/// Connection strings registered by <c>AddMongoDatabase(configuration, key)</c>, by options name (the database name in lower case).
/// </summary>
internal sealed class MongoConnectionStrings : Dictionary<string, string>
{
}

/// <summary>
/// Creates the named <see cref="MongoClientSettings"/> directly from the registered connection string, so every option the driver parses is kept.
/// The configure, post-configure and validate steps registered for the name still apply on top.
/// </summary>
internal sealed class MongoClientSettingsFactory : OptionsFactory<MongoClientSettings>
{
    public MongoClientSettingsFactory(MongoConnectionStrings connectionStrings,
                                      IEnumerable<IConfigureOptions<MongoClientSettings>> setups,
                                      IEnumerable<IPostConfigureOptions<MongoClientSettings>> postConfigures,
                                      IEnumerable<IValidateOptions<MongoClientSettings>> validations)
        : base(setups, postConfigures, validations)
    {
        _connectionStrings = connectionStrings;
    }

    private readonly MongoConnectionStrings _connectionStrings;

    protected override MongoClientSettings CreateInstance(string name)
    {
        return _connectionStrings.TryGetValue(name, out var connectionString)
            ? MongoClientSettings.FromConnectionString(connectionString)
            : base.CreateInstance(name);
    }
}
