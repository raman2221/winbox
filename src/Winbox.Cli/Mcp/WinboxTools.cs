using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Winbox.Cli.Desktop;
using Winbox.Core;
using Winbox.Protocol;

namespace Winbox.Cli.Mcp;

/// <summary>The MCP tools an agent uses to see and drive its winbox desktop.</summary>
[McpServerToolType]
internal sealed class WinboxTools(GuestClient guest, SandboxLauncher launcher, DesktopActions actions, AgentView view)
{
    private const string Animated =
        " The pointer moves along a smooth, human-paced path, so a person watching can follow along;"
        + " expect it to take a few hundred milliseconds.";

    private const string XHelp = "x in pixels of the latest screenshot, from its left edge.";
    private const string YHelp = "y in pixels of the latest screenshot, from its top edge.";

    private const string IntentHelp =
        "Optional short phrase shown on screen to people watching, e.g. 'Opening the Start menu'."
        + " Keep it under ~6 words.";

    [McpServerTool(Name = "screenshot", ReadOnly = true)]
    [Description("Capture the winbox desktop as a PNG. Call this first and after every action to see what changed."
        + " All other tools take coordinates in the pixels of this image, measured from the top-left corner.")]
    public async Task<IEnumerable<ContentBlock>> Screenshot(CancellationToken cancel)
    {
        var shot = await view.CaptureAsync(cancel);
        var caption =
            $"Screen is {shot.Width}x{shot.Height} pixels; coordinates are pixels of this image from the top-left.";
        return [ImageContentBlock.FromBytes(shot.Png, "image/png"), new TextContentBlock { Text = caption }];
    }

    [McpServerTool(Name = "click")]
    [Description("Move the mouse to (x, y) on the winbox desktop and click." + Animated)]
    public Task<string> Click(
        [Description(XHelp)] int x,
        [Description(YHelp)] int y,
        [Description("Mouse button: left, right or middle.")] string button = "left",
        [Description("Number of clicks: 1 for a single click, 2 for a double-click, 3 for a triple-click.")]
        int clicks = 1,
        [Description(IntentHelp)] string? intent = null,
        CancellationToken cancel = default) =>
        actions.PerformAsync(DesktopRequests.Click(new Point(x, y), button, clicks), intent, cancel);

    [McpServerTool(Name = "move_mouse")]
    [Description("Move the mouse to (x, y) without clicking, e.g. to reveal a tooltip or hover menu." + Animated)]
    public Task<string> MoveMouse(
        [Description(XHelp)] int x,
        [Description(YHelp)] int y,
        [Description(IntentHelp)] string? intent = null,
        CancellationToken cancel = default) =>
        actions.PerformAsync(new MoveCommand(new Point(x, y)), intent, cancel);

    [McpServerTool(Name = "drag")]
    [Description("Press the left button at (fromX, fromY), drag to (toX, toY) and release."
        + " Use it to move windows, select text or drop files." + Animated)]
    public Task<string> Drag(
        int fromX,
        int fromY,
        int toX,
        int toY,
        [Description(IntentHelp)] string? intent = null,
        CancellationToken cancel = default) =>
        actions.PerformAsync(new DragCommand(new Point(fromX, fromY), new Point(toX, toY)), intent, cancel);

    [McpServerTool(Name = "scroll")]
    [Description("Move the mouse to (x, y) and turn the scroll wheel there." + Animated)]
    public Task<string> Scroll(
        [Description(XHelp)] int x,
        [Description(YHelp)] int y,
        [Description("Wheel notches: positive scrolls up, negative scrolls down. 3 is about one screenful of text.")]
        int notches,
        [Description(IntentHelp)] string? intent = null,
        CancellationToken cancel = default) =>
        actions.PerformAsync(new ScrollCommand(new Point(x, y), notches), intent, cancel);

    [McpServerTool(Name = "type_text")]
    [Description("Type text into whatever has keyboard focus, exactly as given (newlines press Enter)."
        + " Click the target field first. Keys are typed one at a time at a human pace (about 50 ms each)"
        + " so a person can watch, so long text takes a while.")]
    public Task<string> TypeText(
        [Description("The text to type.")] string text,
        [Description(IntentHelp)] string? intent = null,
        CancellationToken cancel = default) =>
        actions.PerformAsync(DesktopRequests.TypeText(text), intent, cancel);

    [McpServerTool(Name = "press_key")]
    [Description("Press a key or key combination, e.g. \"enter\", \"ctrl+l\", \"alt+f4\", \"win+r\","
        + " \"ctrl+shift+esc\". Keys: letters, digits, f1-f24, enter, tab, esc, space, backspace, delete,"
        + " arrows (up/down/left/right), home, end, pageup, pagedown; modifiers ctrl, shift, alt, win.")]
    public Task<string> PressKey(
        [Description("Key names joined with '+'.")] string combo,
        [Description(IntentHelp)] string? intent = null,
        CancellationToken cancel = default) =>
        actions.PerformAsync(DesktopRequests.PressKey(combo), intent, cancel);

    [McpServerTool(Name = "run_program")]
    [Description("Start a program inside the sandbox, the way the Win+R Run dialog would, e.g. \"msedge\","
        + " \"msedge https://example.com\" or \"cmd /k dir\". Returns once launched; take a screenshot to see it.")]
    public Task<string> RunProgram(
        [Description("The program and its arguments.")] string command,
        [Description(IntentHelp)] string? intent = null,
        CancellationToken cancel = default) =>
        actions.PerformAsync(DesktopRequests.Run(command), intent, cancel);

    [McpServerTool(Name = "status", ReadOnly = true)]
    [Description("Report whether the winbox sandbox is running, whether its helper responds,"
        + " and the screen size. Use it when other tools fail.")]
    public string Status() => StatusReport.Describe(guest);

    [McpServerTool(Name = "start_sandbox")]
    [Description("Boot a fresh winbox desktop in Windows Sandbox. Takes up to a few minutes;"
        + " returns when the desktop is ready. Fails if a sandbox is already running.")]
    public async Task<string> StartSandbox(
        [Description("Memory for the sandbox in MB.")] int memoryMb = 4096,
        [Description("True to start without network access.")] bool offline = false,
        CancellationToken cancel = default)
    {
        var progress = new LineLog();
        await launcher.StartAsync(DesktopRequests.SandboxOptions(memoryMb, offline), progress, cancel);
        return progress.Text + Environment.NewLine + "The sandbox is ready. Take a screenshot to see the desktop.";
    }

    [McpServerTool(Name = "stop_sandbox", Destructive = true)]
    [Description("Shut the winbox sandbox down. Everything inside it (files, open apps) is discarded for good.")]
    public async Task<string> StopSandbox(CancellationToken cancel = default)
    {
        await launcher.StopAsync(cancel);
        return "The sandbox is shutting down. Everything inside it will be discarded.";
    }

    /// <summary>Collects progress lines so they can be returned to the agent in one reply.</summary>
    private sealed class LineLog : IProgress<string>
    {
        private readonly List<string> lines = [];

        public string Text => string.Join(Environment.NewLine, lines);

        public void Report(string value) => lines.Add(value);
    }
}
