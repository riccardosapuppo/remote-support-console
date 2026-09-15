namespace SupportConsole.App;

using System.IO;
using System.Windows;

/// <summary>
/// The window arrangement, asked about on screens nobody here has.
/// </summary>
/// <remarks>
/// <para>
/// The application opens two windows and places them as a pair, and a pair
/// placed by arithmetic is a pair that can be wrong on a screen the author did
/// not own: a laptop too narrow to hold both, a second monitor left of the
/// first so the work area starts at a negative x, a work area shortened by a
/// taskbar on the side.
/// </para>
/// <para>
/// None of those can be met by opening the window and looking. So the
/// arithmetic is a function, and this runs it over a set of screens and says
/// whether what came back is usable: on the screen, not overlapping when there
/// was room, and never with the practice machine hiding the list of frames the
/// console reads down its left.
/// </para>
/// <para>
/// A windowed application has nowhere to print, so it writes to stderr where
/// the runner can see it and answers with its exit code.
/// </para>
/// </remarks>
public static class Arranging
{
    /// <summary>The list of frames down the left of the console, in pixels.</summary>
    /// <remarks>
    /// The column width from MainWindow.xaml. Written here as the thing it
    /// means -- what must stay uncovered -- rather than as a number to keep in
    /// step: if the column grows, this check should be the thing that notices.
    /// </remarks>
    private const double TheListDownTheLeft = 288;

    private static readonly (string Called, Rect Screen)[] Screens =
    [
        ("a 1920 desktop", new Rect(0, 0, 1920, 1040)),
        ("a 2560 desktop", new Rect(0, 0, 2560, 1400)),
        ("a 1536 laptop", new Rect(0, 0, 1536, 824)),
        ("a 1366 laptop, too narrow for the pair", new Rect(0, 0, 1366, 728)),
        ("a second screen to the left", new Rect(-1920, 0, 1920, 1040)),
        ("a taskbar down the side", new Rect(72, 0, 1848, 1080)),
    ];

    /// <summary>Run the arithmetic over every screen in the list.</summary>
    /// <returns>Zero when every placement is usable.</returns>
    public static int Check()
    {
        var console = new Size(1160, 752);
        var machine = new Size(720, 566);
        var wrong = 0;

        var said = new StringWriter();
        said.WriteLine("Placing the console and the practice machine");
        said.WriteLine();

        foreach (var (called, screen) in Screens)
        {
            var (mine, theirs) = SideBySide.On(screen, console, machine);

            var complaints = new List<string>();

            if (!SideBySide.FitsOn(screen, mine)) complaints.Add("the console is off the screen");
            if (!SideBySide.FitsOn(screen, theirs)) complaints.Add("the practice machine is off the screen");

            var room = console.Width + SideBySide.Gap + machine.Width <= screen.Width;
            var apart = theirs.Left >= mine.Right - 0.5;

            if (room && !apart) complaints.Add("there was room for both and they overlap anyway");

            // When they do have to overlap, the list of frames stays readable.
            if (!room && theirs.Left < mine.Left + TheListDownTheLeft)
            {
                complaints.Add("the practice machine covers the list of frames");
            }

            if (complaints.Count > 0) wrong += 1;

            said.WriteLine(
                $"  {(complaints.Count == 0 ? "ok  " : "FAIL")}  {called}");
            said.WriteLine(
                $"          console {Say(mine)}   machine {Say(theirs)}   {(room ? "side by side" : "overlapping")}");

            foreach (var one in complaints) said.WriteLine($"          {one}");
        }

        said.WriteLine();
        said.WriteLine(
            wrong == 0
                ? $"All {Screens.Length} screens place both windows where somebody can use them."
                : $"{wrong} of {Screens.Length} screens do not.");

        Console.Error.Write(said.ToString());

        return wrong == 0 ? 0 : 1;
    }

    private static string Say(Rect where) =>
        $"{where.Width:0}x{where.Height:0} at {where.Left:0},{where.Top:0}";
}
