namespace SupportConsole.Vision;

/// <summary>
/// Deciding, from a picture, whether anybody is logged in to a remote machine.
/// </summary>
/// <remarks>
/// <para>
/// There is nothing else to go on. Inside a remote-desktop session there is no
/// UI Automation tree to ask, no API, no window titles — the whole session is
/// one bitmap being repainted. So the question is answered by looking, and the
/// only honest thing to do with that is to be explicit about what is being
/// looked for and what it costs to be wrong.
/// </para>
///
/// <para><b>Three things were believed and measured, and two of them were wrong.</b></para>
///
/// <para>
/// <b>Sharpness.</b> A working desktop is full of crisp text and a lock screen
/// is a soft photograph, so detail ought to separate them. It does not: a lock
/// screen over the stock photograph reads 18.6, one on a dark background reads
/// 2.0, and a desktop in use reads 15.1 to 43.1. The locked cases sit on both
/// sides of the unlocked ones and no threshold exists. It is still computed and
/// still reported — see <see cref="Signals.EdgeEnergy"/> — and never used.
/// </para>
///
/// <para>
/// <b>A uniform band under a straight edge.</b> The first taskbar test. On a
/// dark theme over a dark wallpaper there is no edge to measure: at this
/// resolution the taskbar is a row of bright dots on black, and the detector
/// said "locked" about a machine somebody was working in.
/// </para>
///
/// <para>
/// <b>The bottom of the frame is the bottom of the picture.</b> It is not. A
/// remote desktop with different proportions is centred with black bars, so the
/// search was happening inside the black.
/// </para>
///
/// <para>
/// What survived is two things that a taskbar has and a photograph does not,
/// required together: a <b>step in mean brightness</b> between the bottom strip
/// and the one above it, and <b>structure spread horizontally</b> — icons at one
/// end, a clock at the other. The step is what carries the dark-on-dark case:
/// the bar itself is the colour of the wallpaper, but the icons on it lift the
/// mean of the band by about twenty levels, and a mean does not need an edge.
/// </para>
///
/// <para>
/// <b>A fourth rule was written, measured, and thrown away.</b> A wallpaper lit
/// along the bottom passes both tests — a step at the top of the lit strip, and
/// detail spread the width of it — and reads as a taskbar. The obvious fix is
/// that above a real taskbar the structure stops, and it works: it turns that
/// frame from wrong to right. It also turns a desktop in use over a busy
/// wallpaper from right to wrong, because there the structure does not stop
/// either. One frame each way, so the trade is decided by which way it is
/// safe to be wrong — and calling a locked screen "in use" costs a wasted
/// click, while calling a desktop in use "locked" types a password into it.
/// The rule is kept behind <c>requireStructureToStop</c> so the
/// measurement can print both columns instead of asserting the conclusion.
/// </para>
/// </remarks>
public static class Detector
{
    /// <summary>
    /// The smallest step in mean brightness that counts as the top of a taskbar.
    /// </summary>
    /// <remarks>
    /// Measured: 21.4 on a desktop with a taskbar, against fractions of a point
    /// between two neighbouring bands of the same wallpaper. Set at 8 — well
    /// under what was seen — because the cost of missing a taskbar is high and
    /// the cost of a false one is nothing at all.
    /// </remarks>
    public const double LeastStep = 8.0;

    /// <summary>The straight-edge test, where a theme provides one.</summary>
    public const double StraightEnough = 0.70;

    /// <summary>Read one frame.</summary>
    /// <param name="frame">The look, or null if there has not been one.</param>
    /// <returns>What it is, why, and how sure.</returns>
    public static ScreenReading Read(RemoteFrame? frame)
    {
        if (frame is null) return ScreenReading.Nothing;

        if (frame.IsBlack)
        {
            return new ScreenReading(
                ScreenState.Black,
                TaskbarFound: false,
                EdgeEnergy: 0,
                Confidence: 0.5,
                "black: the session has not drawn yet, or the remote monitor is asleep");
        }

        var edges = Signals.EdgeEnergy(frame);

        if (HasTaskbar(frame))
        {
            return new ScreenReading(
                ScreenState.InUse,
                TaskbarFound: true,
                edges,
                Confidence: 0.9,
                $"a taskbar is visible (detail {edges:F1}, which was not used)");
        }

        return new ScreenReading(
            ScreenState.Locked,
            TaskbarFound: false,
            edges,
            Confidence: 0.6,
            $"no taskbar (detail {edges:F1}, which was not used)");
    }

    /// <summary>
    /// Is there a taskbar: a strip at the bottom, set off from what is above it,
    /// with structure spread along it.
    /// </summary>
    /// <param name="frame">The look.</param>
    /// <param name="requireStructureToStop">
    /// The rule that was measured and rejected — see the note on this class. Off
    /// in production; the measurement turns it on to print what it would cost.
    /// </param>
    /// <returns>True when there is positive evidence of a taskbar.</returns>
    public static bool HasTaskbar(RemoteFrame frame, bool requireStructureToStop = false)
    {
        var bottom = Signals.LastRowWithContent(frame);
        var tallest = Math.Max(2, frame.Height * 8 / 100);

        for (var tall = 2; tall <= tallest; tall++)
        {
            var stripFrom = bottom - tall + 1;
            var aboveTo = stripFrom - 1;
            var aboveFrom = aboveTo - tall + 1;

            if (aboveFrom < 0) break;

            var strip = frame.MeanOfRows(stripFrom, bottom);
            var above = frame.MeanOfRows(aboveFrom, aboveTo);

            // Either direction. A pale bar on a dark wallpaper and a dark bar on
            // a pale one are equally common, and a test that assumed one of them
            // would be a test about themes.
            var steps = Math.Abs(strip - above) >= LeastStep;

            // And a straight edge still counts where a theme draws one, which is
            // the light-theme case where the step alone can be small.
            var straight = Signals.HorizontalEdge(frame, stripFrom) >= StraightEnough;

            if (!steps && !straight) continue;

            var (filled, spread) = Signals.SlicesWithStructure(frame, stripFrom, bottom);
            if (filled < 3 || spread < 6) continue;

            if (requireStructureToStop)
            {
                var (filledAbove, _) = Signals.SlicesWithStructure(frame, aboveFrom, aboveTo);
                if (filledAbove * 2 > filled) continue;
            }

            return true;
        }

        return false;
    }
}
