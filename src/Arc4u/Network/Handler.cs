namespace Arc4u.Network;

/// <summary>
/// Holds the hook (<see cref="OnCalling"/>) invoked by the Arc4u clients before a request is sent over the network.
/// </summary>
public class Handler
{
    /// <summary>
    /// This static propery is used to implement any code that should be called before doing a HttpClient request.
    /// The idea is to force a vpn connexion for sample.
    /// </summary>
    public static Action<Uri>? OnCalling { get; set; }
}
