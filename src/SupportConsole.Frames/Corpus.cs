namespace SupportConsole.Frames;

using SupportConsole.Vision;

/// <summary>
/// One frame, and what it really is.
/// </summary>
/// <param name="Name">Short, and used as the row label in the measurement.</param>
/// <param name="Truth">What a person looking at this screen would say.</param>
/// <param name="Story">Why this frame is in the corpus at all.</param>
/// <param name="Frame">The picture.</param>
/// <param name="Field">
/// Where the password box is, in cells, or null when there is no sign-in screen
/// — as (row, first column, last column). Written down here because it was
/// drawn here, which is the only way to check a search that is meant to find it.
/// </param>
public sealed record Case(
    string Name,
    ScreenState Truth,
    string Story,
    RemoteFrame Frame,
    (int Row, int From, int To)? Field = null);

/// <summary>
/// The screens every claim in this project is measured against.
/// </summary>
/// <remarks>
/// <para>
/// Eleven frames, and each one is here because something went wrong on it.
/// Five reproduce a specific mistake made on a real machine — a dark taskbar
/// with no edge to find, a desktop centred inside black bars, a wallpaper
/// detailed all the way to the bottom, a lit strip that reads as a taskbar, a
/// sign-in box that is not where the other version of Windows puts it. The
/// rest are the ordinary cases that have to keep working while those are fixed.
/// </para>
/// <para>
/// They are drawn, not photographed. A screenshot of a real machine is
/// somebody else's desktop and cannot be published; a drawing can be varied one
/// axis at a time, which is the difference between a demonstration and a
/// measurement. Nothing here reads a clock or an unseeded random number, so the
/// corpus is the same on every machine and a README can quote a figure from it.
/// </para>
/// </remarks>
public static class Corpus
{
    /// <summary>Every frame, in the order the measurement prints them.</summary>
    public static IReadOnlyList<Case> All { get; } = Build();

    /// <summary>Just the sign-in screens, which are the ones with a box to find.</summary>
    public static IReadOnlyList<Case> SignIn { get; } =
        All.Where(one => one.Field is not null).ToList();

    private static List<Case> Build() =>
    [
        new(
            "black",
            ScreenState.Black,
            "connected, nothing drawn yet. Its own answer, not a kind of locked.",
            new Paint(seed: 11).Flat(0).Done()),

        new(
            "locked-flat",
            ScreenState.Locked,
            "the plain case: a sign-in screen on a flat colour.",
            SignInScreen(new Paint(seed: 12).Flat(64), boxRow: 58),
            (58, 79, 112)),

        new(
            "locked-photo",
            ScreenState.Locked,
            "locked over the stock photograph: soft, but detailed enough to read as busier than a desktop in use.",
            SignInScreen(new Paint(seed: 13).Gradient(150, 60).Clouds(70, scale: 20).Speckle(38, every: 4), boxRow: 58),
            (58, 79, 112)),

        new(
            "locked-dark",
            ScreenState.Locked,
            "locked on a dark background: the least detailed frame, with the same truth as the most detailed one.",
            SignInScreen(new Paint(seed: 14).Flat(22).Clouds(10, scale: 30), boxRow: 58),
            (58, 79, 112)),

        new(
            "locked-server-2012",
            ScreenState.Locked,
            "the box at 39% of the height instead of 54%, under a user photograph larger than the box.",
            Server2012(new Paint(seed: 15).Gradient(40, 90)),
            (42, 79, 112)),

        new(
            "locked-busy-wallpaper",
            ScreenState.Locked,
            "a wallpaper with detail all the way down: twelve slices out of twelve, below the supposed edge and above it.",
            SignInScreen(new Paint(seed: 16).Gradient(120, 90).Speckle(70), boxRow: 58),
            (58, 79, 112)),

        new(
            "locked-bright-band",
            ScreenState.Locked,
            "a wallpaper with a lit strip along the bottom. Both versions call this in use, and the fix costs more than it saves.",
            SignInScreen(new Paint(seed: 21).Gradient(140, 90).Box(0, 100, 192, 8, 200).Speckle(60), boxRow: 58),
            (58, 79, 112)),

        new(
            "in-use-light",
            ScreenState.InUse,
            "a desktop in a light theme: the case where everything works.",
            Desktop(new Paint(seed: 17).Gradient(170, 140), light: true, step: 40)),

        new(
            "in-use-dark-on-dark",
            ScreenState.InUse,
            "a dark taskbar on a dark wallpaper, with no edge to measure. The first detector called this locked and typed a password at it.",
            Desktop(new Paint(seed: 18).Flat(20).Clouds(8, scale: 30), light: false, step: 0)),

        new(
            "in-use-letterboxed",
            ScreenState.InUse,
            "the same desktop centred inside black bars. The bottom of the frame is not the bottom of the picture.",
            Letterboxed(new Paint(seed: 19).Gradient(170, 140))),

        new(
            "in-use-busy-wallpaper",
            ScreenState.InUse,
            "a taskbar over the wallpaper that has detail everywhere, which is the hardest frame here.",
            Desktop(new Paint(seed: 20).Gradient(120, 90).Speckle(70), light: true, step: 45)),
    ];

    /// <summary>
    /// A sign-in screen: a box 34 cells by 4, a name above it, and the three
    /// small icons that sit at the bottom right of one.
    /// </summary>
    /// <remarks>
    /// Those icons matter. They are structure at the bottom of a locked screen,
    /// and they are why the taskbar test asks how far the structure is
    /// <em>spread</em> and not only how much of it there is: all three of them
    /// fall inside one slice out of twelve.
    /// </remarks>
    private static RemoteFrame SignInScreen(Paint paint, int boxRow)
    {
        paint.Box(79, boxRow, 34, 4, 215);
        paint.TextLike(76, boxRow - 9, 40, 6, 225, gap: 3);

        for (var i = 0; i < 3; i++) paint.Box(176 + (i * 5), 100, 3, 3, 200);

        return paint.Done();
    }

    /// <summary>
    /// The Server 2012 sign-in screen, with the two things that broke the first
    /// search for the box.
    /// </summary>
    /// <remarks>
    /// The box is at row 42 of 108 — 39% — where the Windows 10 screen puts it
    /// at 54%. And the user photograph above it is 20 cells by 21: larger than
    /// the 34-by-4 box by area, and square. Anything looking for the biggest
    /// pale thing in the middle of the screen picks the face.
    /// </remarks>
    private static RemoteFrame Server2012(Paint paint)
    {
        paint.Box(86, 18, 20, 21, 195);
        paint.Box(79, 42, 34, 4, 215);
        paint.TextLike(80, 48, 32, 3, 170, gap: 3);

        for (var i = 0; i < 3; i++) paint.Box(176 + (i * 5), 100, 3, 3, 200);

        return paint.Done();
    }

    /// <summary>A desktop with two windows on it and a taskbar along the bottom.</summary>
    /// <param name="paint">The wallpaper, already drawn.</param>
    /// <param name="light">
    /// Which way round the theme goes. Dark ink on pale windows, or the reverse
    /// — and the detector is not allowed to care, which is what this parameter
    /// is for.
    /// </param>
    /// <param name="step">
    /// How far the taskbar's own colour sits from the wallpaper. Zero is the
    /// case that broke the first version.
    /// </param>
    private static RemoteFrame Desktop(Paint paint, bool light, int step)
    {
        paint.Box(14, 12, 82, 46, light ? 235 : 40);
        paint.TextLike(18, 16, 74, 38, light ? 40 : 210);

        paint.Box(104, 22, 74, 52, light ? 245 : 34);
        paint.TextLike(108, 26, 66, 44, light ? 55 : 195);

        paint.Taskbar(
            light ? 150 : 20,
            tall: 5,
            step: step,
            iconBrightness: light ? 60 : 225);

        return paint.Done();
    }

    /// <summary>
    /// The same desktop, drawn small and centred, with black above and below.
    /// </summary>
    /// <remarks>
    /// What a 16:10 remote desktop looks like inside a 16:9 window, and why the
    /// search starts at the last row with anything on it rather than at the last
    /// row.
    /// </remarks>
    private static RemoteFrame Letterboxed(Paint paint)
    {
        paint.Box(14, 20, 78, 34, 235);
        paint.TextLike(18, 24, 70, 28, 40);

        paint.Box(100, 26, 76, 40, 245);
        paint.TextLike(104, 30, 68, 32, 55);

        // The taskbar of the picture, which ends nine rows above the bottom of
        // the frame it arrived in.
        paint.Box(0, 94, 192, 5, 190);

        for (var i = 0; i < 7; i++) paint.Box(3 + (i * 5), 95, 3, 3, 60);

        paint.Box(174, 95, 12, 3, 60);
        paint.Box(166, 95, 4, 3, 60);

        paint.Letterbox(9);

        return paint.Done();
    }
}
