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
internal sealed class McpCommand(GuestClient guest, SandboxLauncher launcher, ActionSender sender) : ICliCommand
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
        builder.Services.AddSingleton(guest).AddSingleton(launcher).AddSingleton<AgentView>()
            .AddSingleton(services => new DesktopActions(sender, services.GetRequiredService<AgentView>()));
        builder.Services.AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<WinboxTools>()
            .WithRequestFilters(filters => filters.AddCallToolFilter(ReportErrors));
        return builder.Build();
    }

    /// <summary>Turns any failure into a tool result the agent can read, instead of a generic failure.</summary>
    private static McpRequestHandler<CallToolRequestParams, CallToolResult> ReportErrors(
        McpRequestHandler<CallToolRequestParams, CallToolResult> next) =>
        async (context, cancel) =>
        {
            try
            {
                return await next(context, cancel);
            }
            catch (Exception error) when (error is not (OperationCanceledException or McpException))
            {
                throw new McpException(error.Message, error);
            }
        };
}
