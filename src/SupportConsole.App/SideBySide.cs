namespace SupportConsole.App;

using System.Windows;

/// <summary>
/// Where the console and the practice machine go, as arithmetic.
/// </summary>
/// <remarks>
/// <para>
/// The two windows are the demonstration. One reads, the other is read, and
/// seeing that happen means seeing both at once: change the state on the left
/// and watch the decision change on the right, four times a second. A pair
/// stacked on top of each other is two windows somebody has to arrange before
/// the point of the thing is visible, and arranging windows is a manoeuvre
/// that does not get performed.
/// </para>
/// <para>
/// Separated from the window that uses it because a window cannot be asked a
/// question on a build machine. This can: it takes three rectangles and returns
/// two, it has no WPF in it beyond the shape of a rectangle, and
/// <c>--arrange</c> runs it over a set of screens including ones too small to
/// hold the pair.
/// </para>
/// </remarks>
public static class SideBySide
{
    /// <summary>The gap between them, in device-independent pixels.</summary>
    public const double Gap = 14;

    /// <summary>
    /// Place the two windows on a screen.
    /// </summary>
    /// <param name="screen">The work area: the screen without the taskbar.</param>
    /// <param name="console">How big the console is.</param>
    /// <param name="machine">How big the practice machine is.</param>
    /// <returns>Where each one goes.</returns>
    public static (Rect Console, Rect Machine) On(Rect screen, Size console, Size machine)
    {
        /*
         * Cut to the screen before being placed anywhere.
         *
         * The console is 1160 by 752, and the work area of a 1366 by 768
         * laptop is 728 tall once the taskbar has had its share. Moving a
         * window that does not fit only decides which edge it hangs off; the
         * first version of this did exactly that, and `--arrange` said so on
         * the one screen shape nobody here owns.
         */
        console = new Size(Math.Min(console.Width, screen.Width), Math.Min(console.Height, screen.Height));
        machine = new Size(Math.Min(machine.Width, screen.Width), Math.Min(machine.Height, screen.Height));

        var both = console.Width + Gap + machine.Width;

        if (both <= screen.Width)
        {
            // Room for the pair: centre them together, and line the machine up
            // with the middle of the console rather than with its top, so the
            // two read as one arrangement.
            var left = screen.Left + ((screen.Width - both) / 2);
            var top = screen.Top + Math.Max(0, (screen.Height - console.Height) / 2);

            return (
                new Rect(left, top, console.Width, console.Height),
                new Rect(
                    left + console.Width + Gap,
                    top + Math.Max(0, (console.Height - machine.Height) / 2),
                    machine.Width,
                    machine.Height));
        }

        // Not enough room, which is an ordinary laptop and not an edge case.
        // The machine goes over the console's bottom right, because the list
        // of frames runs down the left of the console and is the part that
        // must stay readable underneath.
        var consoleLeft = screen.Left + Math.Max(0, (screen.Width - console.Width) / 2);
        var consoleTop = screen.Top + Math.Max(0, (screen.Height - console.Height) / 2);

        var machineLeft = Math.Min(
            consoleLeft + console.Width - machine.Width,
            screen.Right - machine.Width);

        var machineTop = Math.Min(
            consoleTop + console.Height - machine.Height,
            screen.Bottom - machine.Height);

        return (
            new Rect(consoleLeft, consoleTop, console.Width, console.Height),
            new Rect(Math.Max(screen.Left, machineLeft), Math.Max(screen.Top, machineTop), machine.Width, machine.Height));
    }

    /// <summary>Whether a placement keeps a window on the screen it was given.</summary>
    /// <param name="screen">The work area.</param>
    /// <param name="where">Where the window was put.</param>
    /// <returns>True when none of it is off the edge.</returns>
    /// <remarks>
    /// Half a pixel of slack, because these are doubles and a check that fails
    /// on rounding is a check somebody deletes.
    /// </remarks>
    public static bool FitsOn(Rect screen, Rect where) =>
        where.Left >= screen.Left - 0.5
        && where.Top >= screen.Top - 0.5
        && where.Right <= screen.Right + 0.5
        && where.Bottom <= screen.Bottom + 0.5;
}
