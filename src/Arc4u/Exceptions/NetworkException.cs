using System.Globalization;
using System.Text;
using Arc4u.Network.Connectivity;

namespace Arc4u.Exceptions;

/// <summary>
/// The exception thrown when an operation fails because of the state of the network connectivity.
/// </summary>
public class NetworkException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkException"/> class.
    /// </summary>
    /// <param name="networkStatus">The status of the network connectivity when the error occurred.</param>
    public NetworkException(NetworkStatus networkStatus) : base()
    {
        NetworkStatus = networkStatus;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkException"/> class with a message.
    /// </summary>
    /// <param name="networkStatus">The status of the network connectivity when the error occurred.</param>
    /// <param name="message">The message that describes the error.</param>
    public NetworkException(NetworkStatus networkStatus, string message) : base(message)
    {
        NetworkStatus = networkStatus;
    }

    /// <summary>
    /// Gets or sets the status of the network connectivity when the error occurred.
    /// </summary>
    public NetworkStatus NetworkStatus { get; set; }

    /// <summary>
    /// Returns a string that starts with the network status followed by the standard exception description.
    /// </summary>
    /// <returns>The string representation of the exception.</returns>
    public override string ToString()
    {
        var value = new StringBuilder();
        value.AppendLine(string.Format(CultureInfo.InvariantCulture, "Network connectivity is equal to {0}.", NetworkStatus));
        value.AppendLine(base.ToString());

        return value.ToString();
    }

}
