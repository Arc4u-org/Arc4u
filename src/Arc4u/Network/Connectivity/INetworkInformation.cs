namespace Arc4u.Network.Connectivity;

/// <summary>
/// Gives the current status of the network connectivity and notifies when it changes.
/// </summary>
public interface INetworkInformation
{
    /// <summary>
    /// Gets the current network status.
    /// </summary>
    NetworkStatus Status { get; }

    /// <summary>
    /// Occurs when the network status changes.
    /// </summary>
    event EventHandler<NetworkInformationArgs> StatusMonitoring;
}
