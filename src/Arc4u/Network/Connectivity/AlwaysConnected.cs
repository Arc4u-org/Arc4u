using Arc4u.Dependency.Attribute;

namespace Arc4u.Network.Connectivity;

/// <summary>
/// An <see cref="INetworkInformation"/> that always reports a connected network (<see cref="NetworkStatus.Ethernet"/> and <see cref="NetworkStatus.Internet"/>).
/// It suits applications that do not monitor the connectivity.
/// </summary>
[Export(typeof(INetworkInformation))]
public class AlwaysConnected : INetworkInformation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AlwaysConnected"/> class.
    /// </summary>
    public AlwaysConnected()
    {
        // Suppress a warning (in fact will be actually not usefull)
        var handler = StatusMonitoring;
        handler?.Invoke(this, new NetworkInformationArgs(Status));
    }

    /// <inheritdoc/>
    public NetworkStatus Status => NetworkStatus.Ethernet | NetworkStatus.Internet;

    /// <summary>
    /// Occurs when the network status changes. This implementation never raises the event since the status never changes.
    /// </summary>
    public event EventHandler<NetworkInformationArgs>? StatusMonitoring;
}
