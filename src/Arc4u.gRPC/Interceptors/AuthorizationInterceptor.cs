using System.Globalization;
using Arc4u.Diagnostics;
using Arc4u.Security.Principal;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Arc4u.gRPC.Interceptors;

/// <summary>
/// Server interceptor that prepares the Arc4u context of a call and hides unexpected server errors.
/// </summary>
/// <remarks>
/// For each unary, server-streaming and duplex call (client-streaming calls are not intercepted) it applies the <c>culture</c> request header (when present and a principal exists) to the current thread and to the principal profile.
/// Duplex calls on <c>/grpc.reflection</c> are left untouched.
/// An <see cref="RpcException"/> thrown by the service is logged and rethrown; any other exception is logged and replaced by an <c>Internal</c> status with a generic message.
/// </remarks>
public class AuthorizationInterceptor(
    ILogger<AuthorizationInterceptor> logger,
    IApplicationContext applicationContext) : Interceptor
{
    //const string appSettings = "AppSettings";

    /// <inheritdoc/>
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request,
        ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        SetCultureIfExist(context);

        try
        {
            return await continuation(request, context).ConfigureAwait(false);
        }
        catch (RpcException rcp)
        {
            logger.Technical().LogException(rcp);
            throw;
        }
        catch (Exception ex)
        {
            logger.Technical().LogException(ex);
            throw new RpcException(new Grpc.Core.Status(StatusCode.Internal, "An error occurs."));
        }
    }

    private void SetCultureIfExist(ServerCallContext context)
    {
        // Culture was injected?
        var cultureEntry = context.RequestHeaders.Get("culture");
        if (null != cultureEntry && !cultureEntry.IsBinary && null != applicationContext.Principal)
        {
            try
            {
                var culture = new CultureInfo(cultureEntry.Value);

                Threading.Culture.SetCulture(culture);
                applicationContext.Principal.Profile.CurrentCulture = culture;
            }
            catch (ArgumentNullException)
            {
            }
            catch (CultureNotFoundException)
            {
            }
        }
    }

    /// <inheritdoc/>
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request,
        IServerStreamWriter<TResponse> responseStream, ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        SetCultureIfExist(context);

        var targetType = continuation.Target!.GetType();

        try
        {
            await continuation(request, responseStream, context).ConfigureAwait(false);
        }
        catch (RpcException rcp)
        {
            logger.Technical().LogException(rcp);
            throw;
        }
        catch (Exception ex)
        {
            logger.Technical().LogException(ex);
            throw new RpcException(new Grpc.Core.Status(StatusCode.Internal, "An error occurs."));
        }
    }

    /// <inheritdoc/>
    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        if (!context.Method.StartsWith("/grpc.reflection", StringComparison.InvariantCultureIgnoreCase))
        {
            SetCultureIfExist(context);
        }

        try
        {
            await continuation(requestStream, responseStream, context).ConfigureAwait(false);
        }
        catch (RpcException rcp)
        {
            logger.Technical().LogException(rcp);
            throw;
        }
        catch (Exception ex)
        {
            logger.Technical().LogException(ex);
            throw new RpcException(new Grpc.Core.Status(StatusCode.Internal, "An error occurs."));
        }
    }
}
