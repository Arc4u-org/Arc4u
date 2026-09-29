using Arc4u.Dependency.Attribute;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Arc4u.gRPC.Interceptors;

/// <summary>
/// Corrected client error interceptor: the type in Arc4u has bugs for streaming!
/// </summary>
[Export]
public class ClientErrorInterceptor : Interceptor
{
    private static void HandleException(RpcException rpc)
    {
        //var error = rpc.GetDetail<ErrorInfo>();

        //if (null != error && rpc.Message.Equals("AppSettings", StringComparison.InvariantCultureIgnoreCase))
        //{
        //    var messages = JsonSerializer.Deserialize<string[]>(error.Reason) ?? [];
        //    throw new AppException(Result.Fail(messages));
        //}

        if (StatusCode.PermissionDenied == rpc.StatusCode)
        {
            throw new UnauthorizedAccessException(rpc?.Status.Detail ?? "Access is denied.");
        }

        throw rpc;
    }

    private static void HandleException(AggregateException ag)
    {
        if (ag?.InnerException is RpcException rpc)
        {
            HandleException(rpc);
        }

        throw new InvalidOperationException("Unknown", ag!);
    }

    #region UnaryCall

    /// <summary>
    /// Intercepts an asynchronous unary call and converts the <see cref="RpcException"/> raised while starting it:
    /// <c>PermissionDenied</c> becomes an <see cref="UnauthorizedAccessException"/>, any other status is rethrown unchanged.
    /// </summary>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="request">The request message.</param>
    /// <param name="context">The client call context.</param>
    /// <param name="continuation">The delegate starting the call.</param>
    /// <returns>The call returned by the continuation.</returns>
    /// <exception cref="UnauthorizedAccessException">The server answered <c>PermissionDenied</c>.</exception>
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        try
        {
            return continuation(request, context);
        }
        catch (RpcException rpc)
        {
            HandleException(rpc);
        }

        // never reached
        return default!;
    }

    #endregion

    #region ClientStreaming
    /// <summary>
    /// Intercepts an asynchronous server streaming call and converts the <see cref="RpcException"/> raised while starting it:
    /// <c>PermissionDenied</c> becomes an <see cref="UnauthorizedAccessException"/>, any other status is rethrown unchanged. An <see cref="AggregateException"/> without an inner <see cref="RpcException"/> becomes an <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="request">The request message.</param>
    /// <param name="context">The client call context.</param>
    /// <param name="continuation">The delegate starting the call.</param>
    /// <returns>The call returned by the continuation.</returns>
    /// <exception cref="UnauthorizedAccessException">The server answered <c>PermissionDenied</c>.</exception>
    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
                                    TRequest request,
                                    ClientInterceptorContext<TRequest, TResponse> context,
                                    AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        try
        {
            return continuation(request, context);
        }
        catch (RpcException rpc)
        {
            HandleException(rpc);
        }
        catch (AggregateException ag)
        {
            HandleException(ag);
        }
        // never reached
        return default!;
    }

    #endregion

    #region Duplex

    /// <summary>
    /// Intercepts an asynchronous duplex streaming call and converts the <see cref="RpcException"/> raised while starting it:
    /// <c>PermissionDenied</c> becomes an <see cref="UnauthorizedAccessException"/>, any other status is rethrown unchanged. An <see cref="AggregateException"/> without an inner <see cref="RpcException"/> becomes an <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="context">The client call context.</param>
    /// <param name="continuation">The delegate starting the call.</param>
    /// <returns>The call returned by the continuation.</returns>
    /// <exception cref="UnauthorizedAccessException">The server answered <c>PermissionDenied</c>.</exception>
    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
                                    ClientInterceptorContext<TRequest, TResponse> context,
                                    AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        try
        {
            return continuation(context);
        }
        catch (RpcException rpc)
        {
            HandleException(rpc);
        }
        catch (AggregateException ag)
        {
            HandleException(ag);
        }
        // never reached
        return default!;
    }

    #endregion

}

