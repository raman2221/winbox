using Winbox.Core;
using Winbox.Protocol;

namespace Winbox.Cli.Desktop;

/// <summary>A short readable summary of the sandbox and its helper, shared by the CLI and the MCP tools.</summary>
internal static class StatusReport
{
    public static string Describe(GuestClient guest)
    {
        var status = guest.ReadStatus();
        var alive = status?.IsAliveAt(DateTimeOffset.UtcNow) ?? false;
        var lines = new List<string>
        {
            $"Sandbox: {(SandboxLauncher.IsSandboxRunning() ? "running" : "not running")}",
            $"Helper: {(alive ? "responding" : "not responding")}",
        };
        if (alive) lines.AddRange(HelperDetails(status!));
        else lines.Add("Run `winbox start` to start the sandbox.");
        return string.Join(Environment.NewLine, lines);
    }

    private static IEnumerable<string> HelperDetails(GuestStatus status) =>
    [
        $"Screen: {status.ScreenWidth}x{status.ScreenHeight}",
        $"Helper version: {status.Version}",
    ];
}
