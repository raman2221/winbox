using Winbox.Cli.CommandLine;
using Winbox.Cli.Desktop;
using Winbox.Protocol;

namespace Winbox.Cli.Commands;

/// <summary>A subcommand that turns its arguments into one guest input action, such as a click.</summary>
internal sealed class InputCommand(CommandHelp help, Func<CommandArgs, Command> parse, DesktopActions actions)
    : ICliCommand
{
    private const string WhyOption = "--why";

    public CommandHelp Help { get; } = help with { Usage = $"{help.Usage} [{WhyOption} <text>]" };

    public async Task RunAsync(CommandArgs args, CancellationToken cancel)
    {
        // Taken before parsing, because the parsers reject any option they don't know.
        var why = args.TakeOption(WhyOption);
        Console.WriteLine(await actions.PerformAsync(parse(args), why, cancel));
    }
}
