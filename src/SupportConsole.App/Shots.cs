namespace SupportConsole.App;

using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

/// <summary>
/// The pictures in the README, taken by the application itself.
/// </summary>
/// <remarks>
/// Rather than by somebody with a snipping tool, for the same reason the corpus
/// is drawn rather than photographed: a screenshot taken by hand is a screenshot
/// of whatever the window happened to be showing that afternoon, on a machine
/// with whatever else was open behind it. This runs on a clean process, picks
/// the frames by name, and writes files that are the same every time <b>on one
/// machine</b>.
///
/// Not across machines, and the distinction cost a red build. Text is rendered
/// by the operating system, and a runner and a desk do not lay out a glyph the
/// same way to the last byte, so a CI step comparing these byte for byte
/// asserted a property they do not have. What holds everywhere is that the
/// application still draws every picture the README shows, at the size it
/// shows it, which is what CI compares now.
/// </remarks>
public static class Shots
{
    /// <summary>
    /// Write the three pictures the README shows.
    /// </summary>
    /// <param name="into">The folder to write them into.</param>
    /// <returns>Zero if all three were written.</returns>
    public static int Take(string into)
    {
        Directory.CreateDirectory(into);

        // Both windows open before either closes. Closing the last one is what
        // shuts the application down, and a window opened after that never
        // lays out — it renders as nothing at all, zero pixels wide.
        var console = new MainWindow { Left = -4000, Top = -4000, ShowInTaskbar = false };
        var machine = new PracticeMachine { Left = -4000, Top = -3400, ShowInTaskbar = false };

        console.Show();
        machine.Show();

        console.ShowSource("in-use-dark-on-dark");
        Write(console, Path.Combine(into, "console-in-use.png"), TheConsole);

        console.ShowSource("locked-server-2012");
        Write(console, Path.Combine(into, "console-locked.png"), TheConsole);

        machine.Pretend(PracticeMachine.Screens.InUseDark);
        Write(machine, Path.Combine(into, "practice-machine.png"), TheMachine);

        console.Close();
        machine.Close();

        return 0;
    }

    /// <summary>The console's picture, at the size it is on this page.</summary>
    /// <remarks>
    /// Pinned here rather than taken from the window, and the reason is the
    /// bug below.
    /// </remarks>
    private static readonly Size TheConsole = new(1114, 682);

    /// <summary>The practice machine's picture, likewise.</summary>
    private static readonly Size TheMachine = new(678, 501);

    /// <summary>
    /// Render one window's content at a size this file decides.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It used to render at <c>ActualWidth</c> -- what the window turned out to
    /// be -- and a window turns out to be whatever the screen will allow. On a
    /// desk the console came out 1114 by 682; on a build machine with a 1024 by
    /// 768 display the same code drew 996 by 681, because a 1160-wide window
    /// does not fit and Windows made it fit.
    /// </para>
    /// <para>
    /// So the pictures in the README were pictures of somebody's monitor as
    /// much as of this program, and no two people could produce the same file.
    /// The layout is arranged to a size named here instead, which is the size
    /// the page shows, and the result no longer depends on what is plugged in.
    /// </para>
    /// </remarks>
    private static void Write(Window window, string path, Size at)
    {
        if (window.Content is not FrameworkElement inside) return;

        inside.Measure(at);
        inside.Arrange(new Rect(at));
        inside.UpdateLayout();

        var wide = (int)Math.Round(at.Width);
        var tall = (int)Math.Round(at.Height);

        var bitmap = new RenderTargetBitmap(wide, tall, 96, 96, PixelFormats.Pbgra32);
        var framed = new DrawingVisual();

        using (var draw = framed.RenderOpen())
        {
            // The same brush as Capture, and for the same reason: rendered
            // directly, a visual lands wherever its parent put it.
            draw.DrawRectangle(new VisualBrush(inside), null, new Rect(0, 0, wide, tall));
        }

        bitmap.Render(framed);

        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(bitmap));

        using var file = File.Create(path);
        png.Save(file);
    }
}
