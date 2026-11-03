using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Arc4u.OAuth2.Token;
using Arc4u.Serializer;
using AutoFixture;
using AwesomeAssertions;
using AutoFixture.AutoMoq;
using Xunit;
using Xunit.Abstractions;

namespace Arc4u.UnitTest.Serialization;

[Trait("Category", "CI")]
public class CompressionAndSpeedTests
{
    private static string[]? _bearerTokens;

    private static readonly JwtSecurityToken _jwt = new("issuer", "audience", [new Claim("key", "value")],
        DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddMinutes(-10));

    private static readonly TokenInfo _tokenInfo = new("Bearer", _jwt.EncodedPayload, DateTime.UtcNow);
    private readonly Fixture _fixture;
    private readonly ITestOutputHelper _output;

    public CompressionAndSpeedTests(ITestOutputHelper output)
    {
        _output = output;
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    /// <summary>
    ///     A normal and a "zuppafat" bearer token
    /// </summary>
    private string[] BearerTokens()
    {
        if (null == _bearerTokens)
        {
            List<Claim> claims = [];
            for (var i = 0; i < 150; i++)
            {
                claims.Add(new Claim(_fixture.Create<string>(), _fixture.Create<string>()));
            }

            JwtSecurityToken jwt = new("issuer", "audience", claims, DateTime.UtcNow.AddHours(-1),
                DateTime.UtcNow.AddHours(1));

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            _bearerTokens = [accessToken, "Bearer " + accessToken];
        }

        return _bearerTokens;
    }

    private Measurement Measure(IObjectSerialization objectSerialization, string method)
    {
        var sw = Stopwatch.StartNew();
        var size = 0;
        for (var iterations = 0; iterations < 1000; ++iterations)
        {
            size = 0;

            foreach (var bearerToken in BearerTokens())
            {
                var data = objectSerialization.Serialize(bearerToken);
                size += data.Length;
                // this measures time only, it's not a test of correctness in this context.
                objectSerialization.Deserialize<string>(data);
            }

            {
                var data = objectSerialization.Serialize(_tokenInfo);
                size += data.Length;
                objectSerialization.Deserialize<TokenInfo>(data);
            }

            // add other relevant cases here.
        }

        sw.Stop();
        return new Measurement { Method = method, TimeSpan = sw.Elapsed, Size = size };
    }

    private void ShowMeasurements(IEnumerable<Measurement> measurements, string title)
    {
        _output.WriteLine(title);
        _output.WriteLine($"{"Method",-20} {"Time",-20} {"Size",-10}");
        foreach (var measurement in measurements)
        {
            _output.WriteLine($"{measurement.Method,-20} {measurement.TimeSpan,-20} {measurement.Size,-10}");
        }

        _output.WriteLine("");
    }

    /// <summary>
    ///     Measure response
    /// </summary>
    [Fact]
    public void CheckRuntimeTypeModelConcurrencyAsync()
    {
        // arrange
        var jsonSerialization = new JsonSerialization();
        var jsonBrotliSerialization = new JsonBrotliSerialization();
        var jsonDeflateSerialization = new JsonDeflateSerialization();
        var jsonGZipSerialization = new JsonGZipSerialization();
        var jsonZipSerialization = new JsonZipSerialization();

        // act
        var list = new List<Measurement>
        {
            Measure(jsonSerialization, "Json"),
            Measure(jsonBrotliSerialization, "JSon+Brotli"),
            Measure(jsonDeflateSerialization, "Json+Deflate"),
            Measure(jsonGZipSerialization, "Json+GZip"),
            Measure(jsonZipSerialization, "Json+Zip")
        };

        // sort by fastest compression
        list.Sort((item1, item2) => item1.TimeSpan.CompareTo(item2.TimeSpan));
        ShowMeasurements(list, "Results ordered by ascending serialization speed");
        // sort by smallest size
        list.Sort((item1, item2) => item1.Size.CompareTo(item2.Size));
        ShowMeasurements(list, "Results ordered by ascending serialization size");

        // assert
        list.Should().AllSatisfy(m => m.Size.Should().BePositive());
    }

    private sealed class Measurement
    {
        public string Method { get; set; } = default!;
        public TimeSpan TimeSpan { get; set; }
        public int Size { get; set; }
    }
}
