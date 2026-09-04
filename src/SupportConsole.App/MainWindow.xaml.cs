namespace SupportConsole.App;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

using SupportConsole.Frames;
using SupportConsole.Vision;

/// <summary>
/// The console: what it is looking at, what it decided, and what the version
/// before it would have decided about the same picture.
/// </summary>
/// <remarks>
/// The last of those is the reason the window is laid out this way. A screen
/// reading is a guess, and a guess is only worth showing next to the evidence
/// and next to what it replaced.
/// </remarks>
public partial class MainWindow : Window
{
    private readonly DispatcherTimer clock = new() { Interval = TimeSpan.FromMilliseconds(200) };

    private PracticeMachine? practice;

    /// <summary>Open the console, showing the first frame of the corpus.</summary>
    public MainWindow()
    {
        InitializeComponent();

        var sources = new List<Source> { new("practice machine", "live, from the window next to this one", null) };
        sources.AddRange(Corpus.All.Select(one => new Source(one.Name, one.Story, one)));

        Sources.ItemsSource = sources;
        Sources.SelectedIndex = 1;

        clock.Tick += (_, _) => ReadPracticeMachine();
    }

    /// <summary>One row of the list on the left.</summary>
    /// <param name="Name">What it is called.</param>
    /// <param name="Note">Why it is in the list.</param>
    /// <param name="Drawn">The drawn frame, or null for the live window.</param>
    public sealed record Source(string Name, string Note, Case? Drawn);

    /// <summary>Show one named frame, for the pictures in the README.</summary>
    /// <param name="name">The name as it appears in the list on the left.</param>
    public void ShowSource(string name)
    {
        if (Sources.ItemsSource is not IEnumerable<Source> sources) return;

        Sources.SelectedItem = sources.FirstOrDefault(one => one.Name == name);
        UpdateLayout();
    }

    private void SourceChosen(object sender, SelectionChangedEventArgs e)
    {
        if (Sources.SelectedItem is not Source source) return;

        if (source.Drawn is null)
        {
            clock.Start();
            ReadPracticeMachine();
            return;
        }

        clock.Stop();
        Heading.Text = source.Name;
        Story.Text = source.Note;
        Read(source.Drawn.Frame);
    }

    private void OpenPracticeMachine(object sender, RoutedEventArgs e)
    {
        if (practice is null)
        {
            practice = new PracticeMachine { Owner = this };
            practice.Closed += (_, _) => practice = null;
            practice.Show();
        }

        practice.Activate();
        Sources.SelectedIndex = 0;
    }

    private void ReadPracticeMachine()
    {
        Heading.Text = "practice machine";

        if (practice is null)
        {
            Story.Text = "not open. The button at the bottom left opens it.";
            Blank();
            return;
        }

        Story.Text = "read from the window next to this one, four times a second.";

        var frame = Capture.Of(practice.Screenful);

        if (frame is null)
        {
            Blank();
            return;
        }

        Read(frame);
    }

    private void Read(RemoteFrame frame)
    {
        var reading = Detector.Read(frame);
        var before = FirstVersion.Read(frame);

        View.Source = Capture.Show(frame);

        State.Text = reading.State.ToString();
        State.Foreground = new SolidColorBrush(reading.State switch
        {
            ScreenState.InUse => Color.FromRgb(0x7F, 0xD1, 0x8A),
            ScreenState.Locked => Color.FromRgb(0xE8, 0xB4, 0x5C),
            ScreenState.Black => Color.FromRgb(0x74, 0x8A, 0xC4),
            _ => Color.FromRgb(0x85, 0x8E, 0x9B),
        });

        LoggedIn.Text = reading.SomebodyIsLoggedIn ? "yes" : "nothing says so";
        Because.Text = reading.Because;
        Detail.Text = $"{reading.EdgeEnergy:F1}";

        Before.Text = before.State == reading.State
            ? $"{before.State}, the same"
            : $"{before.State} — {before.Because}";

        Overlay.Children.Clear();

        // The click is only worth drawing when nothing says anybody is logged
        // in. On a screen somebody is working in, this is where a password
        // would have gone, and that is not a thing to show a target for.
        if (reading.SomebodyIsLoggedIn || reading.State == ScreenState.Black)
        {
            Click.Text = reading.SomebodyIsLoggedIn ? "nowhere: somebody is working here" : "nowhere: nothing drawn yet";
            return;
        }

        var spot = SupportConsole.Vision.PasswordBox.FindIn(frame);

        if (spot is null)
        {
            Click.Text = "no field found";
            return;
        }

        var x = spot.Value.X * Overlay.Width;
        var y = spot.Value.Y * Overlay.Height;

        Click.Text = $"{spot.Value.X * 100:F0}% across, {spot.Value.Y * 100:F0}% down";
        DrawCrosshair(x, y);
    }

    private void DrawCrosshair(double x, double y)
    {
        var paint = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x5B));

        var ring = new Ellipse { Width = 26, Height = 26, Stroke = paint, StrokeThickness = 2 };
        Canvas.SetLeft(ring, x - 13);
        Canvas.SetTop(ring, y - 13);
        Overlay.Children.Add(ring);

        Overlay.Children.Add(Line(x - 22, y, x - 16, y, paint));
        Overlay.Children.Add(Line(x + 16, y, x + 22, y, paint));
        Overlay.Children.Add(Line(x, y - 22, x, y - 16, paint));
        Overlay.Children.Add(Line(x, y + 16, x, y + 22, paint));
    }

    private static Line Line(double x1, double y1, double x2, double y2, Brush paint) =>
        new() { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = paint, StrokeThickness = 2 };

    private void Blank()
    {
        View.Source = null;
        Overlay.Children.Clear();
        State.Text = "—";
        LoggedIn.Text = "—";
        Because.Text = "no frame";
        Detail.Text = "—";
        Before.Text = "—";
        Click.Text = "—";
    }
}
