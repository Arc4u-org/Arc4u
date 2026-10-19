using System.Net;
using System.Security.Claims;
using Arc4u.Configuration;
using Arc4u.Dependency;
using Arc4u.Diagnostics;
using Arc4u.gRPC.Interceptors;
using Arc4u.OAuth2;
using Arc4u.OAuth2.AspNetCore;
using Arc4u.OAuth2.Token;
using Arc4u.Security.Principal;
using AwesomeAssertions;
using FluentResults;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using AspNetCoreJwtHttpHandler = Arc4u.OAuth2.Token.JwtHttpHandler<Arc4u.UnitTest.Security.TokenHandlersDegradeTests>;
using ClientJwtHttpHandler = Arc4u.OAuth2.Client.Authentication.Token.JwtHttpHandler<Arc4u.UnitTest.Security.TokenHandlersDegradeTests>;

namespace Arc4u.UnitTest.Security;

/// <summary>
/// The token handlers and the gRPC interceptor must send the request without a token instead of throwing (issue #230).
/// </summary>
[Trait("Category", "CI")]
public class TokenHandlersDegradeTests
{
    private const string FakeProviderName = "fake";

    private static readonly TokenInfo ValidToken = new("Bearer", "the-token", DateTime.UtcNow.AddHours(1));

    // A container without HTTP request and without an explicit scope: the scoped accessor throws when it is read.
    private static ServiceProvider BuildRootProvider(TokenInfo? token = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddILogger();

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        services.AddSingleton(httpContextAccessor.Object);
        services.AddSingleton<IScopedServiceProviderAccessor, ScopedServiceProviderAccessor>();

        var applicationContext = new ApplicationInstanceContext();
        applicationContext.SetPrincipal(new AppPrincipal(new Arc4u.Security.Principal.Authorization(), new ClaimsIdentity(Constants.BearerAuthenticationType), "S-1-0-0") { Profile = UserProfile.Empty });
        services.AddSingleton<IApplicationContext>(applicationContext);

        services.AddKeyedTransient<ITokenProvider, NullTokenProvider>(NullTokenProvider.ProviderName);

        var fakeProvider = new Mock<ITokenProvider>();
        fakeProvider.Setup(p => p.GetTokenAsync(It.IsAny<IKeyValueSettings?>(), It.IsAny<object?>()))
                    .ReturnsAsync(Result.Ok(token ?? ValidToken));
        services.AddKeyedSingleton(FakeProviderName, fakeProvider.Object);

        return services.BuildServiceProvider();
    }

    private static SimpleKeyValueSettings Settings(string? providerId, string? authenticationType = Constants.BearerAuthenticationType)
    {
        var values = new Dictionary<string, string>();
        if (providerId is not null)
        {
            values[TokenKeys.ProviderIdKey] = providerId;
        }
        if (authenticationType is not null)
        {
            values[TokenKeys.AuthenticationTypeKey] = authenticationType;
        }
        return new SimpleKeyValueSettings(values);
    }

    private static OAuth2Interceptor<TokenHandlersDegradeTests> CreateInterceptor(IServiceProvider serviceProvider, IKeyValueSettings settings)
        => new(serviceProvider, serviceProvider.GetRequiredService<ILogger<TokenHandlersDegradeTests>>(), settings);

    private static string? CallBlocking(Interceptor sut, Metadata? headers = null)
    {
        var context = new ClientInterceptorContext<string, string>(new Method<string, string>(MethodType.Unary, "service", "method", Marshallers.StringMarshaller, Marshallers.StringMarshaller), "host", new CallOptions(headers ?? []));
        Metadata? sentHeaders = null;

        sut.BlockingUnaryCall("request", context, (r, c) =>
        {
            sentHeaders = c.Options.Headers;
            return "response";
        });

        return sentHeaders?.GetValue("authorization");
    }

    [Fact]
    public void Interceptor_Outside_Of_An_Http_Request_Should_Use_The_Root_Provider()
    {
        using var serviceProvider = BuildRootProvider();

        var authorization = CallBlocking(CreateInterceptor(serviceProvider, Settings(FakeProviderName)));

        authorization.Should().Be($"Bearer {ValidToken.Token}");
    }

    [Fact]
    public void Interceptor_With_The_Null_Token_Provider_Should_Send_No_Token()
    {
        using var serviceProvider = BuildRootProvider();

        var authorization = CallBlocking(CreateInterceptor(serviceProvider, Settings(NullTokenProvider.ProviderName)));

        authorization.Should().BeNull();
    }

    [Fact]
    public void Interceptor_Without_ProviderId_Should_Send_No_Token()
    {
        using var serviceProvider = BuildRootProvider();

        var authorization = CallBlocking(CreateInterceptor(serviceProvider, Settings(providerId: null)));

        authorization.Should().BeNull();
    }

    [Fact]
    public void Interceptor_With_An_Authorization_Header_And_No_AuthenticationType_Should_Keep_The_Header()
    {
        using var serviceProvider = BuildRootProvider();

        var authorization = CallBlocking(CreateInterceptor(serviceProvider, Settings(FakeProviderName, authenticationType: null)), new Metadata { { "authorization", "Basic existing" } });

        authorization.Should().Be("Basic existing");
    }

    [Fact]
    public async Task Interceptor_AsyncUnaryCall_Should_Add_The_Token()
    {
        using var serviceProvider = BuildRootProvider();
        var sut = CreateInterceptor(serviceProvider, Settings(FakeProviderName));
        var context = new ClientInterceptorContext<string, string>(new Method<string, string>(MethodType.Unary, "service", "method", Marshallers.StringMarshaller, Marshallers.StringMarshaller), "host", new CallOptions());
        Metadata? sentHeaders = null;

        using var call = sut.AsyncUnaryCall("request", context, (r, c) =>
        {
            sentHeaders = c.Options.Headers;
            return new AsyncUnaryCall<string>(Task.FromResult("response"), Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => [], () => { });
        });

        (await call.ResponseAsync).Should().Be("response");
        call.GetStatus().Should().Be(Status.DefaultSuccess);
        sentHeaders!.GetValue("authorization").Should().Be($"Bearer {ValidToken.Token}");
    }

    private static async Task<HttpRequestMessage> SendAsync(DelegatingHandler handler)
    {
        HttpRequestMessage? sent = null;
        handler.InnerHandler = new StubHandler(request =>
        {
            sent = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var invoker = new HttpMessageInvoker(handler);
        using var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://localhost/api"), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return sent!;
    }

    [Fact]
    public async Task AspNetCore_Handler_With_The_Null_Token_Provider_Should_Send_No_Token()
    {
        using var serviceProvider = BuildRootProvider();
        var sut = new AspNetCoreJwtHttpHandler(serviceProvider, serviceProvider.GetRequiredService<ILogger<TokenHandlersDegradeTests>>(), Settings(NullTokenProvider.ProviderName));

        var request = await SendAsync(sut);

        request.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task AspNetCore_Handler_Without_ProviderId_Should_Send_No_Token()
    {
        using var serviceProvider = BuildRootProvider();
        var sut = new AspNetCoreJwtHttpHandler(serviceProvider, serviceProvider.GetRequiredService<ILogger<TokenHandlersDegradeTests>>(), Settings(providerId: null));

        var request = await SendAsync(sut);

        request.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task Client_Handler_With_The_Null_Token_Provider_Should_Send_No_Token()
    {
        using var serviceProvider = BuildRootProvider();
        var sut = new ClientJwtHttpHandler(serviceProvider, serviceProvider.GetRequiredService<ILogger<TokenHandlersDegradeTests>>(), Settings(NullTokenProvider.ProviderName));

        var request = await SendAsync(sut);

        request.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task Client_Handler_With_An_Unknown_Provider_Should_Send_No_Token()
    {
        using var serviceProvider = BuildRootProvider();
        var sut = new ClientJwtHttpHandler(serviceProvider, serviceProvider.GetRequiredService<ILogger<TokenHandlersDegradeTests>>(), Settings("unknown"));

        var request = await SendAsync(sut);

        request.Headers.Authorization.Should().BeNull();
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(send(request));
    }
}
