using System.Text;
using Arc4u.Configuration.Decryptor;
using Arc4u.Security.Cryptography;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Xunit;

namespace Arc4u.UnitTest.Decryptor;

/// <summary>
/// The decryptor must read the providers defined before it, not build their sources again (issue #246).
/// </summary>
[Trait("Category", "CI")]
public class PreviousProvidersDecryptor
{
    private const string ClearText = "Server=.;Database=Orders";

    public static TheoryData<string> Builders => new() { nameof(ConfigurationBuilder), nameof(ConfigurationManager) };

    [Theory]
    [MemberData(nameof(Builders))]
    public void Json_Stream_Before_Decryptor_Should_Decrypt(string builderType)
    {
        // arrange
        var builder = CreateBuilder(builderType);
        builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(Json())));

        // act
        builder.AddRijndaelDecryptorConfiguration();
        var configuration = builder is IConfigurationRoot root ? root : builder.Build();

        // assert
        configuration["ConnectionStrings:Orders"].Should().Be(ClearText);
    }

    [Theory]
    [MemberData(nameof(Builders))]
    public void Previous_Source_Should_Be_Loaded_Once(string builderType)
    {
        // arrange
        var counting = new CountingSource(Values());
        var builder = CreateBuilder(builderType);
        builder.Add(counting);

        // act
        builder.AddRijndaelDecryptorConfiguration();
        var configuration = builder is IConfigurationRoot root ? root : builder.Build();

        // assert
        configuration["ConnectionStrings:Orders"].Should().Be(ClearText);
        counting.Loads.Should().Be(1);
    }

    [Fact]
    public void Later_Previous_Provider_Should_Win()
    {
        // arrange
        var values = Values();
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Orders"] = "Decrypt:not a cypher" })
            .AddInMemoryCollection(values);

        // act
        var configuration = builder.AddRijndaelDecryptorConfiguration().Build();

        // assert
        configuration["ConnectionStrings:Orders"].Should().Be(ClearText);
    }

    private static IConfigurationBuilder CreateBuilder(string builderType)
        => builderType == nameof(ConfigurationManager) ? new ConfigurationManager() : new ConfigurationBuilder();

    private static (string Key, string IV, string Cypher) Encrypt()
    {
        var key = CypherCodec.GenerateKeyAndIV(out var iv);
        return (key, iv, CypherCodec.EncodeClearText(ClearText, Convert.FromBase64String(key), Convert.FromBase64String(iv)));
    }

    private static Dictionary<string, string?> Values()
    {
        var (key, iv, cypher) = Encrypt();
        return new Dictionary<string, string?>
        {
            ["EncryptionRijndael:Key"] = key,
            ["EncryptionRijndael:IV"] = iv,
            ["ConnectionStrings:Orders"] = $"Decrypt:{cypher}",
        };
    }

    private static string Json()
    {
        var (key, iv, cypher) = Encrypt();
        return $$"""
                 {
                   "EncryptionRijndael": { "Key": "{{key}}", "IV": "{{iv}}" },
                   "ConnectionStrings": { "Orders": "Decrypt:{{cypher}}" }
                 }
                 """;
    }

    private sealed class CountingSource(Dictionary<string, string?> values) : IConfigurationSource
    {
        public int Loads { get; private set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder) => new CountingProvider(this, values);

        private sealed class CountingProvider(CountingSource owner, Dictionary<string, string?> values)
            : MemoryConfigurationProvider(new MemoryConfigurationSource { InitialData = values })
        {
            public override void Load()
            {
                owner.Loads++;
                base.Load();
            }
        }
    }
}
