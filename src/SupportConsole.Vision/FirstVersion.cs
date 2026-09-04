namespace SupportConsole.Vision;

/// <summary>
/// The version that was wrong, kept whole so the difference can be counted.
/// </summary>
/// <remarks>
/// <para>
/// This is not dead code and it is not history for its own sake. Every claim
/// this project makes is of the form "the new rule gets N frames right that the
/// old one got wrong", and a claim like that cannot be checked unless the old
/// rule is still here to be run. Delete it and the numbers in the README become
/// something to be believed.
/// </para>
/// <para>
/// It is also the more honest way to describe the work. The first version was
/// not careless — a taskbar really is a straight line across the bottom of the
/// screen, and on the machines it was written against it really was. What was
/// missing was not care but a corpus.
/// </para>
/// </remarks>
public static class FirstVersion
{
    /// <summary>The straight edge it was looking for.</summary>
    public const double StraightEnough = 0.70;

    /// <summary>How flat the band under that edge had to be.</summary>
    public const double FlatEnough = 40.0;

    /// <summary>How tall it assumed a taskbar was.</summary>
    public const int BandRows = 5;

    /// <summary>
    /// The fraction of the height it clicked at, which is where the Windows 10
    /// sign-in box is.
    /// </summary>
    public const double BoxAtHeight = 0.54;

    /// <summary>
    /// Read a frame the way the first version did: a flat band under a straight
    /// edge, at the bottom of the frame, and black counted as locked.
    /// </summary>
    /// <param name="frame">The look.</param>
    /// <returns>What it decided, in the same shape as the new one.</returns>
    public static ScreenReading Read(RemoteFrame? frame)
    {
        if (frame is null) return ScreenReading.Nothing;

        // The bottom of the frame, not the bottom of the picture. This is the
        // whole of the letterbox mistake: with black bars, the band being
        // measured is inside the black.
        var bottom = frame.Height - 1;
        var from = bottom - BandRows + 1;

        var straight = Signals.HorizontalEdge(frame, from) >= StraightEnough;

        var low = double.MaxValue;
        var high = double.MinValue;

        for (var y = from; y <= bottom; y++)
        {
            var mean = frame.MeanOfRows(y, y);
            low = Math.Min(low, mean);
            high = Math.Max(high, mean);
        }

        var flat = high - low <= FlatEnough;
        var taskbar = straight && flat;

        return new ScreenReading(
            taskbar ? ScreenState.InUse : ScreenState.Locked,
            taskbar,
            Signals.EdgeEnergy(frame),
            taskbar ? 0.9 : 0.6,
            taskbar ? "a straight edge with a flat band under it" : "no straight edge at the bottom of the frame");
    }

    /// <summary>
    /// Where it clicked before it looked: a fixed fraction of the height.
    /// </summary>
    /// <param name="frame">Unused, which is the point.</param>
    /// <returns>The same spot on every screen ever shown to it.</returns>
    public static Spot ClickWhereTheBoxUsuallyIs(RemoteFrame? frame)
    {
        _ = frame;
        return new Spot(0.5, BoxAtHeight);
    }

    /// <summary>
    /// The second attempt: find the largest pale rectangle in the middle of the
    /// screen. It finds the user photograph.
    /// </summary>
    /// <param name="frame">The look.</param>
    /// <returns>The middle of whatever was largest, or null if nothing was.</returns>
    public static Spot? LargestPaleThing(RemoteFrame? frame)
    {
        if (frame is null) return null;

        var fromRow = frame.Height * 20 / 100;
        var toRow = frame.Height * 78 / 100;

        int bestFrom = -1, bestTo = -1, bestRow = -1, bestTall = 0, bestArea = 0;

        for (var y = fromRow; y <= toRow; y++)
        {
            var (runFrom, runTo) = Signals.BrightestRun(frame, y);
            if (runFrom < 0) continue;

            var wide = runTo - runFrom + 1;
            var tall = 1;

            for (var below = y + 1; below <= toRow; below++)
            {
                var (from2, to2) = Signals.BrightestRun(frame, below);
                if (from2 < 0 || Math.Abs(from2 - runFrom) > 2 || Math.Abs(to2 - runTo) > 2) break;
                tall++;
            }

            // Area, and nothing about shape. A face is bigger than a text field.
            if (wide * tall > bestArea)
            {
                bestArea = wide * tall;
                bestTall = tall;
                bestFrom = runFrom;
                bestTo = runTo;
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
