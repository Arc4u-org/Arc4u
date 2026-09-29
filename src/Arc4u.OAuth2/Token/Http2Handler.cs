namespace Arc4u.OAuth2;

/// <summary>
/// Force the Http request to use Http 2.0
/// </summary>
public class Http2Handler : DelegatingHandler
{
    /// <summary>Initializes a new instance of the <see cref="Http2Handler"/> class with a default <see cref="HttpClientHandler"/> as inner handler.</summary>
    public Http2Handler() : base(new HttpClientHandler())
    {
    }

    /// <summary>Initializes a new instance of the <see cref="Http2Handler"/> class.</summary>
    /// <param name="handler">The inner handler.</param>
    public Http2Handler(DelegatingHandler handler) : base(handler)
    {
    }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Version = new Version("2.0");

        return base.SendAsync(request, cancellationToken);
    }
}
