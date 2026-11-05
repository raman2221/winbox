using System.Drawing;
using Winbox.Core.Terminal;
using GuestPoint = Winbox.Protocol.Point;

namespace Winbox.Core.Tests.Viewer;

public class ImageLayoutTests
{
    // 10x20 cells showing a 1920x1080 guest at half size (960x540 pixels).
    private static readonly ImageLayout HalfScale = new(new Size(10, 20), new Size(960, 540), new Size(1920, 1080));

    [Fact]
    public void GuestPointAt_TopLeftCell_MapsCellCenterScaledToGuest()
    {
        var point = HalfScale.GuestPointAt(1, 1);

        Assert.Equal(new GuestPoint(11, 21), point);
    }

    [Fact]
    public void GuestPointAt_CellInsideImage_ScalesByGuestOverDisplayedWidth()
    {
        var point = HalfScale.GuestPointAt(49, 14);

        Assert.Equal(new GuestPoint(971, 541), point);
    }

    [Fact]
    public void GuestPointAt_CellRightOfImage_ReturnsNull()
    {
        var point = HalfScale.GuestPointAt(97, 1);

        Assert.Null(point);
    }

    [Fact]
    public void GuestPointAt_CellBelowImage_ReturnsNull()
    {
        var point = HalfScale.GuestPointAt(1, 28);

        Assert.Null(point);
    }

    [Fact]
    public void GuestPointAt_LastCellWhoseCenterIsInside_StaysWithinGuestScreen()
    {
        var point = HalfScale.GuestPointAt(96, 27);

        Assert.Equal(new GuestPoint(1911, 1061), point);
    }

    [Fact]
    public void GuestPointAt_UnscaledImage_MapsCellCenterDirectly()
    {
        var layout = new ImageLayout(new Size(10, 20), new Size(800, 600), new Size(800, 600));

        var point = layout.GuestPointAt(3, 2);

        Assert.Equal(new GuestPoint(25, 30), point);
    }
}
