namespace SupportConsole.Vision;

/// <summary>
/// Where the password field is, as a fraction of the frame.
/// </summary>
/// <remarks>
/// Fractions rather than pixels, because the caller has to turn this back into
/// a click inside a window whose size it knows and this file does not.
/// </remarks>
public readonly record struct Spot(double X, double Y);

/// <summary>
/// Finding the password field by what it looks like, not by where it usually is.
/// </summary>
/// <remarks>
/// <para>
/// The click matters because it is what sends the keystrokes to the remote
/// machine rather than to the window around it. Miss it and the password goes
/// somewhere else.
/// </para>
///
/// <para><b>Two ways of getting this wrong, both met.</b></para>
///
/// <para>
/// <b>A fixed position.</b> The first version clicked at 54% of the height,
/// which is where the box is on the Windows 10 sign-in screen. On Windows
/// Server 2012 it sits at 39% — measured — so the click landed on the wallpaper
/// and the password went into nothing. A percentage tuned on one version is a
/// fact about that version.
/// </para>
///
/// <para>
/// <b>Size without shape.</b> Looking for "the largest pale rectangle in the
/// middle of the screen" finds the <em>user photograph</em> on the sign-in
/// screen, which is bigger. Measured on Server 2012: the password box is 34
/// cells wide by 4 tall, a ratio of about nine; the photograph is 20 by 21, a
/// ratio of one. A text field is much wider than it is tall, and that one
/// constraint is what tells them apart.
/// </para>
/// </remarks>
public static class PasswordBox
{
    /// <summary>Shortest run that could be a field, as a fraction of the width.</summary>
    public const double LeastWide = 0.08;

    /// <summary>And the longest, above which it is a panel rather than a field.</summary>
    public const double MostWide = 0.55;

    /// <summary>A text field is at least this many times wider than it is tall.</summary>
    public const double LeastRatio = 4.0;

    /// <summary>The tallest a run can be and still be a field rather than a band.</summary>
    public const int MostTall = 6;

    /// <summary>Find the field, or say there is not one.</summary>
    /// <param name="frame">The look.</param>
    /// <returns>Where to click, as fractions of the frame, or null.</returns>
    public static Spot? FindIn(RemoteFrame? frame)
    {
        if (frame is null) return null;

        // The middle band. Not because the box is always there, but because the
        // top and bottom of a sign-in screen hold things shaped like fields —
        // a title bar, a taskbar — and this is cheaper than telling them apart.
        var fromRow = frame.Height * 20 / 100;
        var toRow = frame.Height * 78 / 100;

        var leastWide = Math.Max(4, (int)(frame.Width * LeastWide));
        var mostWide = (int)(frame.Width * MostWide);

        int bestFrom = -1, bestTo = -1, bestRow = -1, bestTall = 0;

        for (var y = fromRow; y <= toRow; y++)
        {
            var (from, to) = Signals.BrightestRun(frame, y);
            var wide = to - from + 1;

            if (from < 0 || wide < leastWide || wide > mostWide) continue;

            // How many rows in a row repeat the same bar? A text field is a few
            // rows tall; a band of the wallpaper is many. This also discards the
            // long edges of windows.
            var tall = 1;

            for (var below = y + 1; below <= toRow; below++)
            {
                var (from2, to2) = Signals.BrightestRun(frame, below);
                if (from2 < 0 || Math.Abs(from2 - from) > 2 || Math.Abs(to2 - to) > 2) break;
                tall++;
            }

            var ratio = (double)wide / tall;

            if (tall <= MostTall && ratio >= LeastRatio && wide > bestTo - bestFrom)
            {
                bestTall = tall;
                bestFrom = from;
                bestTo = to;
                bestRow = y;
            }

            y += Math.Max(0, tall - 1);
        }

        if (bestRow < 0) return null;

        return new Spot(
            (bestFrom + bestTo) / 2.0 / frame.Width,
            (bestRow + (bestTall / 2.0)) / frame.Height);
    }
}
