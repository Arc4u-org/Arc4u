namespace Arc4u.OAuth2.Token;

/// <summary>
/// Counts the sign-outs of a token provider, so a token requested before a sign-out is not kept after it: take a <see cref="Snapshot"/> before requesting the token,
/// and keep the token only when <see cref="HasChanged"/> is <see langword="false"/>. The sign-out calls <see cref="Increment"/>.
/// </summary>
public sealed class SignOutGeneration
{
    private int _value;

    /// <summary>Gets the current generation, to compare with <see cref="HasChanged"/> once the token is received.</summary>
    /// <returns>The current generation.</returns>
    public int Snapshot() => Volatile.Read(ref _value);

    /// <summary>Gets a value indicating whether the user signed out since <paramref name="snapshot"/> was taken.</summary>
    /// <param name="snapshot">A value returned by <see cref="Snapshot"/>.</param>
    /// <returns><see langword="true"/> when a sign-out happened since the snapshot.</returns>
    public bool HasChanged(int snapshot) => Volatile.Read(ref _value) != snapshot;

    /// <summary>Records a sign-out.</summary>
    public void Increment() => Interlocked.Increment(ref _value);
}
