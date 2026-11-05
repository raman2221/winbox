using Winbox.Cli.CommandLine;
using Winbox.Core;

namespace Winbox.Cli.Commands;

internal sealed class StopCommand(SandboxLauncher launcher) : ICliCommand
{
    public CommandHelp Help { get; } = new("stop", "Shut the sandbox down (its contents are discarded)");

    public async Task RunAsync(CommandArgs args, CancellationToken cancel)
    {
        args.ExpectBetween(0, 0);
        await launcher.StopAsync(cancel);
        Console.WriteLine("The sandbox is shutting down. Everything inside it will be discarded.");
    }
}
