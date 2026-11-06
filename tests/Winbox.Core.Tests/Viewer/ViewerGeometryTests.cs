using System.Drawing;
using Winbox.Core.Terminal;

namespace Winbox.Core.Tests.Viewer;

public class ViewerGeometryTests
{
    private static readonly Size Cell = new(10, 20);

    [Fact]
    public void ImageBox_LeavesLastRowForStatus()
    {
        var box = ViewerGeometry.ImageBox(new Size(120, 31), Cell);

        Assert.Equal(new Size(1200, 600), box);
    }

    [Fact]
    public void ImageBox_HeightNotWholeSixelBands_RoundsDown()
    {
        var box = ViewerGeometry.ImageBox(new Size(80, 24), Cell);

        Assert.Equal(456, box.Height);
    }

    [Fact]
    public void ImageBox_OddCellHeight_RoundsDownToSixelBand()
    {
        var box = ViewerGeometry.ImageBox(new Size(80, 24), new Size(9, 17));

        Assert.Equal(390, box.Height);
    }

    [Fact]
    public void ImageBox_SingleRowWindow_StillHasOneBand()
    {
        var box = ViewerGeometry.ImageBox(new Size(80, 1), Cell);

        Assert.Equal(6, box.Height);
    }

    [Fact]
    public void StatusLine_WideWindow_ShowsFullText()
    {
        var status = ViewerGeometry.StatusLine(new Size(1920, 1080), null, 120);

        Assert.Equal("winbox · 1920x1080 · Ctrl+Q quit · click/type to control", status);
    }

    [Fact]
    public void StatusLine_NarrowWindow_LeavesLastColumnEmpty()
    {
        var status = ViewerGeometry.StatusLine(new Size(1920, 1080), null, 20);

        Assert.Equal("winbox · 1920x1080 ", status);
    }

    [Fact]
    public void StatusLine_WithNotice_ReplacesControlHintButKeepsQuitHint()
    {
        var status = ViewerGeometry.StatusLine(new Size(1920, 1080), "guest not responding, retrying", 120);

        Assert.Equal("winbox · 1920x1080 · Ctrl+Q quit · guest not responding, retrying", status);
    }

    [Fact]
    public void StatusLine_NoticeWithControlCharacters_PrintsThemAsSpaces()
    {
        var status = ViewerGeometry.StatusLine(new Size(800, 600), "bad\u001b[2Jthing\r\n", 120);

        Assert.Equal("winbox · 800x600 · Ctrl+Q quit · bad [2Jthing  ", status);
    }
}
