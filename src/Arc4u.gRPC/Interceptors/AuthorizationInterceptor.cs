using System.Diagnostics;
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
/// For each call it applies the <c>culture</c> request header (when present and a principal exists) to the current thread and to the principal profile,
/// and sets <see cref="IApplicationContext.ActivityID"/> to the current activity id (or a new GUID). Duplex calls on <c>/grpc.reflection</c> are left untouched.
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
        SetActivityIDIfExist(context);

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
        // Culture and ActivityID was injected?
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

    private void SetActivityIDIfExist(ServerCallContext _)
    {
        applicationContext.ActivityID = Activity.Current?.Id ?? Guid.NewGuid().ToString();
    }

    /// <inheritdoc/>
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request,
        IServerStreamWriter<TResponse> responseStream, ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        SetCultureIfExist(context);
        SetActivityIDIfExist(context);

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
            SetActivityIDIfExist(context);
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
