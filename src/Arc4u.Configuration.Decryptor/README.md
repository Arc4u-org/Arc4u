# Arc4u.Configuration.Decryptor

Keep secrets encrypted in configuration files and decrypt values that start with `Decrypt:` when the application starts, with an X.509 certificate or an AES key.

## Install

```bash
dotnet add package Arc4u.Configuration.Decryptor --prerelease
```

## Usage

```csharp
using Arc4u.Configuration.Decryptor;

var builder = WebApplication.CreateBuilder(args);

// Add it after the providers that hold the encrypted values.
builder.Configuration.AddCertificateDecryptorConfiguration();
```

The certificate is read from the `EncryptionCertificate` section, under `Store` (certificate store) or `File` (PEM files).

## Documentation

- Guide: [Decrypting secrets](https://arc4u-org.github.io/Arc4u/guides/configuration/decrypting-secrets.html)
- API reference: [Arc4u.Configuration.Decryptor](https://arc4u-org.github.io/Arc4u/api/Arc4u.Configuration.Decryptor.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
