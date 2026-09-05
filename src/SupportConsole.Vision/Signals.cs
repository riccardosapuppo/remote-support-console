namespace SupportConsole.Vision;

/// <summary>
/// The measurements a frame can be asked for, each one on its own.
/// </summary>
/// <remarks>
/// <para>
/// Separate from <see cref="Detector"/> on purpose. A signal is a number you
/// can plot over a corpus and argue about; a decision is what you do with it.
/// Keeping them apart is what let the corpus in <c>SupportConsole.Frames</c>
/// show that one of these numbers <b>does not separate anything</b> — see
/// <see cref="EdgeEnergy"/>, which is still here precisely because it does not
/// work.
/// </para>
/// </remarks>
public static class Signals
{
    /// <summary>
    /// How much fine detail is on the screen: the mean gradient.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This number is here to be disbelieved.</b> The idea is reasonable and
    /// it is wrong: a desktop somebody is working at is full of crisp text, and
    /// a lock screen is a soft photograph with a box on it, so detail ought to
    /// tell them apart.
    /// </para>
    /// <para>
    /// Measured, the ranges lie on top of each other: a lock screen over the
    /// stock photograph reads 18.6, one on a dark background reads 2.0, and a
    /// desktop in use reads 15.1 to 43.1. There is no threshold: the locked
    /// ones sit on both sides of the unlocked ones. What this measures is how
    /// detailed the wallpaper is.
    /// </para>
    /// <para>
    /// Those three figures are recomputed off the corpus by a check, which
    /// fails if this paragraph, the one on <see cref="Detector"/>, or the
    /// README stops agreeing with it. They are not to be edited by hand: the
    /// figures that stood here before were true the day they were written, and
    /// went on being quoted for a long time after they had stopped being.
    /// </para>
    /// <para>
    /// It is kept, computed, and reported — never used to decide. The
    /// measurement prints it beside every frame so the claim can be checked
    /// rather than believed, and a test asserts that no threshold over this
    /// number classifies the corpus.
    /// </para>
    /// </remarks>
    public static double EdgeEnergy(RemoteFrame frame)
    {
        long total = 0;
        var counted = 0;

        for (var y = 1; y < frame.Height - 1; y++)
        {
            for (var x = 1; x < frame.Width - 1; x++)
            {
                var acrossX = Math.Abs(frame.At(x + 1, y) - frame.At(x - 1, y));
                var acrossY = Math.Abs(frame.At(x, y + 1) - frame.At(x, y - 1));

                total += (acrossX + acrossY) / 2;
                counted++;
            }
        }

        return counted == 0 ? 0 : (double)total / counted;
    }

    /// <summary>
    /// The last row with anything on it, so black bars below are not searched.
    /// </summary>
    /// <remarks>
    /// A remote desktop whose proportions differ from the window it is shown in
    /// gets centred, with black above and below. Looking for a taskbar in the
    /// bottom rows of the frame then means looking inside the black — and a
    /// machine somebody was working at read as one to unlock. The frame's own
    /// bottom is not the picture's bottom.
    /// </remarks>
    public static int LastRowWithContent(RemoteFrame frame)
    {
        for (var y = frame.Height - 1; y >= frame.Height / 2; y--)
        {
            var lit = 0;

            for (var x = 0; x < frame.Width; x++)
            {
                if (frame.At(x, y) > 14) lit++;
            }

            if (lit > frame.Width / 10) return y;
        }

        return frame.Height - 1;
    }

    /// <summary>
    /// How straight a horizontal edge is at one row: the fraction of columns
    /// where the brightness jumps against the row above.
    /// </summary>
    /// <remarks>
    /// The top of a full-width taskbar is a straight line crossing the whole
    /// screen, and this measures exactly that. It is a strong signal where it
    /// is present and absent entirely on a dark theme over a dark wallpaper,
    /// which is why it is one of two ways in rather than the way in.
    /// </remarks>
    public static double HorizontalEdge(RemoteFrame frame, int row, int jump = 18)
    {
        if (row <= 0 || row >= frame.Height) return 0;

        var counted = 0;

        for (var x = 0; x < frame.Width; x++)
        {
            if (Math.Abs(frame.At(x, row) - frame.At(x, row - 1)) >= jump) counted++;
        }

        return (double)counted / frame.Width;
    }

    /// <summary>
    /// How many vertical slices of a band contain <em>structure</em>, and how far
    /// apart the first and last of them are.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Structure means a strong difference between the lightest and darkest cell
    /// of the slice. It is what tells a row of icons from a gradient in a
    /// photograph, which varies smoothly and produces none.
    /// </para>
    /// <para>
    /// The spread matters as much as the count. A taskbar has icons on the left
    /// and a clock on the right, so its structure runs the width of the screen;
    /// the three small icons at the bottom right of a lock screen all fall in
    /// one slice.
    /// </para>
    /// </remarks>
    public static (int Filled, int Spread) SlicesWithStructure(
        RemoteFrame frame,
        int fromRow,
        int toRow,
        int slices = 12,
        int difference = 25)
    {
        var wide = Math.Max(1, frame.Width / slices);

        var filled = 0;
        var first = -1;
        var last = -1;

        for (var i = 0; i < slices; i++)
        {
            var x0 = i * wide;
            var x1 = Math.Min(frame.Width - 1, x0 + wide - 1);

            if (x0 > x1) break;

            var (low, high) = frame.RangeIn(x0, x1, fromRow, toRow);
            if (high - low < difference) continue;

            filled++;
            if (first < 0) first = i;
            last = i;
        }

        return (filled, first < 0 ? 0 : last - first);
    }

    /// <summary>
    /// The longest run of cells on one row that are brighter than their
    /// surroundings, as (start, end), or (-1, -1).
    /// </summary>
    /// <remarks>
    /// Used to find the password field, which is a pale bar on a darker ground.
    /// The threshold is relative to the row rather than absolute, because the
    /// same field sits on a photograph on one machine and on flat blue on
    /// another.
    /// </remarks>
    public static (int From, int To) BrightestRun(RemoteFrame frame, int row)
    {
        if (row < 0 || row >= frame.Height) return (-1, -1);

        long total = 0;
        for (var x = 0; x < frame.Width; x++) total += frame.At(x, row);

        var mean = (double)total / frame.Width;
        var threshold = mean + 30;

        int bestFrom = -1, bestTo = -1, from = -1;

        for (var x = 0; x < frame.Width; x++)
        {
            if (frame.At(x, row) >= threshold)
            {
                if (from < 0) from = x;

                if (x - from > bestTo - bestFrom)
                {
                    bestFrom = from;
                    bestTo = x;
                }
            }
            else
            {
                from = -1;
            }
        }

        return (bestFrom, bestTo);
    }
}
