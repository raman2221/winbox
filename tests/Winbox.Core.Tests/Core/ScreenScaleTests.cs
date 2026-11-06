using System.Drawing;
using Winbox.Protocol;
using GuestPoint = Winbox.Protocol.Point;

namespace Winbox.Core.Tests.Core;

public class ScreenScaleTests
{
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1024, 768)]
    [InlineData(1568, 600)]
    public void For_ScreenWithinClaudesLimits_IsNotShrunk(int width, int height)
    {
        var scale = ScreenScale.For(width, height);

        Assert.False(scale.IsShrunk);
        Assert.Equal(new Size(width, height), scale.Scaled);
    }

    [Theory]
    [InlineData(1920, 1080, 1429, 804)]
    [InlineData(1920, 1200, 1356, 847)]
    [InlineData(2560, 1440, 1429, 804)]
    public void For_LargeScreen_ShrinksToTheAreaLimit(int width, int height, int scaledWidth, int scaledHeight)
    {
        var scale = ScreenScale.For(width, height);

        Assert.Equal(new Size(scaledWidth, scaledHeight), scale.Scaled);
    }

    [Fact]
    public void For_VeryWideScreen_ShrinksToTheLongEdgeLimit()
    {
        var scale = ScreenScale.For(3000, 500);

        Assert.Equal(new Size(ScreenScale.MaxLongEdge, 261), scale.Scaled);
    }

    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(3840, 2160)]
    [InlineData(5120, 1440)]
    [InlineData(1366, 1024)]
    public void For_AnyScreen_StaysInsideBothLimits(int width, int height)
    {
        var scaled = ScreenScale.For(width, height).Scaled;

        Assert.InRange(Math.Max(scaled.Width, scaled.Height), 1, ScreenScale.MaxLongEdge);
        Assert.InRange(scaled.Width * scaled.Height, 1, ScreenScale.MaxPixels);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(714, 402, 959, 540)]
    [InlineData(1429, 804, 1920, 1080)]
    public void ToGuest_PointInScaledImage_MapsBackToGuestPixels(int x, int y, int guestX, int guestY)
    {
        var scale = ScreenScale.For(1920, 1080);

        var guest = scale.ToGuest(new GuestPoint(x, y));

        Assert.Equal(new GuestPoint(guestX, guestY), guest);
    }

    [Fact]
    public void ToGuest_UnshrunkScreen_KeepsThePoint()
    {
        var scale = ScreenScale.For(1280, 720);

        Assert.Equal(new GuestPoint(640, 360), scale.ToGuest(new GuestPoint(640, 360)));
    }

    [Fact]
    public void ToGuest_DragCommand_MapsBothEnds()
    {
        var scale = ScreenScale.For(1920, 1080);
        var drag = new DragCommand(new GuestPoint(0, 0), new GuestPoint(1429, 804));

        var guest = scale.ToGuest(drag);

        Assert.Equal(new DragCommand(new GuestPoint(0, 0), new GuestPoint(1920, 1080)), guest);
    }

    [Fact]
    public void ToGuest_ClickCommand_KeepsButtonAndCount()
    {
        var scale = ScreenScale.For(1920, 1080);

        var guest = scale.ToGuest(new ClickCommand(new GuestPoint(714, 402), MouseButton.Right, 2));

        Assert.Equal(new ClickCommand(new GuestPoint(959, 540), MouseButton.Right, 2), guest);
    }

    [Fact]
    public void ToGuest_CommandWithoutPoints_PassesThroughUnchanged()
    {
        var typing = new TypeTextCommand("hello");

        var guest = ScreenScale.For(1920, 1080).ToGuest(typing);

        Assert.Same(typing, guest);
    }
}
