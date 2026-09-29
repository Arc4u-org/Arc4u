namespace Arc4u.Security.Principal;

/// <summary>
/// The class extend the security identifier to generate a custom sid based on a Guid.
/// It is useful if you need to use Azman with a custom sid.
/// </summary>
public static class SecurityIdentifierExt
{
    /// <summary>
    /// Generate a S-1-9 SecurityIdentifier based on a random <see cref="Guid"/>.
    /// </summary>
    /// <returns>A S-1-9 <see cref="System.Security.Principal.SecurityIdentifier"/></returns>
    public static string S19()
    {
        return S19(Guid.NewGuid());
    }

    /// <summary>
    /// Generates a S-1-9 security identifier, as a string, from this <see cref="Guid"/>. The same <see cref="Guid"/> always gives the same identifier.
    /// </summary>
    /// <param name="guid">The <see cref="Guid"/> from which the identifier is generated.</param>
    /// <returns>A S-1-9 security identifier in a string format.</returns>
    public static string ToS19(this Guid guid)
    {
        return S19(guid);
    }

    /// <summary>
    /// Generate a S-1-9 string SecurityIdentifier based on a given <see cref="Guid"/>.
    /// </summary>
    /// <param name="guid">A specific <see cref="Guid"/> generating a constant <see cref="System.Security.Principal.SecurityIdentifier"/></param>
    /// <returns>A S-1-9 <see cref="System.Security.Principal.SecurityIdentifier"/> in a string format.</returns>
    public static string S19(Guid guid)
    {
        var guidArray = guid.ToByteArray();
        var SidArray = new byte[16];

        Array.Copy(guidArray, 0, SidArray, 0, 16);

        return $"S-1-9-{BitConverter.ToUInt32(SidArray, 0)}-{BitConverter.ToUInt32(SidArray, 4)}-{BitConverter.ToUInt32(SidArray, 8)}-{BitConverter.ToUInt32(SidArray, 12)}";
    }

}
