using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Winbox.Guest.Motion;
using Winbox.Guest.Overlay;
using Winbox.Protocol;

namespace Winbox.Guest;

/// <summary>Synthesizes mouse and keyboard input with SendInput, paced so a watcher can follow it.</summary>
internal sealed class InputInjector(ScreenCapture screen, HumanPacing pacing, IActivityFeedback feedback)
{
    private const int WheelDelta = 120;
    private const ushort EnterKey = 0x0D, TabKey = 0x09;
    private const int PauseBeforeClickMs = 70, PauseBetweenClicksMs = 90;

    public void MoveTo(Point target)
    {
        foreach (var point in pacing.Motion.PlanPath(Native.CursorPosition(), target))
        {
            JumpTo(point);
            Thread.Sleep(MotionPlanner.FrameMs);
        }
    }

    public void Click(Point at, MouseButton button, int count)
    {
        MoveTo(at);
        Thread.Sleep(PauseBeforeClickMs);
        var (down, up) = Native.ButtonFlags(button);
        for (var i = 0; i < count; i++)
        {
            feedback.ShowClick(at);
            Native.Send(Native.Mouse(down, 0, 0, 0), Native.Mouse(up, 0, 0, 0));
            if (i < count - 1) Thread.Sleep(PauseBetweenClicksMs);
        }
    }

    public void Drag(Point from, Point to)
    {
        MoveTo(from);
        Native.Send(Native.Mouse(Native.LeftDown, 0, 0, 0));
        MoveTo(to);
        Native.Send(Native.Mouse(Native.LeftUp, 0, 0, 0));
    }

    public void Scroll(Point at, int notches)
    {
        MoveTo(at);
        Native.Send(Native.Mouse(Native.Wheel, 0, 0, notches * WheelDelta));
    }

    public void TypeText(string text)
    {
        foreach (var ch in text.Replace("\r\n", "\n"))
        {
            TypeCharacter(ch);
            Thread.Sleep(pacing.Rhythm.DelayAfter(ch));
        }
    }

    /// <summary>Enter and Tab go out as real keys: as Unicode packets they don't submit forms or move focus.</summary>
    private void TypeCharacter(char ch)
    {
        if (ch == '\n') PressKeys(new[] { EnterKey });
        else if (ch == '\t') PressKeys(new[] { TabKey });
        else Native.Send(Native.UnicodeKey(ch, keyUp: false), Native.UnicodeKey(ch, keyUp: true));
    }

    public void PressKeys(IReadOnlyList<ushort> keys)
    {
        var downs = keys.Select(key => Native.VirtualKey(key, keyUp: false));
        var ups = keys.Reverse().Select(key => Native.VirtualKey(key, keyUp: true));
        Native.Send(downs.Concat(ups).ToArray());
    }

    private void JumpTo(Point point)
    {
        var size = screen.Size;
        var dx = Normalize(point.X, size.Width);
        var dy = Normalize(point.Y, size.Height);
        Native.Send(Native.Mouse(Native.MouseMove | Native.MouseAbsolute, dx, dy, 0));
    }

    /// <summary>
    /// Maps a pixel to SendInput's 0..65535 absolute range. Rounding up keeps Windows' truncating reverse mapping
    /// on the intended pixel, and long math avoids overflow on very wide screens.
    /// </summary>
    private static int Normalize(int pixel, int extent)
    {
        var clamped = System.Math.Max(0, System.Math.Min(pixel, extent - 1));
        return (int)(((long)clamped * 65536 + extent - 1) / System.Math.Max(1, extent));
    }
}
