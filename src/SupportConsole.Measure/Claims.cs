namespace SupportConsole.Measure;

using SupportConsole.Frames;
using SupportConsole.Vision;

/// <summary>
/// Something this project says about itself, and whether it is still true.
/// </summary>
/// <param name="Title">The claim, in one line.</param>
/// <param name="Holds">Whether the corpus still bears it out.</param>
/// <param name="Lines">The working, printed under it.</param>
public sealed record Claim(string Title, bool Holds, IReadOnlyList<string> Lines);

/// <summary>
/// The claims, computed rather than written down.
/// </summary>
/// <remarks>
/// <para>
/// Every number in this project's README comes out of here, and the build runs
/// it. That is the only arrangement in which a README stays true: a figure
/// typed into prose is a figure that was true once.
/// </para>
/// <para>
/// Each claim is also falsifiable in the direction that matters. Not "the
/// detector works" — that cannot fail — but "no threshold over this number
/// classifies these frames", which stops being true the moment somebody finds
/// one.
/// </para>
/// </remarks>
public static class Claims
{
    /// <summary>Work out all three.</summary>
    /// <returns>The claims, in the order they are printed.</returns>
    public static IReadOnlyList<Claim> All() =>
    [
        WhatTheRuleGetsRight(),
        NoThresholdOverDetail(),
        WhereTheClickLands(),
    ];

    /// <summary>
    /// The first claim: the new rule reads the corpus, the old one does not, and
    /// the rule that was thrown away trades one frame for a worse one.
    /// </summary>
    private static Claim WhatTheRuleGetsRight()
    {
        var lines = new List<string>
        {
            $"{"frame",-24}{"truth",-8}{"now",-8}{"first version",-15}{"rejected rule",-15}",
            new('-', 70),
        };

        int now = 0, then = 0, strict = 0;
        int nowDangerous = 0, thenDangerous = 0, strictDangerous = 0;

        foreach (var one in Corpus.All)
        {
            var a = Detector.Read(one.Frame).State;
            var b = FirstVersion.Read(one.Frame).State;
            var c = WithRejectedRule(one.Frame);

            if (a == one.Truth) now++;
            if (b == one.Truth) then++;
            if (c == one.Truth) strict++;

            // The expensive direction: a machine somebody is working in, read as
            // one waiting for a password.
            if (one.Truth == ScreenState.InUse)
            {
                if (a != ScreenState.InUse) nowDangerous++;
                if (b != ScreenState.InUse) thenDangerous++;
                if (c != ScreenState.InUse) strictDangerous++;
            }

            lines.Add(
                $"{one.Name,-24}{one.Truth,-8}{Mark(a, one.Truth),-8}{Mark(b, one.Truth),-15}{Mark(c, one.Truth),-15}");
        }

        var total = Corpus.All.Count;

        lines.Add(new string('-', 70));
        lines.Add($"{"right",-24}{"",-8}{now + "/" + total,-8}{then + "/" + total,-15}{strict + "/" + total,-15}");
        lines.Add(
            $"{"in use, read as locked",-24}{"",-8}{nowDangerous,-8}{thenDangerous,-15}{strictDangerous,-15}");
        lines.Add(string.Empty);
        lines.Add("  The first version misses two desktops somebody was working in: the dark");
        lines.Add("  taskbar it could find no edge under, and the one inside black bars. It also");
        lines.Add("  calls a black screen locked, which is a Ctrl+Alt+Del at a session still opening.");
        lines.Add(string.Empty);
        lines.Add("  The rejected rule scores the same as the current one and is not the same:");
        lines.Add("  it trades locked-bright-band, where being wrong wastes a click, for");
        lines.Add("  in-use-busy-wallpaper, where being wrong types a password into somebody's");
        lines.Add("  session. Two rules with one error each are not two rules of equal worth.");

        var holds = now == total - 1
            && nowDangerous == 0
            && thenDangerous == 2
            && strictDangerous == 1;

        return new Claim(
            "The rule that survived reads every frame but one, and never the expensive way round",
            holds,
            lines);
    }

    /// <summary>
    /// The second claim: detail was believed, measured, and is useless — and
    /// this is the arithmetic that says so.
    /// </summary>
    /// <remarks>
    /// Not "the ranges overlap", which is an impression. Every threshold that
    /// could be drawn is tried, in both directions, and the best one is
    /// reported. A claim shaped like this can be beaten by anybody who finds a
    /// better threshold, which is what makes it worth printing.
    /// </remarks>
    private static Claim NoThresholdOverDetail()
    {
        // Black is not in this: it is decided before any of it, and including it
        // would flatter the number.
        var judged = Corpus.All.Where(one => one.Truth != ScreenState.Black).ToList();

        var lines = new List<string> { $"{"frame",-24}{"truth",-8}detail" };
        lines.Add(new string('-', 44));

        foreach (var one in judged.OrderBy(one => Signals.EdgeEnergy(one.Frame)))
        {
            lines.Add($"{one.Name,-24}{one.Truth,-8}{Signals.EdgeEnergy(one.Frame),6:F1}");
        }

        var values = judged.Select(one => Signals.EdgeEnergy(one.Frame)).OrderBy(v => v).ToList();

        var cuts = new List<double> { values[0] - 1 };
        for (var i = 1; i < values.Count; i++) cuts.Add((values[i - 1] + values[i]) / 2);
        cuts.Add(values[^1] + 1);

        var best = int.MaxValue;
        var bestCut = 0.0;
        var bestWayRound = string.Empty;
        var bestDangerous = 0;

        foreach (var cut in cuts)
        {
            foreach (var busyMeansInUse in new[] { true, false })
            {
                var wrong = 0;
                var dangerous = 0;

                foreach (var one in judged)
                {
                    var above = Signals.EdgeEnergy(one.Frame) > cut;
                    var said = above == busyMeansInUse ? ScreenState.InUse : ScreenState.Locked;

                    if (said == one.Truth) continue;

                    wrong++;
                    if (one.Truth == ScreenState.InUse) dangerous++;
                }

                if (wrong >= best) continue;

                best = wrong;
                bestCut = cut;
                bestDangerous = dangerous;
                bestWayRound = busyMeansInUse ? "more detail means in use" : "less detail means in use";
            }
        }

        lines.Add(new string('-', 44));
        lines.Add(string.Empty);
        lines.Add($"  Thresholds tried: {cuts.Count * 2}, which is every cut this corpus can distinguish,");
        lines.Add("  in both directions.");
        lines.Add($"  The best of them is {bestCut:F1} ({bestWayRound}) and it still reads");
        lines.Add($"  {best} of {judged.Count} frames wrong, {bestDangerous} of them the expensive way round.");
        lines.Add($"  The rule that is shipped reads 1 of {judged.Count} wrong and does not use this number at all.");
        lines.Add(string.Empty);
        lines.Add("  What it measures is how detailed the wallpaper is. A lock screen over a");
        lines.Add("  photograph is busier than a desktop with two windows open on flat grey, and");
        lines.Add("  no amount of choosing the cut carefully repairs that.");

        return new Claim(
            "No threshold over how detailed the screen is can tell locked from in use",
            best >= 3,
            lines);
    }

    /// <summary>
    /// The third claim: where each of the three searches for the password box
    /// actually clicks, on a screen where they disagree.
    /// </summary>
    private static Claim WhereTheClickLands()
    {
        var lines = new List<string>
        {
            $"{"frame",-24}{"the box",-18}{"by shape",-12}{"at 54%",-12}{"by size",-12}",
            new('-', 78),
        };

        int shape = 0, fixedHeight = 0, size = 0;

        foreach (var one in Corpus.SignIn)
        {
            var field = one.Field!.Value;

            var byShape = PasswordBox.FindIn(one.Frame);
            var atFixed = FirstVersion.ClickWhereTheBoxUsuallyIs(one.Frame);
            var bySize = FirstVersion.LargestPaleThing(one.Frame);

            if (LandsInside(byShape, one.Frame, field)) shape++;
            if (LandsInside(atFixed, one.Frame, field)) fixedHeight++;
            if (LandsInside(bySize, one.Frame, field)) size++;

            lines.Add(
                $"{one.Name,-24}{$"row {field.Row}, {field.From}-{field.To}",-18}" +
                $"{Where(byShape, one.Frame, field),-12}{Where(atFixed, one.Frame, field),-12}" +
                $"{Where(bySize, one.Frame, field),-12}");
        }

        var total = Corpus.SignIn.Count;

        lines.Add(new string('-', 78));
        lines.Add($"{"in the box",-24}{"",-18}{shape + "/" + total,-12}{fixedHeight + "/" + total,-12}{size + "/" + total,-12}");
        lines.Add(string.Empty);
        lines.Add("  The two that fail, fail on the same frame and for different reasons.");
        lines.Add("  A fixed 54% of the height is a fact about Windows 10; on Server 2012 the box");
        lines.Add("  is at 39% and the click lands on the wallpaper, so the password is typed into");
        lines.Add("  nothing. Taking the largest pale rectangle finds the user photograph, which");
        lines.Add("  is 20 by 21 against the box's 34 by 4 — larger, and the wrong shape.");
        lines.Add("  A text field is much wider than it is tall, and that one constraint is the");
        lines.Add("  whole of the difference.");

        return new Claim(
            "The box is found by its shape, on a screen where position and size both miss it",
            shape == total && fixedHeight == total - 1 && size == total - 1,
            lines);
    }

    private static ScreenState WithRejectedRule(RemoteFrame frame) =>
        frame.IsBlack
            ? ScreenState.Black
            : Detector.HasTaskbar(frame, requireStructureToStop: true) ? ScreenState.InUse : ScreenState.Locked;

    private static string Mark(ScreenState said, ScreenState truth) =>
        said == truth ? said.ToString() : said + " X";

    private static bool LandsInside(Spot? spot, RemoteFrame frame, (int Row, int From, int To) field)
    {
        if (spot is null) return false;

        var x = (int)(spot.Value.X * frame.Width);
        var y = (int)(spot.Value.Y * frame.Height);

        return x >= field.From && x <= field.To && y >= field.Row && y <= field.Row + 3;
    }

    private static string Where(Spot? spot, RemoteFrame frame, (int Row, int From, int To) field)
    {
        if (spot is null) return "nothing";

        var x = (int)(spot.Value.X * frame.Width);
        var y = (int)(spot.Value.Y * frame.Height);

        return $"{x},{y}" + (LandsInside(spot, frame, field) ? string.Empty : " X");
    }
}
