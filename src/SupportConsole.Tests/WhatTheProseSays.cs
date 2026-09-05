namespace SupportConsole.Tests;

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

using SupportConsole.Frames;
using SupportConsole.Vision;
using Xunit;

/// <summary>
/// The figures this repository says out loud, against the ones it computes.
/// </summary>
/// <remarks>
/// <para>
/// Everything else in this suite reads the code. These read the prose, because
/// the prose is where the numbers here have actually gone wrong, and not one of
/// them was wrong the day it was written. A figure copied into a sentence is
/// true once: after that the compiler cannot look at it, the runner cannot look
/// at it, and a reader has no way to tell a measurement from the memory of one.
/// </para>
/// <para>
/// CI already does this to the block at the foot of the README, by running the
/// measurement and diffing its output against the fenced block. What follows is
/// the same idea for the figures quoted in the middle of a sentence, where a
/// diff has nothing to line up against.
/// </para>
/// </remarks>
public class WhatTheProseSays
{
    private static readonly string Root = TheRepository();

    // Spoken, because prose spells a small number out. This is a translation of
    // the cadence rather than a second copy of it: the cadence itself is the
    // one constant in MainWindow, and every sentence about it is checked
    // against that.
    private static readonly string[] Spoken =
    [
        string.Empty,
        "once a second",
        "twice a second",
        "three times a second",
        "four times a second",
        "five times a second",
        "six times a second",
        "seven times a second",
        "eight times a second",
        "nine times a second",
        "ten times a second",
    ];

    /// <summary>
    /// The three detail figures, in all three places that quote them.
    /// </summary>
    /// <param name="file">The file, relative to the repository.</param>
    /// <remarks>
    /// All three rather than the README alone, because the case this was
    /// written for was a README that was right and two comments beside the code
    /// that were an old corpus out of date. The comment is what the next person
    /// reads before touching the detector.
    /// </remarks>
    [Theory]
    [InlineData("README.md")]
    [InlineData("src/SupportConsole.Vision/Detector.cs")]
    [InlineData("src/SupportConsole.Vision/Signals.cs")]
    public void TheDetailFiguresInTheProseAreTheOnesTheCorpusGives(string file)
    {
        var inUse = Corpus.All
            .Where(one => one.Truth == ScreenState.InUse)
            .Select(one => Signals.EdgeEnergy(one.Frame))
            .ToList();

        var said = Regex.Match(
            Prose(file),
            @"a lock screen over the stock photograph reads ([0-9]+\.[0-9]), " +
            @"one on a dark background reads ([0-9]+\.[0-9]), " +
            @"and a desktop in use reads ([0-9]+\.[0-9]) to ([0-9]+\.[0-9])");

        // An expression that matches nothing makes every comparison after it
        // pass, which is the shape of a check that is not one.
        Assert.True(said.Success, $"{file} no longer quotes the detail figures in a shape this check can read.");

        Assert.Equal(Figure(Detail("locked-photo")), said.Groups[1].Value);
        Assert.Equal(Figure(Detail("locked-dark")), said.Groups[2].Value);
        Assert.Equal(Figure(inUse.Min()), said.Groups[3].Value);
        Assert.Equal(Figure(inUse.Max()), said.Groups[4].Value);
    }

    /// <summary>
    /// The cadence the README quotes is the one the console keeps.
    /// </summary>
    [Fact]
    public void TheCadenceTheReadmeQuotesIsTheOneTheClockKeeps()
    {
        var set = Regex.Match(Text("src/SupportConsole.App/MainWindow.xaml.cs"), @"ReadEveryMs = ([0-9]+);");

        Assert.True(set.Success, "MainWindow no longer has one place that says how often it looks.");

        var every = int.Parse(set.Groups[1].Value, CultureInfo.InvariantCulture);

        // A cadence that does not divide a second cannot be said in English
        // without rounding it, and the rounding is where this went wrong: 200
        // ms was described as four times a second, in two places, for as long
        // as nobody divided it.
        Assert.Equal(0, 1000 % every);
        Assert.InRange(1000 / every, 1, Spoken.Length - 1);

        var spoken = Spoken[1000 / every];
        var readme = Prose("README.md");

        Assert.Contains(spoken, readme, StringComparison.Ordinal);

        // And no second cadence left standing beside it. The window's own line
        // needs no check here: it is built out of the same constant.
        foreach (var other in Spoken.Skip(1).Where(one => one != spoken))
        {
            Assert.DoesNotContain(other, readme, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// The number of checks the README quotes is the number there are.
    /// </summary>
    [Fact]
    public void TheNumberOfChecksTheReadmeQuotesIsTheNumberThereAre()
    {
        var quoted = Regex.Matches(Text("README.md"), @"([0-9]+) checks");

        // The README says it in two places. Both are compared, and finding
        // neither of them is a failure rather than a quiet pass.
        Assert.True(
            quoted.Count >= 2,
            "The README has stopped saying how many checks there are, so nothing was compared.");

        var counted = ChecksInHere().ToString(CultureInfo.InvariantCulture);

        foreach (Match one in quoted) Assert.Equal(counted, one.Groups[1].Value);
    }

    private static double Detail(string name) =>
        Signals.EdgeEnergy(Corpus.All.Single(one => one.Name == name).Frame);

    // One decimal place, and a point rather than a comma whatever the machine
    // is set to. The measurement runs invariant and prints a point; a check
    // that formatted the number the local way would pass in London and fail in
    // Milan, on the same corpus.
    private static string Figure(double value) => value.ToString("F1", CultureInfo.InvariantCulture);

    // Counting the suite from inside it, which is not elegant and is the only
    // copy of this number that cannot go stale. A check added without touching
    // the README fails here, instead of leaving the README quietly wrong.
    private static int ChecksInHere()
    {
        var total = 0;

        foreach (var type in typeof(WhatTheProseSays).Assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.GetCustomAttribute<FactAttribute>() is null) continue;

                var cases = method.GetCustomAttributes<InlineDataAttribute>().Count();

                foreach (var data in method.GetCustomAttributes<MemberDataAttribute>())
                {
                    cases += Rows(data.MemberType ?? type, data.MemberName);
                }

                // A fact is one check. A theory is as many as it has rows.
                total += Math.Max(cases, 1);
            }
        }

        return total;
    }

    private static int Rows(Type type, string name)
    {
        var found =
            type.GetMethod(name, BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null)
            ?? type.GetProperty(name, BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
            ?? type.GetField(name, BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

        if (found is not IEnumerable rows)
        {
            throw new InvalidOperationException($"{type.Name}.{name} is not something a theory can be run over.");
        }

        return rows.Cast<object>().Count();
    }

    // Up from wherever the assembly was put, until the README turns up. These
    // checks read the repository the way a reader does, so they find it the
    // same way rather than being handed a path by the build.
    private static string TheRepository()
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);

        while (here is not null && !File.Exists(Path.Combine(here.FullName, "README.md"))) here = here.Parent;

        return here?.FullName
            ?? throw new InvalidOperationException(
                $"No README.md anywhere above {AppContext.BaseDirectory}, so there is no prose to check.");
    }

    private static string Text(string file) => File.ReadAllText(Path.Combine(Root, file));

    // The comment markers and the bold are not part of what a sentence says,
    // and neither is where the line happened to be wrapped. Everything else is
    // left exactly as it was written.
    private static string Prose(string file) =>
        Regex.Replace(
            Text(file)
                .Replace("///", " ", StringComparison.Ordinal)
                .Replace("<b>", string.Empty, StringComparison.Ordinal)
                .Replace("</b>", string.Empty, StringComparison.Ordinal),
            @"\s+",
            " ");
}
