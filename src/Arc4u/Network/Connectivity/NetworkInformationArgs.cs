namespace Arc4u.Network.Connectivity;

/// <summary>
/// Event data of the <see cref="INetworkInformation.StatusMonitoring"/> event.
/// </summary>
public class NetworkInformationArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkInformationArgs"/> class.
    /// </summary>
    /// <param name="newStatus">The new network status.</param>
    public NetworkInformationArgs(NetworkStatus newStatus)
    {
        NewStatus = newStatus;
    }

    /// <summary>
    /// Gets the new network status.
    /// </summary>
    public NetworkStatus NewStatus { get; }
}
