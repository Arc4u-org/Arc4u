using Arc4u.OAuth2.TicketStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class CacheTicketStoreOptionsTests
{
    [Fact]
    public void GetOptionsWithNothingInConfiguration()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddCacheTicketStore(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<CacheTicketStoreOptions>>().Value;

        Assert.Equal("Default", options.CacheName);
        Assert.Equal("AuthSessionStore-", options.KeyPrefix);
        Assert.Equal("Arc4u.TicketStore.v1", options.DataProtectionPurpose);
    }

    [Fact]
    public void GetOptionsWithSpecificCacheName()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AuthenticationCacheTicketStore:CacheName", "CustomCache" }
            })
            .Build();

        services.AddCacheTicketStore(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<CacheTicketStoreOptions>>().Value;

        Assert.Equal("CustomCache", options.CacheName);
        Assert.Equal("AuthSessionStore-", options.KeyPrefix);
    }

    [Fact]
    public void GetOptionsWithSpecificKeyPrefix()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AuthenticationCacheTicketStore:KeyPrefix", "CustomPrefix-" }
            })
            .Build();

        services.AddCacheTicketStore(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<CacheTicketStoreOptions>>().Value;

        Assert.Equal("Default", options.CacheName);
        Assert.Equal("CustomPrefix-", options.KeyPrefix);
    }

    [Fact]
    public void GetOptionsWithSpecificValues()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AuthenticationCacheTicketStore:CacheName", "CustomCache" },
                { "AuthenticationCacheTicketStore:KeyPrefix", "CustomPrefix-" },
                { "AuthenticationCacheTicketStore:DataProtectionPurpose", "Portal.TicketStore.v1" }
            })
            .Build();

        services.AddCacheTicketStore(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<CacheTicketStoreOptions>>().Value;

        Assert.Equal("CustomCache", options.CacheName);
        Assert.Equal("CustomPrefix-", options.KeyPrefix);
        Assert.Equal("Portal.TicketStore.v1", options.DataProtectionPurpose);
    }

    [Fact]
    public void GetOptionsWithSpecificDataProtectionPurpose()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AuthenticationCacheTicketStore:DataProtectionPurpose", "Portal.TicketStore.v1" }
            })
            .Build();

        services.AddCacheTicketStore(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<CacheTicketStoreOptions>>().Value;

        Assert.Equal("Default", options.CacheName);
        Assert.Equal("AuthSessionStore-", options.KeyPrefix);
        Assert.Equal("Portal.TicketStore.v1", options.DataProtectionPurpose);
    }

    [Fact]
    public void GetFileOptionsFromConfiguration()
    {
        // The StorePath is read from the configuration: binding the section could never build its DirectoryInfo.
        var services = new ServiceCollection();
        var directory = Directory.CreateTempSubdirectory("arc4u-tickets-");
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "AuthenticationFileTicketStore:StorePath", directory.FullName },
                    { "AuthenticationFileTicketStore:DataProtectionPurpose", "Portal.TicketStore.v1" }
                })
                .Build();

            services.AddFileTicketStore(configuration);

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<FileTicketStoreOptions>>().Value;

            Assert.Equal(directory.FullName, options.StorePath!.FullName);
            Assert.Equal("Portal.TicketStore.v1", options.DataProtectionPurpose);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void GetFileOptionsFromConfigurationWithTheDefaultPurpose()
    {
        var services = new ServiceCollection();
        var directory = Directory.CreateTempSubdirectory("arc4u-tickets-");
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "AuthenticationFileTicketStore:StorePath", directory.FullName }
                })
                .Build();

            services.AddFileTicketStore(configuration);

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<FileTicketStoreOptions>>().Value;

            Assert.Equal("Arc4u.TicketStore.v1", options.DataProtectionPurpose);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void GetFileOptionsFromConfigurationWithoutStorePathIsRefused()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AuthenticationFileTicketStore:DataProtectionPurpose", "Portal.TicketStore.v1" }
            })
            .Build();

        Assert.Throws<ArgumentNullException>(() => services.AddFileTicketStore(configuration));
    }

    [Fact]
    public void GetFileOptionsWithNothingSet()
    {
        Assert.Equal("Arc4u.TicketStore.v1", new FileTicketStoreOptions().DataProtectionPurpose);
    }
}
