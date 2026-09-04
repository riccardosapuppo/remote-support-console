namespace SupportConsole.App;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

/// <summary>
/// A window that pretends to be a machine somebody is supporting.
/// </summary>
/// <remarks>
/// <para>
/// It exists so the console has something to read that nobody has to trust it
/// with. The six states are the ones the drawn corpus covers, which makes the
/// application checkable against the measurement: put it in
/// <c>In use, dark on dark</c>, and the console should say the same thing the
/// test says about the frame of that name.
/// </para>
/// <para>
/// The two states worth switching to are the ones that were wrong on real
/// machines. Dark on dark paints the bar in the wallpaper's own colour, so
/// there is no edge under it. Letterboxed puts black above and below, so the
/// bottom of the window stops being the bottom of the picture.
/// </para>
/// </remarks>
public partial class PracticeMachine : Window
{
    private static readonly Color PaleTop = Color.FromRgb(0x8E, 0x9A, 0xAE);
    private static readonly Color PaleBottom = Color.FromRgb(0x53, 0x60, 0x75);
    private static readonly Color DarkTop = Color.FromRgb(0x18, 0x1A, 0x1F);
    private static readonly Color DarkBottom = Color.FromRgb(0x0F, 0x11, 0x14);

    private ulong seed = 7;

    private Screens showing = Screens.InUseLight;

    /// <summary>Open the practice machine, showing a desktop in use.</summary>
    public PracticeMachine()
    {
        InitializeComponent();

        DrawTaskbar();
        DrawWindowContents();
        Pretend(Screens.InUseLight);
    }

    /// <summary>What the practice machine is currently pretending to be.</summary>
    public enum Screens
    {
        /// <summary>A desktop, pale theme. The case everything handles.</summary>
        InUseLight,

        /// <summary>A desktop whose taskbar is the colour of its wallpaper.</summary>
        InUseDark,

        /// <summary>A desktop centred inside black bars.</summary>
        InUseLetterboxed,

        /// <summary>A sign-in screen over a bright wallpaper.</summary>
        LockedPhoto,

        /// <summary>A sign-in screen over a dark one.</summary>
        LockedDark,

        /// <summary>Connected, nothing drawn.</summary>
        Black,
    }

    /// <summary>
    /// The element the console reads: the picture, and not the frame around it.
    /// </summary>
    /// <remarks>
    /// The border belongs to the client, not to the session. Reading it in
    /// costs a line of grey along all four edges, which is enough to stop a
    /// black screen being black — and a black screen that is not recognised as
    /// black is one that gets Ctrl+Alt+Del while it is still opening.
    /// </remarks>
    public FrameworkElement Screenful => Picture;

    private void ModeChosen(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized || sender is not RadioButton chosen || chosen.Tag is not string name) return;

        var asked = Enum.Parse<Screens>(name);

        // Pretend ticks the button itself, so that setting the state from code
        // and clicking it look the same afterwards. Coming back the other way
        // it would recur, and this is where it stops.
        if (asked != showing) Pretend(asked);
    }

    /// <summary>Put the machine into one of its six states.</summary>
    /// <param name="screen">What to pretend to be.</param>
    public void Pretend(Screens screen)
    {
        foreach (var button in Modes.Children.OfType<RadioButton>())
        {
            button.IsChecked = (string?)button.Tag == screen.ToString();
        }

        var locked = screen is Screens.LockedPhoto or Screens.LockedDark;
        var black = screen == Screens.Black;
        var dark = screen is Screens.InUseDark or Screens.LockedDark;

        Wallpaper.Visibility = black ? Visibility.Hidden : Visibility.Visible;
        Desktop.Visibility = locked || black ? Visibility.Collapsed : Visibility.Visible;
        SignIn.Visibility = locked ? Visibility.Visible : Visibility.Collapsed;
        Taskbar.Visibility = locked || black ? Visibility.Collapsed : Visibility.Visible;

        WallpaperTop.Color = dark ? DarkTop : PaleTop;
        WallpaperBottom.Color = dark ? DarkBottom : PaleBottom;

        // Dark on dark: the bar is painted the wallpaper's own colour, and the
        // only thing left to find is the icons on it.
        Taskbar.Background = new SolidColorBrush(
            dark ? DarkBottom : Color.FromRgb(0xDC, 0xE0, 0xE6));

        var icon = new SolidColorBrush(dark ? Color.FromRgb(0xE4, 0xE8, 0xEE) : Color.FromRgb(0x3E, 0x44, 0x4E));
        foreach (var shape in Icons.Children.OfType<Shape>()) shape.Fill = icon;
        foreach (var shape in Tray.Children.OfType<Shape>()) shape.Fill = icon;

        WindowLeft.Background = new SolidColorBrush(
            dark ? Color.FromRgb(0x22, 0x25, 0x2A) : Color.FromRgb(0xED, 0xEF, 0xF2));
        WindowRight.Background = new SolidColorBrush(
            dark ? Color.FromRgb(0x1C, 0x1F, 0x24) : Color.FromRgb(0xF7, 0xF8, 0xFA));

        var ink = new SolidColorBrush(dark ? Color.FromRgb(0xBE, 0xC5, 0xD0) : Color.FromRgb(0x3A, 0x40, 0x4A));
        foreach (var shape in Ink.Children.OfType<Shape>()) shape.Fill = ink;

        var bars = screen == Screens.InUseLetterboxed ? 36 : 0;
        BarAbove.Height = new GridLength(bars);
        BarBelow.Height = new GridLength(bars);

        showing = screen;
    }

    private void DrawTaskbar()
    {
        for (var i = 0; i < 7; i++)
        {
            Icons.Children.Add(new Rectangle { Width = 14, Height = 14, Margin = new Thickness(0, 0, 8, 0) });
        }

        Tray.Children.Add(new Rectangle { Width = 10, Height = 10, Margin = new Thickness(0, 0, 10, 0) });
        Tray.Children.Add(new Rectangle { Width = 38, Height = 11 });
    }

    /// <summary>
    /// Lines of something that reads like text, so the windows have detail in
    /// them.
    /// </summary>
    /// <remarks>
    /// Seeded rather than random, for the same reason the drawn corpus is: this
    /// window is meant to be comparable with a measured frame, and a window that
    /// is different every time it opens is not comparable with anything.
    /// </remarks>
    private void DrawWindowContents()
    {
        DrawLines(60, 54, 246, 126, 9);
        DrawLines(358, 88, 220, 144, 9);
    }

    private void DrawLines(double left, double top, double wide, double tall, double gap)
    {
        for (var y = top; y < top + tall; y += gap)
        {
            var x = left;

            while (x < left + wide)
            {
                var run = 12 + (Next() * 46);
                if (x + run > left + wide) run = left + wide - x;

                var line = new Rectangle { Width = Math.Max(4, run), Height = 4 };
                Canvas.SetLeft(line, x);
                Canvas.SetTop(line, y);
                Ink.Children.Add(line);

                x += run + 6 + (Next() * 12);
            }
        }
    }

    private double Next()
    {
        seed ^= seed << 13;
        seed ^= seed >> 7;
        seed ^= seed << 17;
        return (seed >> 11) / (double)(1UL << 53);
    }
}
