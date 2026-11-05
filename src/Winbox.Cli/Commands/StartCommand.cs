using Winbox.Cli.CommandLine;
using Winbox.Cli.Desktop;
using Winbox.Core;

namespace Winbox.Cli.Commands;

internal sealed class StartCommand(SandboxLauncher launcher) : ICliCommand
{
    private static readonly int DefaultMemoryMb = new SandboxOptions().MemoryMb;

    public CommandHelp Help { get; } = new("start [--memory <MB>] [--offline]", "Boot the sandbox desktop");

    public async Task RunAsync(CommandArgs args, CancellationToken cancel)
    {
        var options = ParseOptions(args);
        Console.WriteLine($"Starting Windows Sandbox ({options.MemoryMb} MB, {NetworkLabel(options)})...");
        await launcher.StartAsync(options, new ConsoleProgress(), cancel);
        Console.WriteLine("winbox is ready. Watch it with `winbox view`; connect an agent with `winbox mcp-config`.");
    }

    private static SandboxOptions ParseOptions(CommandArgs args)
    {
        var memory = args.TakeOption("--memory");
        var offline = args.TakeFlag("--offline");
        args.ExpectBetween(0, 0);
        var memoryMb = memory is null ? DefaultMemoryMb : args.ParseInt(memory);
        return DesktopRequests.SandboxOptions(memoryMb, offline);
    }

    private static string NetworkLabel(SandboxOptions options) => options.Networking ? "online" : "offline";

    /// <summary>Prints each line as it arrives, unlike <see cref="Progress{T}"/>, which posts asynchronously.</summary>
    private sealed class ConsoleProgress : IProgress<string>
    {
        public void Report(string value) => Console.WriteLine(value);
    }
}
