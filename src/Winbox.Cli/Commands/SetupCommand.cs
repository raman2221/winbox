using Winbox.Cli.CommandLine;
using Winbox.Core;

namespace Winbox.Cli.Commands;

/// <summary>Checks that this PC can run winbox and says what to do next.</summary>
internal sealed class SetupCommand : ICliCommand
{
    private const string NextSteps =
        "Next steps:\n"
        + "  1. winbox start         boot the sandbox desktop\n"
        + "  2. winbox view          watch and control it (in Windows Terminal)\n"
        + "  3. winbox mcp-config    connect an AI agent to it";

    public CommandHelp Help { get; } = new("setup", "Check that this PC can run winbox");

    public Task RunAsync(CommandArgs args, CancellationToken cancel)
    {
        args.ExpectBetween(0, 0);
        ReportEdition();
        ReportTerminal();
        ReportSandbox();
        Console.WriteLine();
        Console.WriteLine(NextSteps);
        return Task.CompletedTask;
    }

    private static void ReportEdition()
    {
        var edition = SetupAdvice.EditionId();
        Console.WriteLine($"Windows edition:    {(edition.Length > 0 ? edition : "unknown")}");
        if (SetupAdvice.IsHomeEdition(edition))
            throw new WinboxException(
                "Windows Home isn't supported: Windows Sandbox needs Windows 11 Pro, Enterprise or Education.");
    }

    private static void ReportTerminal()
    {
        var state = WindowsTerminal.IsInstalled()
            ? "found"
            : "not found. `winbox view` needs it for Sixel graphics: winget install Microsoft.WindowsTerminal";
        Console.WriteLine($"Windows Terminal:   {state}");
    }

    private static void ReportSandbox()
    {
        var installed = SandboxLauncher.IsSandboxInstalled();
        Console.WriteLine($"Windows Sandbox:    {(installed ? "enabled" : "not enabled")}");
        if (!installed) throw new WinboxException(SetupAdvice.SandboxMissing);
    }
}
