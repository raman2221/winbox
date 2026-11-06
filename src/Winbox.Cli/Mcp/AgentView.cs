using Winbox.Cli.Desktop;
using Winbox.Core;
using Winbox.Core.Rendering;
using Winbox.Protocol;

namespace Winbox.Cli.Mcp;

/// <summary>
/// The desktop as the agent sees it: screenshots shrunk to a size Claude won't shrink again, and the agent's
/// coordinates mapped back to guest pixels. One per MCP server, because a click refers to the latest screenshot.
/// </summary>
internal sealed class AgentView(GuestClient guest) : IScreenMap
{
    private ScreenScale? _latest;

    public async Task<Screenshot> CaptureAsync(CancellationToken cancel)
    {
        var shot = await guest.ScreenshotAsync(cancel);
        var scale = ScreenScale.For(shot.Width, shot.Height);
        var png = scale.IsShrunk ? PngScaler.Resize(shot.Png, scale.Scaled) : shot.Png;
        Volatile.Write(ref _latest, scale);
        return new Screenshot(png, scale.Scaled.Width, scale.Scaled.Height);
    }

    /// <summary>Until the agent has seen a screenshot, its coordinates are taken as guest pixels.</summary>
    public Command ToGuest(Command command) => Volatile.Read(ref _latest)?.ToGuest(command) ?? command;
}
