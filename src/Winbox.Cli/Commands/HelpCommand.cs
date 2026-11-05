using Winbox.Cli.CommandLine;

namespace Winbox.Cli.Commands;

internal sealed class HelpCommand(CommandSet commands) : ICliCommand
{
    private const string Intro =
        "winbox gives AI agents their own Windows desktop inside Windows Sandbox,\n"
        + "so they never touch your real mouse, keyboard or clipboard.";

    public CommandHelp Help { get; } = new("help", "Show this help");

    public Task RunAsync(CommandArgs args, CancellationToken cancel)
    {
        Console.WriteLine(Intro);
        Console.WriteLine();
        Console.WriteLine("Usage:");
        var width = commands.All.Max(command => command.Help.Usage.Length) + 2;
        foreach (var command in commands.All)
            Console.WriteLine($"  winbox {command.Help.Usage.PadRight(width)}{command.Help.Summary}");
        return Task.CompletedTask;
    }
}
