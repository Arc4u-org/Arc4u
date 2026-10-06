using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Arc4u.Configuration;
using Arc4u.Security.Cryptography;
using Arc4u.Security.CustomRootCA;
using Arc4u.UnitTest.Decryptor;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using X509CertificateLoader = Arc4u.Security.Cryptography.X509CertificateLoader;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class CertificateTests
{
    private readonly Fixture _fixture;

    public CertificateTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void FileCertificateShouldBe()
    {
        //arrange
        var publicCert = @"./Configs/cert.pem";
        var privateCert = @"./Configs/key.pem";
        var plainText = "FileCertificateShouldBe()";

        // act
        var certificate = X509Certificate2.CreateFromPemFile(publicCert, privateCert);
        var cypherText = certificate.Encrypt(plainText);
        var sut = certificate.Decrypt(cypherText);

        // assert
        certificate.Should().NotBeNull();
        sut.Should().Be(plainText);
    }

    [Theory]
    [InlineData("0123456789")]
    [InlineData("012345678901234567890123456789012345678901234567890123456789")]
    [InlineData(
        "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789")]
    public void Small_Text_Shoud_Directly_Encrypted(string plainText)
    {
        //arranges
        var certificate = CertificateDecryptor.GetX509Certificate2();

        // act
        var cypherText = certificate.Encrypt(plainText);
        var sut = certificate.Decrypt(cypherText);

        // assert
        certificate.Should().NotBeNull();
        cypherText.Should().NotContain(".");
        sut.Should().Be(plainText);
    }

    [Fact]
    public void Large_Text_Shoud_Encrypted_With_Aes()
    {
        //arranges
        var certificate = CertificateDecryptor.GetX509Certificate2();
        var plainText = new string('A', 600);
        // act
        var cypherText = certificate.Encrypt(plainText);
        var sut = certificate.Decrypt(cypherText);

        // assert
        certificate.Should().NotBeNull();
        cypherText.Should().Contain(".");
        sut.Should().Be(plainText);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(600)]
    public void Encrypt_With_Ecdsa_Certificate_Should_Throw_NotSupportedException(int length)
    {
        // arrange
        using var certificate = CreateEcdsaCertificate();
        var plainText = new string('A', length);

        // act
        var exception = Record.Exception(() => certificate.Encrypt(plainText));

        // assert
        exception.Should().BeOfType<NotSupportedException>();
        exception!.Message.Should().Contain("RSA");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(600)]
    public void Decrypt_With_Ecdsa_Certificate_Should_Throw_NotSupportedException(int length)
    {
        // arrange
        var cypherText = CertificateDecryptor.GetX509Certificate2().Encrypt(new string('A', length));
        using var certificate = CreateEcdsaCertificate();

        // act
        var exception = Record.Exception(() => certificate.Decrypt(cypherText));

        // assert
        exception.Should().BeOfType<NotSupportedException>();
        exception!.Message.Should().Contain("RSA");
    }

    private static X509Certificate2 CreateEcdsaCertificate()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=Arc4u ECDSA", ecdsa, HashAlgorithmName.SHA256);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));
    }
    #if NET10_0
    [Fact]
    public void Custom_Root_CA_With_One_Certificate_Should_Be_Registered()
    {
        var pem = GenerateRsaPublicKeyPem();

        var services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["CustomRootCA:Pem:CaPem"] = pem,
                    ["CustomRootCA:File:CaFilePath"] = "/app/cert/ca.pem",
                    ["CustomRootCA:KeyChain:Store:Location"] = "CurrentUser",
                    ["CustomRootCA:KeyChain:Store:StoreName"] = "Root",
                    ["CustomRootCA:KeyChain:Store:FindType"] = "FindByThumbprint",
                    ["CustomRootCA:KeyChain:Store:Name"] = "ABCDEF1234567890ABCDEF1234567890ABCDEF12"
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        services.AddCustomRootCA(configuration);

        var serviceProvider = services.BuildServiceProvider();

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CARootOption>>();
        optionsMonitor.Should().NotBeNull();

        var caRootOption = optionsMonitor.Get("Pem");
        caRootOption.Should().NotBeNull();
        caRootOption.CaPem.Should().NotBeNullOrEmpty();
        caRootOption.CaPem.Should().Be(pem);
        caRootOption.CaFilePath.Should().BeNullOrEmpty();
        caRootOption.Store.Should().BeNull();

        caRootOption = optionsMonitor.Get("File");
        caRootOption.Should().NotBeNull();
        caRootOption.CaPem.Should().BeNullOrEmpty();
        caRootOption.CaFilePath.Should().NotBeNull();
        caRootOption.CaFilePath.Should().Be("/app/cert/ca.pem");
        caRootOption.Store.Should().BeNull();

        caRootOption = optionsMonitor.Get("KeyChain");
        caRootOption.Should().NotBeNull();
        caRootOption.CaPem.Should().BeNullOrEmpty();
        caRootOption.CaFilePath.Should().BeNullOrEmpty();
        caRootOption.Store.Should().NotBeNull();
        caRootOption.Store.FindType.Should().Be(X509FindType.FindByThumbprint);
        caRootOption.Store.Location.Should().Be(StoreLocation.CurrentUser);
        caRootOption.Store.StoreName.Should().Be(StoreName.Root);
        caRootOption.Store.Name.Should().Be("ABCDEF1234567890ABCDEF1234567890ABCDEF12");
    }

    [Fact]
    public void Custom_Root_CA_With_Multiple_Certificate_Should_Be_Registered()
    {
        var pem = GenerateRsaPublicKeyPem();

        var services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["CustomRootCA:Multiple:CaPem"] = pem,
                    ["CustomRootCA:Multiple:CaFilePath"] = "/app/cert/ca.pem",
                    ["CustomRootCA:Multiple:Store:Location"] = "CurrentUser",
                    ["CustomRootCA:Multiple:Store:StoreName"] = "Root",
                    ["CustomRootCA:Multiple:Store:FindType"] = "FindByThumbprint",
                    ["CustomRootCA:Multiple:Store:Name"] = "ABCDEF1234567890ABCDEF1234567890ABCDEF12"
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        var exception = Record.Exception(() => services.AddCustomRootCA(configuration));

        exception.Should().NotBeNull();
        exception.Should().BeOfType<ConfigurationException>();
        exception.Message.Should().Contain($"The certificate Multiple has more than one CA source defined.");

    }


    [Fact]
    public void Custom_Root_CA_With_No_Certificate_Defined_Should_Throw_ConfigurationException()
    {
        var services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["CustomRootCA:EmptyCert:CaPem"] = "",
                    ["CustomRootCA:EmptyCert:CaFilePath"] = ""
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        var exception = Record.Exception(() => services.AddCustomRootCA(configuration));

        exception.Should().NotBeNull();
        exception.Should().BeOfType<ConfigurationException>();
        exception.Message.Should().Contain("The certificate EmptyCert does not have a CA file or a CA PEM or a Store defined.");
    }

    [Fact]
    public void Read_Certificate_From_Pem_File_Should_Return_Certificate()
    {
       var pem = GenerateRsaPublicKeyPem();

        var services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["CustomRootCA:Pem:CaPem"] = pem,
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        services.AddCustomRootCA(configuration);

        var serviceProvider = services.BuildServiceProvider();

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CARootOption>>();
        optionsMonitor.Should().NotBeNull();

        var certificate = optionsMonitor.Get("Pem").GetCertificate(new X509CertificateLoader(null));

        certificate.Should().NotBeNull();
        certificate.Subject.Should().Be("CN=Arc4u Custom Root CA");

    }

    private static string GenerateRsaPublicKeyPem()
    {
        using var rsa = RSA.Create(2048);

        var certRequest = new CertificateRequest(
            "CN=Arc4u Custom Root CA",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var cert = certRequest.CreateSelfSigned(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddYears(1));

        return cert.ExportCertificatePem();
    }
#endif
}
