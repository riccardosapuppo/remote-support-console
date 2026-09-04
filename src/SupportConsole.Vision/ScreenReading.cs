namespace SupportConsole.Vision;

/// <summary>What a frame turned out to be.</summary>
public enum ScreenState
{
    /// <summary>Nothing was handed in.</summary>
    Unknown,

    /// <summary>
    /// Black, which is its own answer and not a kind of locked.
    /// </summary>
    /// <remarks>
    /// A session that has connected and not drawn yet, or a monitor asleep.
    /// Neither is a machine waiting for a password.
    /// </remarks>
    Black,

    /// <summary>Nothing says anybody is logged in.</summary>
    Locked,

    /// <summary>Somebody is logged in, and there is positive evidence of it.</summary>
    InUse,
}

/// <summary>
/// One reading of a remote screen: what it is, why, and how sure.
/// </summary>
/// <param name="State">The verdict.</param>
/// <param name="TaskbarFound">
/// The only signal that means <em>somebody is logged in</em>. Carried separately
/// from <paramref name="State"/> because the asymmetry below turns on it.
/// </param>
/// <param name="EdgeEnergy">
/// How much fine detail is on screen. <b>Measured and reported, never used to
/// decide</b> — see <see cref="Signals.EdgeEnergy"/> for why.
/// </param>
/// <param name="Confidence">Between 0 and 1, and honest about being a guess.</param>
/// <param name="Because">The reason, in words, for a person reading a log.</param>
public sealed record ScreenReading(
    ScreenState State,
    bool TaskbarFound,
    double EdgeEnergy,
    double Confidence,
    string Because)
{
    /// <summary>The reading of no frame at all.</summary>
    public static ScreenReading Nothing { get; } =
        new(ScreenState.Unknown, false, 0, 0, "no frame");

    /// <summary>
    /// True only when there is <em>positive evidence</em> that somebody has
    /// already logged in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the property the dangerous decisions hang on, and it is
    /// deliberately not the negation of <see cref="ScreenState.Locked"/>.
    /// </para>
    /// <para>
    /// The two mistakes do not cost the same. Deciding a locked machine is in
    /// use means doing nothing — somebody presses the button again. Deciding a
    /// machine in use is locked means sending Ctrl+Alt+Del at a session
    /// somebody is working in, and then, if nothing else stopped it, typing a
    /// password into whatever happens to have focus. One of those is a wasted
    /// click and the other is a password in a chat window.
    /// </para>
    /// <para>
    /// So the question is never "is it locked" but "is there anything saying it
    /// is <em>not</em>", and absence of evidence is treated as the dangerous
    /// direction rather than the safe one.
    /// </para>
    /// </remarks>
    public bool SomebodyIsLoggedIn => TaskbarFound;
}
