using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Arc4u.gRPC.Interceptors;

/// <summary>
/// The relative path interceptor prefixes the service name with a relative path.
/// When used with a proxy like Yarp and a path prefix is used to route the call to a specific service
/// the interceptor can be used.
/// </summary>
public abstract class AddSuffixPathInterceptor : Interceptor
{
    /// <summary>
    /// Creates the interceptor.
    /// </summary>
    /// <param name="relativePathUrl">The prefix put in front of the service name; trimmed.</param>
    protected AddSuffixPathInterceptor(string relativePathUrl)
    {
        _relativePathUrl = relativePathUrl.Trim();
    }

    readonly string _relativePathUrl;
    /// <inheritdoc/>
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        CreateContext(ref context);

        return continuation(request, context);
    }

    /// <inheritdoc/>
    public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        CreateContext(ref context);

        return continuation(request, context);
    }

    /// <inheritdoc/>
    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        CreateContext(ref context);

        return continuation(context);
    }

    /// <inheritdoc/>
    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        CreateContext(ref context);

        return continuation(request, context);
    }

    /// <inheritdoc/>
    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        CreateContext(ref context);

        return continuation(context);
    }

    private Method<TRequest, TResponse> GetMethod<TRequest, TResponse>(Method<TRequest, TResponse> method) => new Method<TRequest, TResponse>(method.Type, $"{_relativePathUrl}{method.ServiceName}", method.Name, method.RequestMarshaller, method.ResponseMarshaller);

    private void CreateContext<TRequest, TResponse>(ref ClientInterceptorContext<TRequest, TResponse> context)
                 where TRequest : class
                 where TResponse : class
    {
        context = new ClientInterceptorContext<TRequest, TResponse>(GetMethod(context.Method), context.Host, context.Options);
    }

}
