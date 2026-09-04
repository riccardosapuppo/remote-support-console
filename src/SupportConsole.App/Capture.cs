namespace SupportConsole.App;

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using SupportConsole.Vision;

/// <summary>
/// Turning something on screen into a frame the detector can read, and back.
/// </summary>
/// <remarks>
/// <para>
/// This is the only file in the project that knows what a pixel is, and it is
/// deliberately small. Everything that decides anything lives in
/// <c>SupportConsole.Vision</c>, which has no reference to WPF, no reference to
/// Windows, and is tested on a machine that has neither.
/// </para>
/// <para>
/// It captures a window this application owns and nothing else. There is no
/// screen grab, no window enumeration, no reaching into another process — the
/// thing being read is the practice machine next to it. What the original did
/// with a remote-desktop session is the same operation applied to a window that
/// belongs to somebody else, and the deciding is identical either way, which is
/// why the deciding is what got built.
/// </para>
/// </remarks>
public static class Capture
{
    /// <summary>Render an element and reduce it to a frame.</summary>
    /// <param name="element">A visible element of this application.</param>
    /// <returns>The reduced frame, or null if the element has no size yet.</returns>
    public static RemoteFrame? Of(FrameworkElement element)
    {
        var wide = (int)Math.Round(element.ActualWidth);
        var tall = (int)Math.Round(element.ActualHeight);

        if (wide < 4 || tall < 4) return null;

        var rendered = new RenderTargetBitmap(wide, tall, 96, 96, PixelFormats.Pbgra32);

        // Through a brush, and not element straight into the bitmap.
        //
        // RenderTargetBitmap.Render draws a visual where it sits, offset by
        // wherever its parent put it. Handed an element forty pixels down the
        // window, it leaves forty blank rows at the top and drops forty from
        // the bottom — which is where the taskbar is. It cost an afternoon, and
        // the symptom was the same as the letterbox one a floor down: something
        // decided the bottom of the picture was somewhere it was not.
        var framed = new DrawingVisual();

        using (var draw = framed.RenderOpen())
        {
            draw.DrawRectangle(new VisualBrush(element), null, new Rect(0, 0, wide, tall));
        }

        rendered.Render(framed);

        var stride = wide * 4;
        var pixels = new byte[stride * tall];
        rendered.CopyPixels(pixels, stride, 0);

        return RemoteFrame.From(
            wide,
            tall,
            (x, y) =>
            {
                var at = (y * stride) + (x * 4);

                // Rec. 601 luma, in integers. Brightness rather than colour,
                // for the reason RemoteFrame gives: a taskbar is dark on light
                // or light on dark depending on a theme somebody chose.
                return (byte)(((pixels[at + 2] * 77) + (pixels[at + 1] * 150) + (pixels[at] * 29)) >> 8);
            });
    }

    /// <summary>
    /// Draw a frame back out, at one screen pixel per cell.
    /// </summary>
    /// <remarks>
    /// Shown at its own size and scaled with nearest-neighbour, so what is on
    /// screen is what the detector is looking at rather than a tidied version
    /// of it. If the grid looks coarse, that is the point: every threshold in
    /// this project is calibrated on exactly this much information.
    /// </remarks>
    /// <param name="frame">The frame to draw.</param>
    /// <returns>A grey image, one pixel per cell.</returns>
    public static BitmapSource Show(RemoteFrame frame)
    {
        var cells = new byte[frame.Width * frame.Height];

        for (var y = 0; y < frame.Height; y++)
        {
            for (var x = 0; x < frame.Width; x++) cells[(y * frame.Width) + x] = frame.At(x, y);
        }

        return BitmapSource.Create(
            frame.Width,
            frame.Height,
            96,
            96,
            PixelFormats.Gray8,
            null,
            cells,
            frame.Width);
    }
}
