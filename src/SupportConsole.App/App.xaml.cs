namespace SupportConsole.App;

using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;

using SupportConsole.Vision;

/// <summary>
/// The application: it opens the console, or it checks itself and stops.
/// </summary>
/// <remarks>
/// <para>
/// The self-check is the reason this class has any code in it. Everything in
/// <c>SupportConsole.Vision</c> is measured against frames that were drawn by
/// <c>SupportConsole.Frames</c>, and drawn frames prove only that the deciding
/// is right about drawings. Run with <c>--check</c>, the application puts the
/// practice machine through its six states, reads each one the way it would
/// read a remote session, and fails if any of them comes back as something
/// other than what it was told to be.
/// </para>
/// <para>
/// That closes the gap the tests cannot: from a real window, rendered by the
/// real graphics stack at the real size, through the real downsample, to the
/// same answer. It writes a report rather than printing one because this is a
/// windowed application and has nowhere to print to.
/// </para>
/// </remarks>
public partial class App : Application
{
    private static readonly (PracticeMachine.Screens Screen, ScreenState Truth)[] Expected =
    [
        (PracticeMachine.Screens.InUseLight, ScreenState.InUse),
        (PracticeMachine.Screens.InUseDark, ScreenState.InUse),
        (PracticeMachine.Screens.InUseLetterboxed, ScreenState.InUse),
        (PracticeMachine.Screens.LockedPhoto, ScreenState.Locked),
        (PracticeMachine.Screens.LockedDark, ScreenState.Locked),
        (PracticeMachine.Screens.Black, ScreenState.Black),
    ];

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // The invariant culture, set rather than compiled in. The build
        // property that does this globally turns off the tables WPF looks a
        // font up in, and the application will not draw a character without
        // them — so the setting that made the numbers reproducible stopped the
        // window opening. Here it reaches the formatting and nothing else.
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var check = Array.IndexOf(e.Args, "--check");
        var shot = Array.IndexOf(e.Args, "--shot");

        if (check >= 0)
        {
            Shutdown(CheckTheWholeLoop(check + 1 < e.Args.Length ? e.Args[check + 1] : "check.txt"));
            return;
        }

        if (shot >= 0)
        {
            Shutdown(Shots.Take(shot + 1 < e.Args.Length ? e.Args[shot + 1] : "docs"));
            return;
        }

        new MainWindow().Show();
    }

    private static int CheckTheWholeLoop(string report)
    {
        var said = new StringBuilder();
        var wrong = 0;

        // Off to one side rather than hidden: a window that is not being drawn
        // is a window with nothing to capture.
        var machine = new PracticeMachine { Left = -4000, Top = -4000, ShowInTaskbar = false };
        machine.Show();

        foreach (var (screen, truth) in Expected)
        {
            machine.Pretend(screen);
            machine.UpdateLayout();

            var frame = Capture.Of(machine.Screenful);
            var reading = frame is null ? ScreenReading.Nothing : Detector.Read(frame);

            var right = reading.State == truth;
            if (!right) wrong++;

            said.AppendLine(
                $"{screen,-20} expected {truth,-7} read {reading.State,-7} {(right ? "ok" : "WRONG")}  {reading.Because}");

            if (!right && frame is not null)
            {
                var bottom = Signals.LastRowWithContent(frame);
                said.AppendLine($"    last row with content {bottom} of {frame.Height - 1}");

                for (var tall = 2; tall <= 8; tall++)
                {
                    var stripFrom = bottom - tall + 1;
                    var aboveTo = stripFrom - 1;
                    var aboveFrom = aboveTo - tall + 1;
                    if (aboveFrom < 0) break;

                    var strip = frame.MeanOfRows(stripFrom, bottom);
                    var above = frame.MeanOfRows(aboveFrom, aboveTo);
                    var straight = Signals.HorizontalEdge(frame, stripFrom);
                    var (filled, spread) = Signals.SlicesWithStructure(frame, stripFrom, bottom);

                    said.AppendLine(
                        $"    tall {tall}: strip {strip,6:F1} above {above,6:F1} step {Math.Abs(strip - above),6:F1} " +
                        $"straight {straight:F2} filled {filled,2} spread {spread,2}");
                }
            }
        }

        machine.Close();

        said.AppendLine();
        said.AppendLine(wrong == 0
            ? $"All {Expected.Length} states of the practice machine read as what they are."
            : $"{wrong} of {Expected.Length} states read as something else.");

        File.WriteAllText(report, said.ToString());

        return wrong == 0 ? 0 : 1;
    }
}
