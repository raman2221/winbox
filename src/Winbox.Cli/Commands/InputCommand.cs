using Winbox.Cli.CommandLine;
using Winbox.Cli.Desktop;
using Winbox.Protocol;

namespace Winbox.Cli.Commands;

/// <summary>A subcommand that turns its arguments into one guest input action, such as a click.</summary>
internal sealed class InputCommand(CommandHelp help, Func<CommandArgs, Command> parse, DesktopActions actions)
    : ICliCommand
{
    public CommandHelp Help { get; } = help;

    public async Task RunAsync(CommandArgs args, CancellationToken cancel) =>
        Console.WriteLine(await actions.PerformAsync(parse(args), cancel));
}
