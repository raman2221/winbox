using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Winbox.Cli.CommandLine;
using Winbox.Cli.Desktop;
using Winbox.Core;

namespace Winbox.Cli.Mcp;

/// <summary>Runs winbox as a stdio MCP server, so an agent can drive the sandbox desktop.</summary>
internal sealed class McpCommand(GuestClient guest, SandboxLauncher launcher, DesktopActions actions) : ICliCommand
{
    public CommandHelp Help { get; } = new("mcp", "Run the MCP server on stdio (started by your agent)");

    public Task RunAsync(CommandArgs args, CancellationToken cancel)
    {
        args.ExpectBetween(0, 0);
        return BuildHost().RunAsync(cancel);
    }

    private IHost BuildHost()
    {
        var builder = Host.CreateApplicationBuilder();
        // stdout carries the MCP protocol, so every log line has to go to stderr.
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
        builder.Services.AddSingleton(guest).AddSingleton(launcher).AddSingleton(actions);
        builder.Services.AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<WinboxTools>()
            .WithRequestFilters(filters => filters.AddCallToolFilter(ReportWinboxErrors));
        return builder.Build();
    }

    /// <summary>Turns winbox errors into tool results the agent can read, instead of a generic failure.</summary>
    private static McpRequestHandler<CallToolRequestParams, CallToolResult> ReportWinboxErrors(
        McpRequestHandler<CallToolRequestParams, CallToolResult> next) =>
        async (context, cancel) =>
        {
            try
            {
                return await next(context, cancel);
            }
            catch (WinboxException error)
            {
                throw new McpException(error.Message, error);
            }
        };
}
