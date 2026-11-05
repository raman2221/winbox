using Winbox.Cli.CommandLine;
using Winbox.Cli.Desktop;
using Winbox.Core;

namespace Winbox.Cli.Commands;

internal sealed class StatusCommand(GuestClient guest) : ICliCommand
{
    public CommandHelp Help { get; } = new("status", "Show whether the sandbox and its helper are running");

    public Task RunAsync(CommandArgs args, CancellationToken cancel)
    {
        args.ExpectBetween(0, 0);
        Console.WriteLine(StatusReport.Describe(guest));
        return Task.CompletedTask;
    }
}
