using System.Security.Claims;
using Arc4u.OAuth2.TicketStore;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Security;

/// <summary>
/// The ticket stores encrypt the tickets with Data Protection: a ticket carries the user's access, id and refresh tokens (<c>SaveTokens</c>),
/// which whoever can read the cache or the store directory must not be able to read (Arc4u.Portal #704). A ticket written in clear by a
/// previous version reads as no session and is removed. The purpose is an option, so Arc4u.Portal keeps the sessions its own store encrypted.
/// </summary>
[Trait("Category", "CI")]
public sealed class TicketStoreEncryptionTests : IDisposable
{
    private readonly ServiceProvider _container = SignOutTestSupport.BuildContainer(_ => { });
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("arc4u-tickets-");
    private readonly EphemeralDataProtectionProvider _dataProtection = new();
    private readonly Dictionary<string, byte[]> _entries = [];

    public void Dispose()
    {
        _container.Dispose();
        _directory.Delete(recursive: true);
    }

    public static TheoryData<string> Stores => ["cache", "file"];

    [Theory]
    [MemberData(nameof(Stores))]
    public async Task The_stored_bytes_are_not_the_serialized_ticket(string kind)
    {
        // Arrange
        var sut = CreateStore(kind);
        var ticket = Ticket(("sub", "user"), ("access_token", "secret-token"));

        // Act
        var key = await sut.StoreAsync(ticket);

        // Assert
        var stored = Stored(kind, key);
        stored.Should().NotEqual(TicketSerializer.Default.Serialize(ticket));
        System.Text.Encoding.UTF8.GetString(stored).Should().NotContain("secret-token");
    }

    [Theory]
    [MemberData(nameof(Stores))]
    public async Task A_stored_ticket_is_read_back(string kind)
    {
        // Arrange
        var sut = CreateStore(kind);
        var key = await sut.StoreAsync(Ticket(("sub", "user")));

        // Act
        var ticket = await sut.RetrieveAsync(key);

        // Assert
        ticket!.Principal.FindFirst("sub")!.Value.Should().Be("user");
    }

    [Theory]
    [MemberData(nameof(Stores))]
    public async Task A_ticket_stored_in_clear_reads_as_no_session_and_is_removed(string kind)
    {
        // Arrange
        var sut = CreateStore(kind);
        const string key = "legacy";
        Write(kind, key, TicketSerializer.Default.Serialize(Ticket(("sub", "user"))));

        // Act
        var ticket = await sut.RetrieveAsync(key);

        // Assert
        ticket.Should().BeNull();
        Exists(kind, key).Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(Stores))]
    public async Task A_ticket_protected_elsewhere_with_the_configured_purpose_and_key_ring_is_read(string kind)
    {
        // Arrange: as Arc4u.Portal's own store wrote them, read by a store configured with the Portal's purpose.
        var sut = CreateStore(kind, "Portal.TicketStore.v1");
        const string key = "from-the-portal";
        var protector = _dataProtection.CreateProtector("Portal.TicketStore.v1");
        Write(kind, key, protector.Protect(TicketSerializer.Default.Serialize(Ticket(("sub", "user")))));

        // Act
        var ticket = await sut.RetrieveAsync(key);

        // Assert
        ticket!.Principal.FindFirst("sub")!.Value.Should().Be("user");
    }

    [Theory]
    [MemberData(nameof(Stores))]
    public async Task A_ticket_protected_under_another_purpose_reads_as_no_session(string kind)
    {
        // Arrange: the default purpose does not read the Portal's tickets.
        var sut = CreateStore(kind);
        const string key = "from-the-portal";
        var protector = _dataProtection.CreateProtector("Portal.TicketStore.v1");
        Write(kind, key, protector.Protect(TicketSerializer.Default.Serialize(Ticket(("sub", "user")))));

        // Act
        var ticket = await sut.RetrieveAsync(key);

        // Assert
        TicketStoreDataProtection.DefaultPurpose.Should().Be("Arc4u.TicketStore.v1");
        ticket.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(Stores))]
    public async Task A_ticket_protected_under_another_key_ring_reads_as_no_session(string kind)
    {
        // Arrange
        var sut = CreateStore(kind);
        const string key = "lost-key-ring";
        var protector = new EphemeralDataProtectionProvider().CreateProtector(TicketStoreDataProtection.DefaultPurpose);
        Write(kind, key, protector.Protect(TicketSerializer.Default.Serialize(Ticket(("sub", "user")))));

        // Act
        var ticket = await sut.RetrieveAsync(key);

        // Assert
        ticket.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(Stores))]
    public async Task A_renewal_over_a_ticket_stored_in_clear_writes_an_encrypted_ticket(string kind)
    {
        // Arrange
        var sut = CreateStore(kind);
        const string key = "legacy";
        Write(kind, key, TicketSerializer.Default.Serialize(Ticket(("sub", "attacker"))));

        // Act: the unreadable entry holds no ticket to compare with, like a missing one.
        await sut.RenewAsync(key, Ticket(("sub", "user")));

        // Assert
        (await sut.RetrieveAsync(key))!.Principal.FindFirst("sub")!.Value.Should().Be("user");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void An_empty_purpose_is_refused(string purpose)
    {
        // Act
        var cache = () => CreateStore("cache", purpose);
        var file = () => CreateStore("file", purpose);
        var register = () => new ServiceCollection().AddCacheTicketStore(o => o.DataProtectionPurpose = purpose);

        // Assert
        cache.Should().Throw<ArgumentException>();
        file.Should().Throw<ArgumentException>();
        register.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Registering_a_ticket_store_registers_data_protection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCacheTicketStore(o => { });
        services.AddFileTicketStore(o => o.StorePath = _directory);

        // Assert
        services.Should().Contain(d => d.ServiceType == typeof(IDataProtectionProvider));
    }

    private ITicketStore CreateStore(string kind, string purpose = TicketStoreDataProtection.DefaultPurpose) => kind switch
    {
        "cache" => new CacheTicketStore(
            _container.GetRequiredService<ILogger<CacheTicketStore>>(),
            TicketStoreSubjectTests.CreateCacheContext(_entries),
            Mock.Of<IOptionsMonitor<CacheTicketStoreOptions>>(m => m.CurrentValue == new CacheTicketStoreOptions { DataProtectionPurpose = purpose }),
            _dataProtection),
        "file" => new FileTicketStore(
            _container.GetRequiredService<ILogger<FileTicketStore>>(),
            Mock.Of<IOptionsMonitor<FileTicketStoreOptions>>(m => m.CurrentValue == new FileTicketStoreOptions { StorePath = _directory, DataProtectionPurpose = purpose }),
            _dataProtection),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private string FilePath(string key) => Path.Combine(_directory.FullName, key + ".bin");

    private byte[] Stored(string kind, string key) => kind == "cache" ? _entries[key] : File.ReadAllBytes(FilePath(key));

    private bool Exists(string kind, string key) => kind == "cache" ? _entries.ContainsKey(key) : File.Exists(FilePath(key));

    private void Write(string kind, string key, byte[] content)
    {
        if (kind == "cache")
        {
            _entries[key] = content;
        }
        else
        {
            File.WriteAllBytes(FilePath(key), content);
        }
    }

    private static AuthenticationTicket Ticket(params (string Type, string Value)[] claims)
        => new(new ClaimsPrincipal(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "Cookies")), CookieAuthenticationDefaults.AuthenticationScheme);
}
