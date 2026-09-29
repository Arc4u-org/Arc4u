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
    /// 
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <param name="continuation"></param>
    /// <returns></returns>
    /// <exception cref="UnauthorizedAccessException"></exception>
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
    /// 
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <param name="continuation"></param>
    /// <returns></returns>
    /// <exception cref="UnauthorizedAccessException"></exception>
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
    /// 
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="context"></param>
    /// <param name="continuation"></param>
    /// <returns></returns>
    /// <exception cref="UnauthorizedAccessException"></exception>
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

