using Winbox.Protocol;

namespace Winbox.Core.Terminal;

/// <summary>Decides which guest command, if any, a terminal event in the live viewer turns into.</summary>
public static class ViewerInput
{
    public const string QuitCombo = "ctrl+q";

    private const int WheelFlag = 64;
    private const int MotionFlag = 32;
    private const int ButtonBits = 3;

    public static bool IsQuit(TerminalEvent input) => input is KeyEvent { Combo: QuitCombo };

    /// <summary>Returns null for events the guest should not see (quit, releases, clicks outside the image).</summary>
    public static Command? ToCommand(TerminalEvent input, ImageLayout? layout) => input switch
    {
        TextEvent text => new TypeTextCommand(text.Text),
        KeyEvent key when !IsQuit(key) => new PressKeyCommand(key.Combo),
        MouseEvent { Pressed: true } mouse when layout is not null => ToMouseCommand(mouse, layout),
        _ => null,
    };

    private static Command? ToMouseCommand(MouseEvent mouse, ImageLayout layout)
    {
        if ((mouse.Button & MotionFlag) != 0) return null;
        if (layout.GuestPointAt(mouse.Column, mouse.Row) is not { } at) return null;
        if ((mouse.Button & WheelFlag) != 0) return new ScrollCommand(at, WheelNotches(mouse.Button));
        return ToButton(mouse.Button) is { } button ? new ClickCommand(at, button, 1) : null;
    }

    // SGR wheel codes: 64 is wheel up, 65 wheel down; modifier bits (4, 8, 16) are ignored.
    private static int WheelNotches(int code) => (code & ButtonBits) == 0 ? 1 : -1;

    private static MouseButton? ToButton(int code) => (code & ButtonBits) switch
    {
        0 => MouseButton.Left,
        1 => MouseButton.Middle,
        2 => MouseButton.Right,
        _ => null,
    };
}
