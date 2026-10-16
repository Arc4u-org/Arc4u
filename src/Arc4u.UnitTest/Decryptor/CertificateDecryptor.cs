using System.Security.Cryptography.X509Certificates;
using Arc4u.Configuration.Decryptor;
using Arc4u.Security;
using Arc4u.Security.Cryptography;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;
using X509CertificateLoader = Arc4u.Security.Cryptography.X509CertificateLoader;

namespace Arc4u.UnitTest.Decryptor;

[Trait("Category", "CI")]
public class CertificateDecryptor
{
    private readonly Fixture _fixture;

    public CertificateDecryptor()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void CertficateShouldThrowKeyNotFoundException()
    {
        // arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["EncryptionCertificate:Store:Name"] = "friendlyName" });

        var sut = new SecretConfigurationCertificateProvider(
            new SecretCertificateOptions
            {
                Prefix = "Prefix:", CertificateLoader = new X509CertificateLoader(default!)
            }, config.Sources);

        // act
        var exception = Record.Exception(sut.Load);

        // assert
        exception.Should().BeOfType<KeyNotFoundException>();
    }

    [Fact]
    public void Certficate_Small_Text_ShouldDecrypt()
    {
        // arrange
        var certificate = GetX509Certificate2();

        var plainText = _fixture.Create<string>();
        var cypherText = certificate.Encrypt(plainText);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Toto"] = $"Tag:{cypherText}" });

        var sut = new SecretConfigurationCertificateProvider(
            new SecretCertificateOptions { Prefix = "Tag:", Certificate = certificate }, config.Sources);

        // act
        sut.Load();

        // assert
        sut.TryGet("ConnectionStrings:Toto", out var value).Should().BeTrue();
        value.Should().NotBeNull();
        value.Should().Be(plainText);
        cypherText.Should().NotContain(".");
    }

    [Fact]
    public void Certficate_Large_Text_ShouldDecrypt()
    {
        // arrange
        var certificate = GetX509Certificate2();

        var plainText = new string('A', 600);
        var cypherText = certificate.Encrypt(plainText);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Toto"] = $"Tag:{cypherText}" });

        var sut = new SecretConfigurationCertificateProvider(
            new SecretCertificateOptions { Prefix = "Tag:", Certificate = certificate }, config.Sources);

        // act
        sut.Load();

        // assert
        sut.TryGet("ConnectionStrings:Toto", out var value).Should().BeTrue();
        cypherText.Should().Contain(".");
        value.Should().NotBeNull();
        value.Should().Be(plainText);
    }

    [Fact]
    public void CertficateWithCustomPathShouldDecrypt()
    {
        // arrange
        var certificate = GetX509Certificate2();

        var plainText = _fixture.Create<string>();
        var cypherText = certificate.Encrypt(plainText);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:DataProtection:EncryptionCertificate:File:Cert"] = @"./Configs/cert.pem",
                    ["Authentication:DataProtection:EncryptionCertificate:File:Key"] = @"./Configs/key.pem",
                    ["ConnectionStrings:Toto"] = $"Decrypt:{cypherText}"
                });

        var sut = new SecretConfigurationCertificateProvider(
            new SecretCertificateOptions
            {
                SecretSectionName = "Authentication:DataProtection:EncryptionCertificate", Certificate = certificate
            }, config.Sources);

        // act
        sut.Load();

        // assert
        sut.TryGet("ConnectionStrings:Toto", out var value).Should().BeTrue();
        value.Should().NotBeNull();
        value.Should().Be(plainText);
    }

    [Fact]
    public void CertficateWithPemFilesShouldDecrypt()
    {
        // arrange
        var certificate = GetX509Certificate2();

        var plainText = _fixture.Create<string>();
        var cypherText = certificate.Encrypt(plainText);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["EncryptionCertificate:File:Cert"] = @"./Configs/cert.pem",
                    ["EncryptionCertificate:File:Key"] = @"./Configs/key.pem",
                    ["ConnectionStrings:Toto"] = $"Tag:{cypherText}"
                });

        var sut = new SecretConfigurationCertificateProvider(
            new SecretCertificateOptions { Prefix = "Tag:", Certificate = certificate }, config.Sources);

        // act
        sut.Load();

        // assert
        sut.TryGet("ConnectionStrings:Toto", out var value).Should().BeTrue();
        value.Should().NotBeNull();
        value.Should().Be(plainText);
    }

    [Fact]
    public void CertficateShouldNotDecrypt()
    {
        // arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Toto"] = "Tag:Database" });

        var sut = new SecretConfigurationCertificateProvider(new SecretCertificateOptions { Prefix = "Tag:" },
            config.Sources);

        // act
        sut.Load();

        // assert
        sut.TryGet("ConnectionStrings:Toto", out _).Should().BeFalse();
    }

    [Fact]
    public void RijndaelShouldDecrypt()
    {
        // arrange
        var stringKey = CypherCodec.GenerateKey();

        CypherCodecConfig rijndaelConfig = new() { Key = stringKey };

        var plainText = _fixture.Create<string>();
        var cypherText = CypherCodec.Encrypt(plainText, Convert.FromBase64String(stringKey));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Toto"] = $"Tag:{cypherText}" });

        var sut = new SecretRijndaelConfigurationProvider(
            new SecretRijndaelOptions { Prefix = "Tag:", RijnDael = rijndaelConfig }, config.Sources);

        // act
        sut.Load();

        // assert
        sut.TryGet("ConnectionStrings:Toto", out var value).Should().BeTrue();
        value.Should().NotBeNull();
        value.Should().Be(plainText);
    }

    [Fact]
    public void RijndaelConfigShouldBe()
    {
        // arrange
        var stringKey = CypherCodec.GenerateKey();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["EncryptionRijndael:Key"] = stringKey }).Build();
        var tempRoot = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        // act
        var rijndaelConfig = tempRoot.GetSection("EncryptionRijndael").Get<CypherCodecConfig>();

        // assert
        rijndaelConfig.Should().NotBeNull();
        rijndaelConfig!.Key.Should().NotBeNull();
        rijndaelConfig.Key.Should().Be(stringKey);
    }

    [Fact]
    public void RijndaelWithConfigShouldDecrypt()
    {
        // arrange
        var stringKey = CypherCodec.GenerateKey();

        var plainText = _fixture.Create<string>();
        var cypherText = CypherCodec.Encrypt(plainText, Convert.FromBase64String(stringKey));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["EncryptionRijndael:Key"] = stringKey,
                    ["ConnectionStrings:Toto"] = $"Tag:{cypherText}"
                });

        var sut = new SecretRijndaelConfigurationProvider(new SecretRijndaelOptions { Prefix = "Tag:" },
            config.Sources);

        // act
        sut.Load();

        // assert
        sut.TryGet("ConnectionStrings:Toto", out var value).Should().BeTrue();
        value.Should().NotBeNull();
        value.Should().Be(plainText);
    }

    [Fact]
    public void EncryptShouldUseANewNonceForEachValue()
    {
        // arrange
        var key = Convert.FromBase64String(CypherCodec.GenerateKey());
        var plainText = _fixture.Create<string>();

        // act
        var first = CypherCodec.Encrypt(plainText, key);
        var second = CypherCodec.Encrypt(plainText, key);

        // assert
        first.Should().NotBe(second);
        CypherCodec.Decrypt(first, key).Should().Be(plainText);
        CypherCodec.Decrypt(second, key).Should().Be(plainText);
    }

    [Fact]
    public void DecryptShouldDetectTampering()
    {
        // arrange
        var key = Convert.FromBase64String(CypherCodec.GenerateKey());
        var cypherText = CypherCodec.Encrypt(_fixture.Create<string>(), key);
        var data = Convert.FromBase64String(cypherText);
        data[^1] ^= 0x01;
        var tampered = Convert.ToBase64String(data);

        // act
        var exception = Record.Exception(() => CypherCodec.Decrypt(tampered, key));

        // assert
        exception.Should().BeAssignableTo<System.Security.Cryptography.CryptographicException>();
    }

    [Fact]
    public void DecryptWithAnotherKeyShouldThrow()
    {
        // arrange
        var cypherText = CypherCodec.Encrypt(_fixture.Create<string>(), Convert.FromBase64String(CypherCodec.GenerateKey()));

        // act
        var exception = Record.Exception(() => CypherCodec.Decrypt(cypherText, Convert.FromBase64String(CypherCodec.GenerateKey())));

        // assert
        exception.Should().BeAssignableTo<System.Security.Cryptography.CryptographicException>();
    }

    [Fact]
    public void CertificateShouldEncryptAndDecryptLongText()
    {
        // arrange
        using var certificate = GetX509Certificate2();
        var plainText = new string('x', 500);

        // act
        var cypherText = certificate.Encrypt(plainText);

        // assert
        cypherText.Split('.').Should().HaveCount(2);
        certificate.Decrypt(cypherText).Should().Be(plainText);
    }

    public static X509Certificate2 GetX509Certificate2()
    {
        var certificatestring =
            "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";
        var privateKeyBytes = Convert.FromBase64String(certificatestring);
        var pfxPassword = "Password";
#pragma warning disable SYSLIB0057 // Type or member is obsolete
        return new X509Certificate2(privateKeyBytes, pfxPassword);
#pragma warning restore SYSLIB0057 // Type or member is obsolete
    }
}
