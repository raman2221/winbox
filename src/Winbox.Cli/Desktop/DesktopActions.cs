using Winbox.Core;
using Winbox.Protocol;

namespace Winbox.Cli.Desktop;

/// <summary>
/// Performs input actions on the guest desktop and reports what was done, in the caller's own coordinates.
/// </summary>
internal sealed class DesktopActions(ActionSender sender, IScreenMap screen)
{
    /// <param name="intent">Why the agent acts, shown to people watching; null for a generic caption.</param>
    public async Task<string> PerformAsync(Command command, string? intent, CancellationToken cancel)
    {
        await sender.SendAsync(screen.ToGuest(command), intent, cancel);
        return ActionText.Describe(command);
    }
}
