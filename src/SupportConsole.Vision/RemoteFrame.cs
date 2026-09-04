namespace SupportConsole.Vision;

/// <summary>
/// One look at a remote screen, as a small grid of brightnesses.
/// </summary>
/// <remarks>
/// <para>
/// A frame arrives as a bitmap of a couple of million pixels and is reduced to
/// a grid of about twenty thousand before anything looks at it. That is not an
/// optimisation, or not only: every signal in <see cref="Signals"/> is about a
/// <em>region</em> — a band across the bottom, a run of bright cells, structure
/// spread horizontally — and a region is what a downsample makes visible.
/// Averaging is the first half of every one of those measurements, so it is
/// done once, here.
/// </para>
/// <para>
/// Brightness, not colour. Nothing this decides depends on hue: a taskbar is
/// dark on light or light on dark depending on a theme somebody chose, and a
/// signal that assumed one of those would be a signal about themes.
/// </para>
/// <para>
/// <b>The grid is the unit of every threshold in this project.</b> `8% of the
/// height`, `three sectors out of twelve`, `a run at least 8% of the width` —
/// all of them are fractions rather than pixels, because the same remote
/// desktop arrives at whatever size the window happens to be. A threshold in
/// pixels is a threshold that changes meaning when somebody resizes.
/// </para>
/// </remarks>
public sealed class RemoteFrame
{
    /// <summary>The grid the whole project is calibrated on.</summary>
    public const int DefaultWidth = 192;

    /// <summary>The other half of it.</summary>
    public const int DefaultHeight = 108;

    private readonly byte[] cells;

    /// <summary>Wrap a grid of brightnesses that is already the right size.</summary>
    /// <param name="width">Cells across.</param>
    /// <param name="height">Cells down.</param>
    /// <param name="cells">Row by row, top to bottom.</param>
    public RemoteFrame(int width, int height, byte[] cells)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "a frame with no area is not a frame");
        }

        if (cells.Length != width * height)
        {
            throw new ArgumentException(
                $"a {width}x{height} grid needs {width * height} cells and was given {cells.Length}",
                nameof(cells));
        }

        Width = width;
        Height = height;
        this.cells = cells;
    }

    /// <summary>Cells across.</summary>
    public int Width { get; }

    /// <summary>Cells down.</summary>
    public int Height { get; }

    /// <summary>The brightness at one cell, 0 to 255.</summary>
    public byte At(int x, int y) => cells[(y * Width) + x];

    /// <summary>
    /// A frame with nothing on it.
    /// </summary>
    /// <remarks>
    /// Its own answer, not a kind of "locked". A remote session that has
    /// connected but not drawn yet is black, and so is a monitor that is asleep
    /// — and neither of those is a machine waiting for a password. The first
    /// version of this treated black as locked and sent Ctrl+Alt+Del at a
    /// session that was still opening.
    /// </remarks>
    public bool IsBlack
    {
        get
        {
            foreach (var cell in cells)
            {
                if (cell > 12) return false;
            }

            return true;
        }
    }

    /// <summary>The mean brightness of one band of rows.</summary>
    public double MeanOfRows(int fromRow, int toRow)
    {
        var from = Math.Clamp(fromRow, 0, Height - 1);
        var to = Math.Clamp(toRow, 0, Height - 1);

        if (to < from) (from, to) = (to, from);

        long total = 0;

        for (var y = from; y <= to; y++)
        {
            for (var x = 0; x < Width; x++) total += At(x, y);
        }

        return (double)total / ((to - from + 1) * Width);
    }

    /// <summary>The darkest and brightest cell in a rectangle.</summary>
    public (byte Low, byte High) RangeIn(int x0, int x1, int y0, int y1)
    {
        byte low = 255;
        byte high = 0;

        for (var y = Math.Max(0, y0); y <= Math.Min(Height - 1, y1); y++)
        {
            for (var x = Math.Max(0, x0); x <= Math.Min(Width - 1, x1); x++)
            {
                var value = At(x, y);
                if (value < low) low = value;
                if (value > high) high = value;
            }
        }

        return (low, high);
    }

    /// <summary>
    /// Build a frame from a full-size image, by averaging each block of pixels.
    /// </summary>
    /// <param name="sourceWidth">Pixels across in the image being reduced.</param>
    /// <param name="sourceHeight">Pixels down in it.</param>
    /// <param name="brightnessAt">
    /// The brightness of one source pixel. Passed in rather than reached for, so
    /// this file compiles and is tested on any operating system — the part that
    /// knows about bitmaps and screen captures lives on the Windows side and
    /// none of the deciding does.
    /// </param>
    /// <param name="width">Cells across in the result.</param>
    /// <param name="height">Cells down in it.</param>
    /// <returns>The reduced frame.</returns>
    public static RemoteFrame From(
        int sourceWidth,
        int sourceHeight,
        Func<int, int, byte> brightnessAt,
        int width = DefaultWidth,
        int height = DefaultHeight)
    {
        var cells = new byte[width * height];

        for (var y = 0; y < height; y++)
        {
            var y0 = (int)((long)y * sourceHeight / height);
            var y1 = (int)((long)(y + 1) * sourceHeight / height);
            if (y1 <= y0) y1 = y0 + 1;

            for (var x = 0; x < width; x++)
            {
                var x0 = (int)((long)x * sourceWidth / width);
                var x1 = (int)((long)(x + 1) * sourceWidth / width);
                if (x1 <= x0) x1 = x0 + 1;

                long total = 0;
                var counted = 0;

                for (var sy = y0; sy < y1 && sy < sourceHeight; sy++)
                {
                    for (var sx = x0; sx < x1 && sx < sourceWidth; sx++)
                    {
                        total += brightnessAt(sx, sy);
                        counted++;
                    }
                }

                cells[(y * width) + x] = counted == 0 ? (byte)0 : (byte)(total / counted);
            }
        }

        return new RemoteFrame(width, height, cells);
    }
}
