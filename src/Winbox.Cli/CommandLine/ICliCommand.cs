namespace Winbox.Cli.CommandLine;

/// <summary>How a command is invoked and what it does, for dispatch and the help screen.</summary>
internal sealed record CommandHelp(string Usage, string Summary)
{
    public string Name => Usage.Split(' ')[0];
}

/// <summary>One <c>winbox</c> subcommand.</summary>
internal interface ICliCommand
{
    CommandHelp Help { get; }

    Task RunAsync(CommandArgs args, CancellationToken cancel);
}
