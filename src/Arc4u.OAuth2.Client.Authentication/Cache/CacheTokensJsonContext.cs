using System.Text.Json.Serialization;
using Arc4u.IdentityModel.Claims;
using Arc4u.OAuth2.Token;

namespace Arc4u.OAuth2.Client.Authentication.Cache;

/// <summary>
/// The source-generated JSON metadata of the values that Arc4u stores in the <see cref="CacheTokens"/>, so that they are serialized without reflection.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(TokenRefreshInfo))]
[JsonSerializable(typeof(TokenInfo))]
[JsonSerializable(typeof(List<ClaimDto>))]
[JsonSerializable(typeof(string))]
internal sealed partial class CacheTokensJsonContext : JsonSerializerContext
{
}
