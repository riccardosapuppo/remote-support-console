namespace SupportConsole.Frames;

using SupportConsole.Vision;

/// <summary>
/// Drawing the screens the detector is measured against.
/// </summary>
/// <remarks>
/// <para>
/// Invented, and drawn rather than photographed, for two reasons. A screenshot
/// of a real machine is somebody's desktop — their files, their mail, their
/// patients — and no amount of care makes it safe to put in a public
/// repository. And a drawn frame can be varied on one axis at a time, which is
/// what turns "the detector works" into a measurement.
/// </para>
/// <para>
/// Nothing here reads a clock or an unseeded random number. The corpus is the
/// same corpus on every machine and every run, which is what lets a README
/// quote a number and a test assert it.
/// </para>
/// </remarks>
public sealed class Paint
{
    private readonly byte[] cells;
    private ulong seed;

    /// <summary>Start a blank frame.</summary>
    /// <param name="width">Cells across.</param>
    /// <param name="height">Cells down.</param>
    /// <param name="seed">Fixed, so the same corpus comes out every run.</param>
    public Paint(int width = RemoteFrame.DefaultWidth, int height = RemoteFrame.DefaultHeight, ulong seed = 1)
    {
        Width = width;
        Height = height;
        cells = new byte[width * height];
        this.seed = seed == 0 ? 1 : seed;
    }

    /// <summary>Cells across.</summary>
    public int Width { get; }

    /// <summary>Cells down.</summary>
    public int Height { get; }

    /// <summary>
    /// A repeatable stream of numbers.
    /// </summary>
    /// <remarks>
    /// Written out rather than taken from <c>Random</c>: the shared one is
    /// seeded from the clock, and a corpus that differs between runs is a corpus
    /// no number can be quoted from.
    /// </remarks>
    private double Next()
    {
        seed ^= seed << 13;
        seed ^= seed >> 7;
        seed ^= seed << 17;
        return (seed >> 11) / (double)(1UL << 53);
    }

    private void Set(int x, int y, double value)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        cells[(y * Width) + x] = (byte)Math.Clamp(value, 0, 255);
    }

    private byte Get(int x, int y) => cells[(Math.Clamp(y, 0, Height - 1) * Width) + Math.Clamp(x, 0, Width - 1)];

    /// <summary>Fill the whole frame with one brightness.</summary>
    /// <param name="brightness">0 to 255.</param>
    /// <returns>This, to keep drawing.</returns>
    public Paint Flat(int brightness)
    {
        for (var i = 0; i < cells.Length; i++) cells[i] = (byte)Math.Clamp(brightness, 0, 255);
        return this;
    }

    /// <summary>A soft vertical gradient: the shape of most wallpapers.</summary>
    /// <param name="top">Brightness at the first row.</param>
    /// <param name="bottom">Brightness at the last.</param>
    /// <returns>This, to keep drawing.</returns>
    public Paint Gradient(int top, int bottom)
    {
        for (var y = 0; y < Height; y++)
        {
            var value = top + ((bottom - top) * (double)y / Math.Max(1, Height - 1));
            for (var x = 0; x < Width; x++) Set(x, y, value);
        }

        return this;
    }

    /// <summary>
    /// Slow, smooth variation, the way a photograph varies.
    /// </summary>
    /// <remarks>
    /// The point of it: it must add <em>brightness</em> without adding
    /// structure, because "structure" is exactly what the detector looks for.
    /// A photograph that produced structure would make the corpus argue for the
    /// wrong thing.
    /// </remarks>
    /// <param name="amount">How far the brightness wanders, in levels.</param>
    /// <param name="scale">Cells per knot: bigger is smoother.</param>
    /// <returns>This, to keep drawing.</returns>
    public Paint Clouds(int amount, int scale = 24)
    {
        var across = Math.Max(2, Width / scale) + 2;
        var down = Math.Max(2, Height / scale) + 2;

        var knots = new double[across * down];
        for (var i = 0; i < knots.Length; i++) knots[i] = Next();

        for (var y = 0; y < Height; y++)
        {
            var fy = (double)y / Height * (down - 1);
            var y0 = (int)fy;
            var ty = fy - y0;

            for (var x = 0; x < Width; x++)
            {
                var fx = (double)x / Width * (across - 1);
                var x0 = (int)fx;
                var tx = fx - x0;

                var a = knots[(y0 * across) + x0];
                var b = knots[(y0 * across) + Math.Min(across - 1, x0 + 1)];
                var c = knots[(Math.Min(down - 1, y0 + 1) * across) + x0];
                var d = knots[(Math.Min(down - 1, y0 + 1) * across) + Math.Min(across - 1, x0 + 1)];

                var top = a + ((b - a) * tx);
                var low = c + ((d - c) * tx);

                Set(x, y, Get(x, y) + ((top + ((low - top) * ty)) - 0.5) * amount);
            }
        }

        return this;
    }

    /// <summary>A filled rectangle.</summary>
    /// <param name="x0">Left edge.</param>
    /// <param name="y0">Top edge.</param>
    /// <param name="wide">Cells across.</param>
    /// <param name="tall">Cells down.</param>
    /// <param name="brightness">0 to 255.</param>
    /// <returns>This, to keep drawing.</returns>
    public Paint Box(int x0, int y0, int wide, int tall, int brightness)
    {
        for (var y = y0; y < y0 + tall; y++)
        {
            for (var x = x0; x < x0 + wide; x++) Set(x, y, brightness);
        }

        return this;
    }

    /// <summary>
    /// Something that reads like text: short bright runs on alternating rows.
    /// </summary>
    /// <remarks>
    /// This is what makes a working desktop <em>detailed</em>, and it is the
    /// signal the whole project measured and then refused to use. It is drawn
    /// here so the refusal can be demonstrated rather than asserted.
    /// </remarks>
    /// <param name="x0">Left edge.</param>
    /// <param name="y0">Top edge.</param>
    /// <param name="wide">Cells across.</param>
    /// <param name="tall">Cells down.</param>
    /// <param name="brightness">The ink.</param>
    /// <param name="gap">Rows between lines.</param>
    /// <returns>This, to keep drawing.</returns>
    public Paint TextLike(int x0, int y0, int wide, int tall, int brightness, int gap = 2)
    {
        for (var y = y0; y < y0 + tall; y += gap)
        {
            var x = x0;

            while (x < x0 + wide)
            {
                var run = 1 + (int)(Next() * 4);

                for (var i = 0; i < run && x < x0 + wide; i++, x++) Set(x, y, brightness);

                x += 1 + (int)(Next() * 2);
            }
        }

        return this;
    }

    /// <summary>
    /// A taskbar: a strip at the bottom with icons at one end and a clock at the
    /// other.
    /// </summary>
    /// <param name="step">
    /// How far the strip's brightness is from the wallpaper above it. Set it to
    /// zero for the case that broke the first detector: a dark bar on a dark
    /// wallpaper, where there is no edge to measure and the only thing left is
    /// the structure along it.
    /// </param>
    /// <param name="brightness">The wallpaper it sits on.</param>
    /// <param name="tall">How many rows the strip takes.</param>
    /// <param name="iconBrightness">The icons and the clock.</param>
    /// <returns>This, to keep drawing.</returns>
    public Paint Taskbar(int brightness, int tall = 4, int step = 30, int iconBrightness = 220)
    {
        var from = Height - tall;

        Box(0, from, Width, tall, brightness + step);

        // Icons at the left, spread over about a third of the width.
        for (var i = 0; i < 7; i++)
        {
            Box(3 + (i * 5), from + 1, 3, Math.Max(1, tall - 2), iconBrightness);
        }

        // A clock and a notification area at the right.
        Box(Width - 18, from + 1, 12, Math.Max(1, tall - 2), iconBrightness);
        Box(Width - 26, from + 1, 4, Math.Max(1, tall - 2), iconBrightness);

        return this;
    }

    /// <summary>
    /// Fine detail in every part of the frame.
    /// </summary>
    /// <remarks>
    /// The wallpaper that caused the trouble: a photograph busy enough that
    /// every slice of every band has something in it, top to bottom. Against
    /// <see cref="Clouds"/>, which adds brightness and no structure, this adds
    /// structure and almost no brightness, and the two of them are how the
    /// corpus separates detailed from bright.
    /// </remarks>
    /// <param name="amount">How far a speck is pushed from its neighbours.</param>
    /// <param name="every">One cell in this many gets one.</param>
    /// <returns>This, to keep drawing.</returns>
    public Paint Speckle(int amount, int every = 3)
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                if (Next() * every >= 1) continue;
                Set(x, y, Get(x, y) + (Next() < 0.5 ? -amount : amount));
            }
        }

        return this;
    }

    /// <summary>Black bars above and below, the way a mismatched aspect ratio arrives.</summary>
    /// <param name="bars">How many rows of black at each end.</param>
    /// <returns>This, to keep drawing.</returns>
    public Paint Letterbox(int bars)
    {
        Box(0, 0, Width, bars, 0);
        Box(0, Height - bars, Width, bars, 0);
        return this;
    }

    /// <summary>Hand back what was drawn.</summary>
    /// <returns>A frame the detector can read.</returns>
    public RemoteFrame Done() => new(Width, Height, (byte[])cells.Clone());
}
