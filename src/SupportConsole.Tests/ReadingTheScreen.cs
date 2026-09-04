namespace SupportConsole.Tests;

using SupportConsole.Frames;
using SupportConsole.Measure;
using SupportConsole.Vision;
using Xunit;

/// <summary>
/// The corpus, one frame per check, plus the two rules it exists to settle.
/// </summary>
public class ReadingTheScreen
{
    public static TheoryData<string> EveryFrame()
    {
        var data = new TheoryData<string>();
        foreach (var one in Corpus.All) data.Add(one.Name);
        return data;
    }

    private static Case Find(string name) => Corpus.All.Single(one => one.Name == name);

    [Theory]
    [MemberData(nameof(EveryFrame))]
    public void TheDetectorAgreesWithTheTruth(string name)
    {
        var one = Find(name);

        // locked-bright-band is the known miss, and it is in the corpus because
        // it is a miss. Asserting it away would be the point of the corpus,
        // undone: see the note on Detector about the rule that would fix it.
        if (name == "locked-bright-band")
        {
            Assert.Equal(ScreenState.InUse, Detector.Read(one.Frame).State);
            return;
        }

        Assert.Equal(one.Truth, Detector.Read(one.Frame).State);
    }

    [Theory]
    [MemberData(nameof(EveryFrame))]
    public void NoDesktopInUseIsEverReadAsLocked(string name)
    {
        var one = Find(name);

        if (one.Truth != ScreenState.InUse) return;

        // The whole asymmetry, as one assertion. Locked read as in use is a
        // wasted click. In use read as locked is Ctrl+Alt+Del and a password
        // typed into somebody's session.
        Assert.Equal(ScreenState.InUse, Detector.Read(one.Frame).State);
    }

    [Fact]
    public void BlackIsItsOwnAnswerAndNotAKindOfLocked()
    {
        var reading = Detector.Read(Find("black").Frame);

        Assert.Equal(ScreenState.Black, reading.State);
        Assert.False(reading.SomebodyIsLoggedIn);
    }

    [Fact]
    public void LoggedInMeansPositiveEvidenceAndNotTheAbsenceOfLocked()
    {
        Assert.False(Detector.Read(null).SomebodyIsLoggedIn);
        Assert.False(Detector.Read(Find("black").Frame).SomebodyIsLoggedIn);
        Assert.False(Detector.Read(Find("locked-photo").Frame).SomebodyIsLoggedIn);
        Assert.True(Detector.Read(Find("in-use-dark-on-dark").Frame).SomebodyIsLoggedIn);
    }

    [Fact]
    public void ADarkTaskbarOnADarkWallpaperIsStillATaskbar()
    {
        // There is no edge to find here: the bar is the colour of the wallpaper.
        // What gives it away is that the icons lift the mean of the band.
        var frame = Find("in-use-dark-on-dark").Frame;

        Assert.True(Signals.HorizontalEdge(frame, frame.Height - 5) < Detector.StraightEnough);
        Assert.True(Detector.HasTaskbar(frame));
    }

    [Fact]
    public void TheBottomOfTheFrameIsNotTheBottomOfThePicture()
    {
        var frame = Find("in-use-letterboxed").Frame;

        Assert.True(Signals.LastRowWithContent(frame) < frame.Height - 1);
        Assert.True(Detector.HasTaskbar(frame));
    }

    [Fact]
    public void ThreeIconsInOneCornerAreNotATaskbar()
    {
        // A lock screen has structure at the bottom too. What it does not have
        // is structure at both ends of the screen.
        var frame = Find("locked-flat").Frame;
        var (filled, spread) = Signals.SlicesWithStructure(frame, frame.Height - 9, frame.Height - 1);

        Assert.True(spread < 6);
        Assert.False(Detector.HasTaskbar(frame));
        Assert.True(filled >= 1);
    }

    [Fact]
    public void TheRejectedRuleTradesOneFrameForAWorseOne()
    {
        var band = Find("locked-bright-band").Frame;
        var busy = Find("in-use-busy-wallpaper").Frame;

        // It fixes the safe error...
        Assert.True(Detector.HasTaskbar(band));
        Assert.False(Detector.HasTaskbar(band, requireStructureToStop: true));

        // ...and creates the expensive one.
        Assert.True(Detector.HasTaskbar(busy));
        Assert.False(Detector.HasTaskbar(busy, requireStructureToStop: true));
    }

    [Fact]
    public void TheFirstVersionStillMakesBothOfItsMistakes()
    {
        // The old rule is kept runnable so the claim can be checked rather than
        // believed. If this ever passes, the claim in the README is worthless.
        Assert.Equal(ScreenState.Locked, FirstVersion.Read(Find("in-use-dark-on-dark").Frame).State);
        Assert.Equal(ScreenState.Locked, FirstVersion.Read(Find("in-use-letterboxed").Frame).State);
        Assert.Equal(ScreenState.Locked, FirstVersion.Read(Find("black").Frame).State);
    }

    [Fact]
    public void EveryClaimTheReadmeMakesStillHolds()
    {
        foreach (var claim in Claims.All()) Assert.True(claim.Holds, claim.Title);
    }
}
