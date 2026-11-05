using Winbox.Cli.CommandLine;
using Winbox.Cli.Viewer;
using Winbox.Core;

namespace Winbox.Cli.Commands;

internal sealed class ViewCommand(GuestClient guest) : ICliCommand
{
    public CommandHelp Help { get; } = new("view", "Watch and control the desktop in this terminal");

    public Task RunAsync(CommandArgs args, CancellationToken cancel)
    {
        args.ExpectBetween(0, 0);
        return ViewerSession.RunAsync(guest, cancel);
    }
}
