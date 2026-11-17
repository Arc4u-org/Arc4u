using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;

namespace Arc4u.OAuth2.TicketStore;

/// <summary>The Data Protection settings shared by <see cref="CacheTicketStore"/> and <see cref="FileTicketStore"/>.</summary>
public static class TicketStoreDataProtection
{
    /// <summary>
    /// The default Data Protection purpose under which the ticket stores encrypt the authentication tickets, isolating them from every other
    /// use of the key ring (<see cref="CacheTicketStoreOptions.DataProtectionPurpose"/>, <see cref="FileTicketStoreOptions.DataProtectionPurpose"/>).
    /// </summary>
    public const string DefaultPurpose = "Arc4u.TicketStore.v1";

    internal static byte[] Protect(IDataProtector protector, AuthenticationTicket ticket)
        => protector.Protect(TicketSerializer.Default.Serialize(ticket));

    /// <summary>The ticket, or <see langword="null"/> when <paramref name="content"/> cannot be unprotected (written in clear, or under a key ring that no longer exists).</summary>
    internal static bool TryUnprotect(IDataProtector protector, byte[] content, out AuthenticationTicket? ticket)
    {
        try
        {
            ticket = TicketSerializer.Default.Deserialize(protector.Unprotect(content));
            return true;
        }
        catch (CryptographicException)
        {
            ticket = null;
            return false;
        }
    }
}
