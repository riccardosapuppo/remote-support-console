namespace SupportConsole.Tests;

using SupportConsole.Frames;
using SupportConsole.Vision;
using Xunit;

/// <summary>
/// The frame itself, and the signals taken off it, each one on its own.
/// </summary>
public class Measuring
{
    private static RemoteFrame Flat(byte value, int width = 8, int height = 6)
    {
        var cells = new byte[width * height];
        Array.Fill(cells, value);
        return new RemoteFrame(width, height, cells);
    }

    [Fact]
    public void AFrameWithNoAreaIsNotAFrame()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RemoteFrame(0, 4, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RemoteFrame(4, 0, []));
    }

    [Fact]
    public void TheGridHasToBeTheSizeItSaysItIs()
    {
        var wrong = Assert.Throws<ArgumentException>(() => new RemoteFrame(4, 4, new byte[15]));

        // The message carries both numbers, because "invalid argument" is not
        // something anybody can act on at three in the morning.
        Assert.Contains("16", wrong.Message, StringComparison.Ordinal);
        Assert.Contains("15", wrong.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BlackIsNotQuiteZero()
    {
        // A remote session that has connected and not drawn yet is not exactly
        // zero everywhere: compression leaves a few levels behind.
        Assert.True(Flat(0).IsBlack);
        Assert.True(Flat(11).IsBlack);
        Assert.False(Flat(40).IsBlack);
    }

    [Fact]
    public void MeanOfRowsSurvivesBeingAskedBackwardsOrOffTheEdge()
    {
        var frame = Flat(100);

        Assert.Equal(100, frame.MeanOfRows(1, 3), 3);
        Assert.Equal(100, frame.MeanOfRows(3, 1), 3);
        Assert.Equal(100, frame.MeanOfRows(-5, 99), 3);
    }

    [Fact]
    public void DownsamplingAveragesTheBlockItStandsFor()
    {
        // Four source pixels per cell: 0, 100, 200, 255 in the top-left block.
        var source = new byte[,]
        {
            { 0, 100, 10, 10 },
            { 200, 255, 10, 10 },
            { 60, 60, 60, 60 },
            { 60, 60, 60, 60 },
        };

        var frame = RemoteFrame.From(4, 4, (x, y) => source[y, x], width: 2, height: 2);

        Assert.Equal((0 + 100 + 200 + 255) / 4, frame.At(0, 0));
        Assert.Equal(10, frame.At(1, 0));
        Assert.Equal(60, frame.At(0, 1));
    }

    [Fact]
    public void DetailIsZeroOnAFlatScreenAndLargeOnASpeckledOne()
    {
        Assert.Equal(0, Signals.EdgeEnergy(Flat(90)), 3);

        var speckled = new Paint(seed: 3).Flat(90).Speckle(70).Done();
        Assert.True(Signals.EdgeEnergy(speckled) > 20);
    }

    [Fact]
    public void AStraightEdgeReadsAsOneAndAGradientDoesNot()
    {
        var edge = new Paint(seed: 4).Flat(40).Box(0, 50, 192, 58, 200).Done();
        Assert.Equal(1.0, Signals.HorizontalEdge(edge, 50), 3);

        var gradient = new Paint(seed: 4).Gradient(40, 200).Done();
        Assert.Equal(0.0, Signals.HorizontalEdge(gradient, 50), 3);
    }

    [Fact]
    public void SpreadIsWhatSeparatesATaskbarFromACorner()
    {
        var bar = new Paint(seed: 5).Flat(40).Taskbar(40).Done();
        var corner = new Paint(seed: 5).Flat(40).Box(180, 100, 8, 4, 220).Done();

        var (barFilled, barSpread) = Signals.SlicesWithStructure(bar, 103, 107);
        var (cornerFilled, cornerSpread) = Signals.SlicesWithStructure(corner, 103, 107);

        Assert.True(barSpread >= 6);
        Assert.True(cornerSpread < 6);

        // And not by how much structure there is: the corner has some too.
        Assert.True(cornerFilled >= 1);
        Assert.True(barFilled >= 3);
    }

    [Fact]
    public void TheBrightestRunIsRelativeToItsOwnRow()
    {
        // The same box, on a dark ground and on a pale one. An absolute
        // threshold would find one of these and not the other.
        var dark = new Paint(seed: 6).Flat(30).Box(80, 50, 34, 4, 120).Done();
        var pale = new Paint(seed: 6).Flat(150).Box(80, 50, 34, 4, 240).Done();

        Assert.Equal((80, 113), Signals.BrightestRun(dark, 51));
        Assert.Equal((80, 113), Signals.BrightestRun(pale, 51));
    }

    [Fact]
    public void TheCorpusIsTheSameCorpusEveryTime()
    {
        // Seeded, because a README that quotes a number from a corpus that
        // moves is a README that quotes nothing.
        var once = new Paint(seed: 9).Gradient(140, 90).Speckle(60).Done();
        var again = new Paint(seed: 9).Gradient(140, 90).Speckle(60).Done();

        for (var y = 0; y < once.Height; y++)
        {
            for (var x = 0; x < once.Width; x++) Assert.Equal(once.At(x, y), again.At(x, y));
        }
    }

    [Fact]
    public void ADifferentSeedIsADifferentWallpaper()
    {
        var one = new Paint(seed: 9).Flat(90).Speckle(60).Done();
        var other = new Paint(seed: 10).Flat(90).Speckle(60).Done();

        var same = 0;
        for (var y = 0; y < one.Height; y++)
        {
            for (var x = 0; x < one.Width; x++)
            {
                if (one.At(x, y) == other.At(x, y)) same++;
            }
        }

        Assert.True(same < one.Width * one.Height);
    }
}
