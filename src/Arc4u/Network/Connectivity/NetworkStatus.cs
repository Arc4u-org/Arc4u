namespace Arc4u.Network.Connectivity;

/// <summary>
/// The status of the network connectivity. The values can be combined (for example <see cref="Wifi"/> and <see cref="Internet"/>).
/// </summary>
[Flags]
public enum NetworkStatus
{
    /// <summary>
    /// There is no network connectivity.
    /// </summary>
    None = 1,

    /// <summary>
    /// The local network is reachable.
    /// </summary>
    Local = 2,

    /// <summary>
    /// The internet is reachable.
    /// </summary>
    Internet = 4,

    /// <summary>
    /// The device is connected through a cellular network.
    /// </summary>
    Cellular = 8,

    /// <summary>
    /// The device is connected through Wi-Fi.
    /// </summary>
    Wifi = 16,

    /// <summary>
    /// The device is connected through Ethernet.
    /// </summary>
    Ethernet = 32,

    /// <summary>
    /// The device is connected through Bluetooth.
    /// </summary>
    Bluetooth = 64,
}
