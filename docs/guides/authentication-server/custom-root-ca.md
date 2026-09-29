---
description: "Trust a private certificate authority for outgoing HTTPS calls, including the identity provider metadata, without changing the container image."
---
# Custom root CA

In a cluster, services often use certificates issued by a private certificate authority, for example by
cert-manager, so that they can talk HTTP/2 and TLS 1.2 or 1.3 to each other. The nodes and the container images
do not trust that authority. You can add the root certificate to the image when you build it; if you prefer to
keep the image independent of the cluster, store the root certificate in a Kubernetes secret, mount it as a
volume, and tell your HTTP clients to trust it. Arc4u reads the root certificates from the configuration and
configures `HttpClient` instances to validate server certificates against them. For the place of these calls
in an Arc4u application, see [Concepts](../../concepts/index.md).

## Configuration

`AddCustomRootCA(configuration)` reads the `CustomRootCA` section (parameter `sectionName`). Each entry is a
named <xref:Arc4u.Security.CustomRootCA.CARootOption> with exactly one source:

```json
{
  "CustomRootCA": {
    "Cluster": { "CaFilePath": "/app/ca/ca.crt" }
  }
}
```

| Key | Type | Description |
|---|---|---|
| `CustomRootCA:<name>:CaFilePath` | string | Path of a PEM file holding the root certificate, for example a mounted secret. |
| `CustomRootCA:<name>:CaPem` | string | The PEM text of the root certificate. |
| `CustomRootCA:<name>:Store` | object | A certificate of the operating system store: `Name`, `FindType` (default `FindBySubjectName`), `Location` (default `LocalMachine`), `StoreName` (default `My`). |

An entry with no source or with more than one source throws `ConfigurationException` at startup. When the
section is missing, nothing is registered, so the feature can be enabled per environment.

## Code

```csharp
using Arc4u.OAuth2.Extensions;
using Arc4u.Security.Cryptography;
using Arc4u.Security.CustomRootCA;

builder.Services.AddSingleton<IX509CertificateLoader, X509CertificateLoader>();
builder.Services.AddCustomRootCA(builder.Configuration);

builder.Services.AddHttpClient("Orders", client => client.BaseAddress = new Uri("https://orders.my-namespace.svc"))
                .ConfigureLocalCaCertificate("Cluster");
```

`ConfigureLocalCaCertificate(name)` sets the primary handler of the named client. Without a name, it trusts all
the registered root certificates. It needs `IX509CertificateLoader` and the Arc4u logger
(`AddApplicationContext()`, see the [overview](index.md#code)).

For gRPC clients, `ConfigureLocalCaCertificateForGrpc` of `Arc4u.AspNetCore.gRpc` does the same with a
`SocketsHttpHandler`; see [gRPC and API versioning](../grpc-versioning/index.md).

When a server certificate fails the standard validation, the handler builds the chain again with the custom
certificates as the only trusted roots, without revocation check. A certificate that passes the standard
validation is accepted as usual.

> [!CAUTION]
> Known issue: when the standard validation fails, only the chain is checked. A certificate issued by your
> custom authority for another host name is accepted (the gRPC variant behaves the same way). Issue the certificates of your private authority only to
> services you trust.

## Trust the identity provider

The JWT bearer and OpenID Connect handlers download the metadata and the signing keys with their own
`Backchannel` HTTP client, which `ConfigureLocalCaCertificate` does not change. When the identity provider uses
a certificate of your private authority, give the handler a client configured with it (general ASP.NET Core
configuration, verified with `AddJwtAuthentication`):

```csharp
using Arc4u.OAuth2.Extensions;
using Arc4u.Security.Cryptography;
using Arc4u.Security.CustomRootCA;
using Microsoft.AspNetCore.Authentication.JwtBearer;

builder.Services.AddSingleton<IX509CertificateLoader, X509CertificateLoader>();
builder.Services.AddCustomRootCA(builder.Configuration);

builder.Services.AddHttpClient("IdentityProvider")
                .ConfigureLocalCaCertificate("Cluster");

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IHttpClientFactory>((options, factory) =>
                    options.Backchannel = factory.CreateClient("IdentityProvider"));
```

For OpenID Connect, configure `OpenIdConnectOptions` of the `OpenIdConnect` scheme the same way: the token
refresh uses its `Backchannel` too. Without this, the API answers 401 because the metadata cannot be downloaded.

## Kubernetes example

With cert-manager, the secret of a certificate contains `ca.crt`. Mount it and point `CaFilePath` to it
(**general guidance**, adapt the names to your cluster):

```yaml
spec:
  containers:
    - name: my-api
      volumeMounts:
        - name: root-ca
          mountPath: /app/ca
          readOnly: true
  volumes:
    - name: root-ca
      secret:
        secretName: my-api-tls
        items:
          - key: ca.crt
            path: ca.crt
```

## See also

- [Server authentication](index.md)
- [gRPC and API versioning](../grpc-versioning/index.md)
- <xref:Arc4u.Security.CustomRootCA.CustomRootCaExtensions> and <xref:Arc4u.OAuth2.Extensions.CustomRootCaExtension> in the API reference
