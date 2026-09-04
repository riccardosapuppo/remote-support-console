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
/// the frames by name, and writes files that are the same every time.
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
        Write(console, Path.Combine(into, "console-in-use.png"));

        console.ShowSource("locked-server-2012");
        Write(console, Path.Combine(into, "console-locked.png"));

        machine.Pretend(PracticeMachine.Screens.InUseDark);
        Write(machine, Path.Combine(into, "practice-machine.png"));

        console.Close();
        machine.Close();

        return 0;
    }

    private static void Write(Window window, string path)
    {
        if (window.Content is not FrameworkElement inside) return;

        window.UpdateLayout();

        var wide = (int)Math.Round(inside.ActualWidth);
        var tall = (int)Math.Round(inside.ActualHeight);

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
