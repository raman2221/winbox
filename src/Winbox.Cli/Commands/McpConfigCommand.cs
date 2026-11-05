using System.Text.Json;
using Winbox.Cli.CommandLine;
using Winbox.Core;

namespace Winbox.Cli.Commands;

/// <summary>Prints how to register this executable as an MCP server with Claude Code and other clients.</summary>
internal sealed class McpConfigCommand : ICliCommand
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public CommandHelp Help { get; } = new("mcp-config", "Print the config that connects an agent to winbox");

    public Task RunAsync(CommandArgs args, CancellationToken cancel)
    {
        args.ExpectBetween(0, 0);
        var exe = Environment.ProcessPath ?? throw new WinboxException("Can't tell where winbox.exe is installed.");
        Console.WriteLine("Claude Code:");
        Console.WriteLine();
        Console.WriteLine($"    claude mcp add winbox -- \"{exe}\" mcp");
        Console.WriteLine();
        Console.WriteLine("Other MCP clients (add to their mcpServers config):");
        Console.WriteLine();
        Console.WriteLine(ServersJson(exe));
        return Task.CompletedTask;
    }

    private static string ServersJson(string exe)
    {
        var config = new { mcpServers = new { winbox = new { command = exe, args = new[] { "mcp" } } } };
        return JsonSerializer.Serialize(config, Indented);
    }
}
