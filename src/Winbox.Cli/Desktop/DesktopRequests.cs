using Winbox.Core;
using Winbox.Protocol;

namespace Winbox.Cli.Desktop;

/// <summary>
/// Builds guest commands from user or agent input, rejecting bad values on the host
/// so the error message can say what to do instead.
/// </summary>
internal static class DesktopRequests
{
    private const int MaxClicks = 3;

    public static ClickCommand Click(Point at, string button, int clicks)
    {
        if (clicks is < 1 or > MaxClicks) throw new WinboxException($"clicks must be between 1 and {MaxClicks}.");
        return new ClickCommand(at, ParseButton(button), clicks);
    }

    public static PressKeyCommand PressKey(string combo)
    {
        // The wire format separates arguments with spaces, so a combo must not carry any.
        var compact = combo.Replace(" ", "");
        try
        {
            KeyCombo.Parse(compact);
        }
        catch (ProtocolException error)
        {
            throw new WinboxException($"{error.Message}. Use combos like enter, ctrl+l, alt+f4 or win+r.");
        }
        return new PressKeyCommand(compact);
    }

    public static TypeTextCommand TypeText(string text) =>
        text.Length > 0 ? new TypeTextCommand(text) : throw new WinboxException("There is no text to type.");

    public static RunCommand Run(string commandLine) =>
        string.IsNullOrWhiteSpace(commandLine)
            ? throw new WinboxException("There is no command to run.")
            : new RunCommand(commandLine.Trim());

    public static SandboxOptions SandboxOptions(int memoryMb, bool offline) =>
        memoryMb > 0
            ? new SandboxOptions(memoryMb, Networking: !offline)
            : throw new WinboxException("Memory must be a positive number of megabytes.");

    private static MouseButton ParseButton(string name) => name.ToLowerInvariant() switch
    {
        "left" => MouseButton.Left,
        "right" => MouseButton.Right,
        "middle" => MouseButton.Middle,
        _ => throw new WinboxException($"Unknown mouse button '{name}'. Use left, right or middle."),
    };
}
