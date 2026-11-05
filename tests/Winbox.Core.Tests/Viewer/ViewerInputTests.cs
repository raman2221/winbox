using System.Drawing;
using Winbox.Core.Terminal;
using Winbox.Protocol;

namespace Winbox.Core.Tests.Viewer;

public class ViewerInputTests
{
    private static readonly ImageLayout Layout = new(new Size(10, 20), new Size(800, 600), new Size(800, 600));
    private static readonly Winbox.Protocol.Point CellThreeTwoCenter = new(25, 30);

    [Fact]
    public void ToCommand_LeftPressInsideImage_ClicksLeftOnceAtMappedPoint()
    {
        var command = ViewerInput.ToCommand(new MouseEvent(0, 3, 2, true), Layout);

        Assert.Equal(new ClickCommand(CellThreeTwoCenter, MouseButton.Left, 1), command);
    }

    [Fact]
    public void ToCommand_MiddlePress_ClicksMiddle()
    {
        var command = ViewerInput.ToCommand(new MouseEvent(1, 3, 2, true), Layout);

        Assert.Equal(new ClickCommand(CellThreeTwoCenter, MouseButton.Middle, 1), command);
    }

    [Fact]
    public void ToCommand_RightPress_ClicksRight()
    {
        var command = ViewerInput.ToCommand(new MouseEvent(2, 3, 2, true), Layout);

        Assert.Equal(new ClickCommand(CellThreeTwoCenter, MouseButton.Right, 1), command);
    }

    [Fact]
    public void ToCommand_PressWithCtrlModifier_StillClicks()
    {
        var command = ViewerInput.ToCommand(new MouseEvent(16, 3, 2, true), Layout);

        Assert.Equal(new ClickCommand(CellThreeTwoCenter, MouseButton.Left, 1), command);
    }

    [Fact]
    public void ToCommand_Release_IsIgnored()
    {
        var command = ViewerInput.ToCommand(new MouseEvent(0, 3, 2, false), Layout);

        Assert.Null(command);
    }

    [Fact]
    public void ToCommand_PressOutsideImage_IsIgnored()
    {
        var command = ViewerInput.ToCommand(new MouseEvent(0, 81, 2, true), Layout);

        Assert.Null(command);
    }

    [Fact]
    public void ToCommand_PressBeforeFirstFrame_IsIgnored()
    {
        var command = ViewerInput.ToCommand(new MouseEvent(0, 3, 2, true), null);

        Assert.Null(command);
    }

    [Fact]
    public void ToCommand_WheelUp_ScrollsUpOneNotch()
    {
        var command = ViewerInput.ToCommand(new MouseEvent(64, 3, 2, true), Layout);

        Assert.Equal(new ScrollCommand(CellThreeTwoCenter, 1), command);
    }

    [Fact]
    public void ToCommand_WheelDown_ScrollsDownOneNotch()
    {
        var command = ViewerInput.ToCommand(new MouseEvent(65, 3, 2, true), Layout);

        Assert.Equal(new ScrollCommand(CellThreeTwoCenter, -1), command);
    }

    [Fact]
    public void ToCommand_MotionReport_IsIgnored()
    {
        var command = ViewerInput.ToCommand(new MouseEvent(32, 3, 2, true), Layout);

        Assert.Null(command);
    }

    [Fact]
    public void ToCommand_Text_TypesIt()
    {
        var command = ViewerInput.ToCommand(new TextEvent("héllo"), Layout);

        Assert.Equal(new TypeTextCommand("héllo"), command);
    }

    [Fact]
    public void ToCommand_Key_PressesCombo()
    {
        var command = ViewerInput.ToCommand(new KeyEvent("ctrl+c"), Layout);

        Assert.Equal(new PressKeyCommand("ctrl+c"), command);
    }

    [Fact]
    public void ToCommand_CtrlQ_IsNotForwarded()
    {
        var command = ViewerInput.ToCommand(new KeyEvent("ctrl+q"), Layout);

        Assert.Null(command);
    }

    [Fact]
    public void ToCommand_CellSizeReport_IsNotForwarded()
    {
        var command = ViewerInput.ToCommand(new CellSizeEvent(10, 20), Layout);

        Assert.Null(command);
    }

    [Fact]
    public void IsQuit_CtrlQ_IsTrue()
    {
        Assert.True(ViewerInput.IsQuit(new KeyEvent("ctrl+q")));
    }

    [Fact]
    public void IsQuit_OtherKey_IsFalse()
    {
        Assert.False(ViewerInput.IsQuit(new KeyEvent("ctrl+w")));
    }
}
