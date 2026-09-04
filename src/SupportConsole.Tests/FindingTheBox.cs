namespace SupportConsole.Tests;

using SupportConsole.Frames;
using SupportConsole.Vision;
using Xunit;

/// <summary>
/// The click that sends the password somewhere, and the two ways of getting it
/// wrong that both happened.
/// </summary>
public class FindingTheBox
{
    public static TheoryData<string> EverySignInScreen()
    {
        var data = new TheoryData<string>();
        foreach (var one in Corpus.SignIn) data.Add(one.Name);
        return data;
    }

    private static Case Find(string name) => Corpus.SignIn.Single(one => one.Name == name);

    private static (int X, int Y) Cells(Spot spot, RemoteFrame frame) =>
        ((int)(spot.X * frame.Width), (int)(spot.Y * frame.Height));

    [Theory]
    [MemberData(nameof(EverySignInScreen))]
    public void TheClickLandsInsideTheBox(string name)
    {
        var one = Find(name);
        var field = one.Field!.Value;

        var spot = PasswordBox.FindIn(one.Frame);
        Assert.NotNull(spot);

        var (x, y) = Cells(spot.Value, one.Frame);

        Assert.InRange(x, field.From, field.To);
        Assert.InRange(y, field.Row, field.Row + 3);
    }

    [Fact]
    public void APercentageTunedOnOneWindowsIsAFactAboutThatWindows()
    {
        var one = Find("locked-server-2012");
        var field = one.Field!.Value;

        var (_, y) = Cells(FirstVersion.ClickWhereTheBoxUsuallyIs(one.Frame), one.Frame);

        // 54% of 108 is row 58. The box is at row 42. Sixteen rows of wallpaper
        // between them, and the password goes into nothing.
        Assert.Equal(58, y);
        Assert.True(y > field.Row + 3);
    }

    [Fact]
    public void TheLargestPaleThingIsTheFace()
    {
        var one = Find("locked-server-2012");
        var field = one.Field!.Value;

        var spot = FirstVersion.LargestPaleThing(one.Frame);
        Assert.NotNull(spot);

        var (_, y) = Cells(spot.Value, one.Frame);

        // The photograph is 20 by 21 and sits above the box, which is 34 by 4.
        // By area the face wins; by shape it is not remotely a text field.
        Assert.True(y < field.Row);
    }

    [Fact]
    public void ShapeIsTheOnlyThingThatTellsThemApart()
    {
        var one = Find("locked-server-2012");

        var byShape = PasswordBox.FindIn(one.Frame);
        var bySize = FirstVersion.LargestPaleThing(one.Frame);

        Assert.NotNull(byShape);
        Assert.NotNull(bySize);
        Assert.NotEqual(bySize.Value.Y, byShape.Value.Y);
    }

    [Fact]
    public void ThereIsNothingToClickOnABlackScreen()
    {
        var black = Corpus.All.Single(one => one.Name == "black");

        Assert.Null(PasswordBox.FindIn(black.Frame));
        Assert.Null(PasswordBox.FindIn(null));
    }

    [Fact]
    public void ATallPanelIsNotAField()
    {
        // A window, not a text box: 40 wide and 30 tall. The ratio is what
        // rejects it, and the ratio is the entire fix.
        var frame = new Paint(seed: 7).Flat(40).Box(70, 30, 40, 30, 200).Done();

        Assert.Null(PasswordBox.FindIn(frame));
    }

    [Fact]
    public void AFieldIsFoundWhicheverWayTheThemeGoes()
    {
        // The same shape, pale on dark and dark on pale. Only the first is
        // found, and that is deliberate rather than an oversight: the box on a
        // sign-in screen is the lighter thing on every Windows there has been,
        // and a search that took either would take a window's shadow as well.
        var pale = new Paint(seed: 8).Flat(50).Box(79, 58, 34, 4, 210).Done();

        var spot = PasswordBox.FindIn(pale);
        Assert.NotNull(spot);
        Assert.InRange(Cells(spot.Value, pale).X, 79, 112);
    }
}
