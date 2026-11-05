using Winbox.Core;

namespace Winbox.Cli.CommandLine;

/// <summary>Finds the subcommand named on the command line and runs it.</summary>
internal sealed class CommandSet
{
    private const string DefaultCommand = "help";

    private readonly List<ICliCommand> commands = [];

    public IReadOnlyList<ICliCommand> All => commands;

    public void Register(ICliCommand command) => commands.Add(command);

    public Task RunAsync(string[] args, CancellationToken cancel)
    {
        var name = args.Length == 0 ? DefaultCommand : args[0];
        var command = Find(name);
        return command.RunAsync(new CommandArgs(args.Skip(1), command.Help.Usage), cancel);
    }

    private ICliCommand Find(string name) =>
        commands.FirstOrDefault(command => command.Help.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? throw new WinboxException($"Unknown command '{name}'. Run `winbox help` to see what's available.");
}
