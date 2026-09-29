---
description: "Keep secrets encrypted in appsettings.json and decrypt them at startup with an X.509 certificate or an AES key."
---
# Decrypting secrets

Connection strings, client secrets and passwords do not belong in a repository in clear text, and
.NET user secrets stay on one developer machine. The `Arc4u.Configuration.Decryptor` package lets you commit
the values **encrypted** and decrypts them when the application builds its configuration. A new
developer clones the repository and starts the application; only the certificate must be
available on the machine that runs it. See [Configuration](index.md) for the other parts of
Arc4u configuration, and the [concepts](../../concepts/index.md) for how Arc4u fits together.

## How it works

A value is encrypted when it starts with a prefix, `Decrypt:` by default. The decryptor is an
`IConfigurationProvider` that you add **after** the providers holding the encrypted values. When the
configuration is built, it reads every provider registered before it, decrypts each value that
starts with the prefix and publishes the clear text under the same key. Because .NET lets the last
provider win, the application sees the decrypted value.

```mermaid
flowchart LR
    A["appsettings.json<br/>Decrypt:Zm9v..."] --> D
    B["Environment variables"] --> D
    C["EncryptionCertificate section"] --> D
    D["Certificate decryptor"] --> F["IConfiguration<br/>clear text"]
```

The decryptor finds its own settings (which certificate to use) in the same providers, in a section
named `EncryptionCertificate`. If that section does not exist, the decryptor does nothing and the
values keep their `Decrypt:` prefix.

## Set it up

The steps below use a PEM file pair and the built-in `File` option. Section
[Configuration](#configuration) shows the Windows certificate store and the other variants.

### Step 1: Create a certificate

Create a self-signed RSA certificate and its private key. Both files must be unencrypted PEM: the
key must not be protected by a passphrase.

```bash
openssl req -x509 -newkey rsa:2048 -nodes \
  -keyout key.pem \
  -out cert.pem \
  -days 365 \
  -subj "/CN=Arc4u Config Secrets"
```

The certificate must have an RSA key: the encryption uses the RSA public key, so an ECDSA
certificate cannot be used. To check that a certificate and a key belong together, both commands
print the same public key:

```bash
openssl x509 -in cert.pem -noout -pubkey
openssl pkey -in key.pem -pubout
```

`cert.pem` (`-----BEGIN CERTIFICATE-----`) is public: you can share it with everybody who must
encrypt values. `key.pem` (`-----BEGIN PRIVATE KEY-----`) is the secret: it goes only to the machines
that run the application. See [Security notes](#security-notes).

### Step 2: Encrypt a value

Arc4u has no command-line tool for encryption. The encryption method is
<xref:Arc4u.Security.Cryptography.Certificate.Encrypt*>,
an extension method on `X509Certificate2` in the `Arc4u` package. It needs only the **public**
certificate. A small console project that references `Arc4u` does the job:

```bash
dotnet new console -o EncryptSecret
cd EncryptSecret
dotnet add package Arc4u --prerelease
```

```csharp
// Program.cs
using System.Security.Cryptography.X509Certificates;
using Arc4u.Security.Cryptography;
using CertificateLoader = System.Security.Cryptography.X509Certificates.X509CertificateLoader;

// dotnet run -- cert.pem "value to protect"
using var certificate = CertificateLoader.LoadCertificateFromFile(args[0]);
Console.WriteLine($"Decrypt:{certificate.Encrypt(args[1])}");
```

The alias avoids a name clash between the .NET `X509CertificateLoader` and the Arc4u class with the
same name. Run it for each secret and copy the output, prefix included:

```bash
dotnet run -- ../cert.pem "Server=db;Database=Orders;Password=s3cret"
```

```text
Decrypt:C6PBLlEuyKB3aTnC77sRYvEmgXvoKqHsKlR7kjoYVppiQXfDTWj5r0Wm...
```

The output is Base64 text and changes on every run (the encryption is randomized), so a different
string for the same value is normal. A value shorter than 191 bytes is trimmed of leading and trailing whitespace and encrypted directly with the
certificate's RSA public key (RSA-OAEP with SHA-256, for a 2048-bit key). A longer value is
encrypted with a random AES key, and the output has the form `<key>.<iv>.<data>` where the AES key
and IV are themselves encrypted with the certificate. Both forms decrypt the same way.

### Step 3: Put the encrypted value in the configuration

```json
{
  "ConnectionStrings": {
    "Orders": "Decrypt:C6PBLlEuyKB3aTnC77sRYvEmgXvoKqHsKlR7kjoYVppiQXfDTWj5r0Wm..."
  },
  "EncryptionCertificate": {
    "File": {
      "Cert": "certs/cert.pem",
      "Key": "certs/key.pem"
    }
  }
}
```

The paths are relative to the working directory of the process, or absolute. The example above keeps
the encrypted value and the certificate location in the same file; you can put the
`EncryptionCertificate` section in any provider, for example in an environment-specific file or in
environment variables (`EncryptionCertificate__File__Cert`).

### Step 4: Register the decryptor

```bash
dotnet add package Arc4u.Configuration.Decryptor --prerelease
```

```csharp
// Program.cs
using Arc4u.Configuration.Decryptor;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddCertificateDecryptorConfiguration();

var app = builder.Build();
// app.Configuration["ConnectionStrings:Orders"] is now "Server=db;Database=Orders;Password=s3cret"
app.Run();
```

`AddCertificateDecryptorConfiguration()` must come after the sources that contain the encrypted values and the
`EncryptionCertificate` section. `WebApplication.CreateBuilder` already added `appsettings.json`,
`appsettings.{Environment}.json`, user secrets, environment variables and the command line, so
calling it right after works for all of them. Sources you add **after** the decryptor are not
decrypted, and they override the decrypted value if they use the same key.

## Configuration

### EncryptionCertificate section

`AddCertificateDecryptorConfiguration()` reads the `EncryptionCertificate` section and binds it to
<xref:Arc4u.Security.CertificateStoreOrFileInfo>. The certificate is under exactly one of two
child sections, `Store` or `File`. If both are present, `Store` is used.

| Key | Type | Default | Description |
|---|---|---|---|
| `EncryptionCertificate:Store:Name` | `string` | none (required) | The value to search for in the certificate store, interpreted according to `FindType`. |
| `EncryptionCertificate:Store:FindType` | `X509FindType` | `FindBySubjectName` | How `Name` is matched: `FindBySubjectName`, `FindByThumbprint`, `FindBySerialNumber`, and so on. |
| `EncryptionCertificate:Store:Location` | `StoreLocation` | `LocalMachine` | `LocalMachine` or `CurrentUser`. |
| `EncryptionCertificate:Store:StoreName` | `StoreName` | `My` | The store folder. `My` is the Personal store. |
| `EncryptionCertificate:File:Cert` | `string` | none (required) | Path of the PEM file with the certificate. |
| `EncryptionCertificate:File:Key` | `string` | none (required) | Path of the PEM file with the unencrypted private key. |

Enumeration values are written by name, as in the example below. The store variant looks like this:

```json
{
  "EncryptionCertificate": {
    "Store": {
      "Name": "<certificate-thumbprint>",
      "FindType": "FindByThumbprint",
      "Location": "CurrentUser"
    }
  }
}
```

With the store variant, the first certificate that matches is used, and it must have a private key that the
process may read. The `File` variant loads the certificate and key with
`X509Certificate2.CreateFromPemFile`. It works on every operating system and does not need a
certificate store, which suits containers.

> [!NOTE]
> Arc4u 6.0.11 to 6.0.13 accepted a flat `EncryptionCertificate:Name`. Arc4u 6.0.14 moved it under
> `CertificateStore` (next to `File`), and the child was renamed `Store` in the 8.x line, which
> Arc4u 9 keeps. Only `Store` and `File` are read now. See
> [Troubleshooting](#no-certificate-information-found-in-the-configuration).

### Options in code

`AddCertificateDecryptorConfiguration` has an overload that takes an
`Action<`<xref:Arc4u.Configuration.Decryptor.SecretCertificateOptions>`>`.

| Property | Type | Default | Description |
|---|---|---|---|
| `Prefix` | `string` | `Decrypt:` | Values starting with this text are decrypted. |
| `SecretSectionName` | `string` | `EncryptionCertificate` | Path of the section that holds `Store` or `File`. |
| `Certificate` | `X509Certificate2?` | `null` | A certificate you already loaded. When set, the section is not read. It needs a private key. |
| `CertificateLoader` | `IX509CertificateLoader?` | Arc4u `X509CertificateLoader` | The service that turns the section into a certificate. Not used when `Certificate` is set. |

## Common scenarios

### Use a certificate in the Windows certificate store

Install the certificate with its private key in the machine store, give the account that runs the
application read access to the key, and reference it by thumbprint or subject name in the `Store`
section as shown above. Because the default `Location` is `LocalMachine` and `StoreName` is `My`,
the minimal form is:

```json
{
  "EncryptionCertificate": {
    "Store": {
      "Name": "Arc4u Config Secrets"
    }
  }
}
```

That value is matched with `FindBySubjectName`. It is a partial, case-insensitive match on the subject name, so use the
thumbprint when several certificates share a name.

### Use one certificate per environment

Keep the encrypted values in `appsettings.json` and the certificate reference in
`appsettings.{Environment}.json` (or in environment variables). Encrypt the values for each
environment with that environment's certificate, and put them in the environment-specific file.

### Run in Kubernetes

Mount the certificate and its key from a Kubernetes secret as files, and point the `File` section
at them, using environment variables so that the image is the same in every environment:

```yaml
env:
  - name: EncryptionCertificate__File__Cert
    value: /certs/tls.crt
  - name: EncryptionCertificate__File__Key
    value: /certs/tls.key
```

`tls.crt` and `tls.key` are the file names of a `kubernetes.io/tls` secret when it is mounted as a volume. Both must be PEM,
and the key must be unencrypted (a PKCS#8 `PRIVATE KEY` or `RSA PRIVATE KEY` block).
To convert a key, use `openssl pkcs8 -topk8 -nocrypt -in old.key -out key.pem`.

### Load the certificate yourself

Set `Certificate` when the certificate comes from somewhere the built-in options do not cover, such
as a secret manager, a PFX in a secret, or a certificate generated in a test:

```csharp
// Program.cs
using System.Security.Cryptography.X509Certificates;
using Arc4u.Configuration.Decryptor;

var builder = WebApplication.CreateBuilder(args);

var certificate = X509CertificateLoader.LoadPkcs12FromFile("secrets.pfx", "<pfx-password>");
builder.Configuration.AddCertificateDecryptorConfiguration(options => options.Certificate = certificate);

var app = builder.Build();
app.Run();
```

Arc4u does not read the public and private key from configuration values: the certificate is
found through the `EncryptionCertificate` section, or you provide it as shown above.

### Change the prefix or the section

```csharp
// Program.cs
using Arc4u.Configuration.Decryptor;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddCertificateDecryptorConfiguration(options =>
{
    options.Prefix = "Secret:";
    options.SecretSectionName = "Security:Certificate";
});

var app = builder.Build();
app.Run();
```

Values that start with `Secret:` are now decrypted, and the certificate is read from
`Security:Certificate:File:Cert` or `Security:Certificate:Store:Name`, and so on.

### Encrypt with an AES key instead of a certificate

`AddRijndaelDecryptorConfiguration()` decrypts with an AES key and IV that you put in the
configuration. It uses the same prefix mechanism, so values start with `Decrypt:`. The key and IV are
Base64 strings in the `EncryptionRijndael` section (<xref:Arc4u.Security.CypherCodecConfig>):

```json
{
  "EncryptionRijndael": {
    "Key": "<base64-key>",
    "IV": "<base64-iv>"
  }
}
```

```csharp
// Program.cs
using Arc4u.Configuration.Decryptor;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddRijndaelDecryptorConfiguration();

var app = builder.Build();
app.Run();
```

<xref:Arc4u.Configuration.Decryptor.SecretRijndaelOptions> has the same `Prefix` and
`SecretSectionName` properties (default `EncryptionRijndael`), and a `RijnDael` property to pass the key
and IV in code. To generate a key and encrypt a value, use <xref:Arc4u.Security.Cryptography.CypherCodec>:

```csharp
using Arc4u.Security.Cryptography;

var key = CypherCodec.GenerateKeyAndIV(out var iv);   // both Base64 strings
var cypher = CypherCodec.EncodeClearText("s3cret", Convert.FromBase64String(key), Convert.FromBase64String(iv));
Console.WriteLine($"Decrypt:{cypher}");
```

> [!CAUTION]
> The AES key sits in the configuration, next to the values it protects. This only hides values from
> someone who reads the file but not the rest of the configuration. Prefer the certificate
> decryptor, where the private key is kept apart from the configuration.

### Rotate the certificate

The decryptor uses one certificate at a time and cannot try an old one as a fallback. To rotate:

1. Create the new certificate.
2. Encrypt every secret again with the new public certificate (Step 2).
3. Deploy the new encrypted values together with the new private key and a matching
   `EncryptionCertificate` section.
4. Delete the old private key once no deployment uses it.

## Extensibility points

| Service | Default implementation | Replace it to |
|---|---|---|
| <xref:Arc4u.Security.Cryptography.IX509CertificateLoader> | <xref:Arc4u.Security.Cryptography.X509CertificateLoader> | Find the certificate somewhere else. Set it as `SecretCertificateOptions.CertificateLoader`. |

The configuration providers are built before dependency injection exists, so the loader is not
resolved from the service collection: pass an instance in the options.

```csharp
// Program.cs
using System.Security.Cryptography.X509Certificates;
using Arc4u.Configuration.Decryptor;
using Arc4u.Security;
using Arc4u.Security.Cryptography;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddCertificateDecryptorConfiguration(options => options.CertificateLoader = new MyCertificateLoader());

var app = builder.Build();
app.Run();

internal sealed class MyCertificateLoader : IX509CertificateLoader
{
    public X509Certificate2 FindCertificate(CertificateInfo certificateInfo) => throw new NotSupportedException();

    public X509Certificate2 FindCertificate(CertificateFilePathInfo? certificateInfo) => throw new NotSupportedException();

    public X509Certificate2? FindCertificate(IConfiguration configuration, string sectionName)
    {
        // Read what you need from the section and return the certificate, or null to skip decryption.
        return null;
    }
}
```

The decryptor calls only the `FindCertificate(IConfiguration, string)` overload, with the temporary
configuration built from the earlier providers and `SecretSectionName`. Returning `null` skips decryption.

## Security notes

> [!CAUTION]
> Never commit `key.pem`, a PFX or any private key. Commit only the encrypted values and, if you
> want, the public `cert.pem`.

- **Who can do what.** Anyone with the public certificate can produce an encrypted value. Only the
  holder of the private key can read it. Keep the private key out of the repository, out of the
  container image and out of build logs.
- **Where the private key lives.** In production, use the certificate store with an access control
  list on the private key, or a Kubernetes secret mounted as a read-only file. A PEM private key on
  a file system is readable by every process that can read the file.
- **Clear text in memory.** Once decrypted, a value is an ordinary configuration string. It shows up
  in a configuration dump, in a debugger and in a diagnostic endpoint that prints
  `IConfiguration`. Do not log the configuration.
- **One key per environment.** Use a different certificate per environment so that a leak in a
  test environment does not expose production secrets. A secret is only as private as the private key that decrypts it.
- **Start-up fails on purpose, mostly.** The decryptor does not catch exceptions: a missing certificate
  or a value that cannot be decrypted with an RSA key stops the application at startup, so the error is
  visible. The exception is an ECDSA certificate, which does not fail (see
  [Troubleshooting](#values-are-empty-with-an-ecdsa-certificate)).
- **Not a secret manager.** The encrypted values are public data. Rotating or revoking a secret means
  re-encrypting and redeploying. For secrets that change often, use a dedicated secret store and a
  configuration provider for it.

## Troubleshooting

### The value still starts with `Decrypt:`

Check, in this order:

1. `AddCertificateDecryptorConfiguration()` is called after the provider that holds the value.
2. There is an `EncryptionCertificate` section (or the section you set as `SecretSectionName`). Without
   it the decryptor skips every value.
3. The prefix matches, including the colon and the case. A custom `Prefix` replaces `Decrypt:`, it does not add to it.
4. A provider added after the decryptor does not overwrite the key with the encrypted text.

### No certificate information found in the configuration

The application stops with an `InvalidOperationException`. The section exists but has neither a
`Store` nor a `File` child. This happens with the flat layout of Arc4u 6.0.11 to 6.0.13
(`EncryptionCertificate:Name`) and with the `CertificateStore` child of 6.0.14
(`EncryptionCertificate:CertificateStore:Name`). Rename it to `Store`, or move the settings under
`Store` or `File`.

### Certificate name cannot be null

This `InvalidOperationException` means the `Store` section has no `Name`.

### No certificate found for the given criteria

This `KeyNotFoundException` means no certificate in the configured store matches. Check `Name`, `FindType`, `Location` (the default is
`LocalMachine`, not `CurrentUser`) and `StoreName`. On Linux and macOS, use the `File` variant.

### Public key file doesn't exist

The `FileNotFoundException` message is "Public key file doesn't exist." or "Private key file doesn't exist.". The `Cert` or `Key` path is wrong. Relative paths are resolved from the working directory of the
process, which is not always the folder of the application.

### CryptographicException at startup

The value was encrypted with another certificate, or the certificate has no private key
(`The certificate ... has no private key!`). The exact type and message depend on the operating
system. Encrypt the value again with the public certificate that matches `key.pem`.

### Values are empty with an ECDSA certificate

Encryption and decryption use the RSA key of the certificate. With an ECDSA certificate nothing
throws: `Encrypt` returns an empty string for short values (or `..<data>` with empty key and IV parts
for long ones), and decrypting with an ECDSA pair gives an empty value. If a decrypted value is
empty, check that the certificate has an RSA key (`openssl x509 -in cert.pem -noout -text` shows
`Public Key Algorithm: rsaEncryption`).

### The decryptor reads my earlier sources a second time

To find the values to decrypt, the decryptor builds a temporary configuration from all the sources
registered before it. Those sources are therefore loaded twice.

> [!WARNING]
> Known issue: a source that cannot be read twice fails. `AddJsonStream` before the decryptor throws
> "Stream was not readable" (the stream was consumed by the first build), and a remote provider such as
> Azure Key Vault loads twice, doubling its calls. Files, environment variables and command-line
> sources are not affected. Prefer file-based sources before the decryptor, and add stream or remote
> sources after it (their values are then not decrypted).

### The input is not a valid Base-64 string

This `FormatException` means the text after the prefix is not the output of `Encrypt`. Copy the whole string, without line
breaks or quotes.

## See also

- [Configuration](index.md)
- [Configuration store](configuration-store.md)
- <xref:Arc4u.Configuration.Decryptor> in the API reference
