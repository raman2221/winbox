using System;
using System.IO;
using System.Linq;
using System.Threading;
using Winbox.Protocol;

namespace Winbox.Guest;

/// <summary>Polls the requests folder, runs each command, and keeps the heartbeat fresh.</summary>
internal sealed class GuestServer(ControlPaths paths, CommandExecutor executor, ScreenCapture screen)
{
    public const string Version = "0.1.0";
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(10);
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(1);

    private DateTime _lastHeartbeat = DateTime.MinValue;

    public void Run()
    {
        paths.EnsureCreated();
        while (true)
        {
            WriteHeartbeatIfDue();
            HandlePendingRequests();
            Thread.Sleep(PollInterval);
        }
    }

    private void WriteHeartbeatIfDue()
    {
        if (DateTime.UtcNow - _lastHeartbeat < HeartbeatInterval) return;
        var size = screen.Size;
        var status = new GuestStatus(DateTimeOffset.UtcNow, size.Width, size.Height, Version);
        ControlPaths.WriteAtomically(paths.StatusFile, status.Encode());
        _lastHeartbeat = DateTime.UtcNow;
    }

    private void HandlePendingRequests()
    {
        var requests = Directory.GetFiles(paths.RequestsDir, "*.cmd").OrderBy(File.GetCreationTimeUtc);
        foreach (var file in requests) HandleRequest(file);
    }

    private void HandleRequest(string file)
    {
        var id = Path.GetFileNameWithoutExtension(file);
        var line = File.ReadAllText(file);
        File.Delete(file);
        var reply = executor.Execute(paths.ImageFile(id), line);
        ControlPaths.WriteAtomically(paths.ReplyFile(id), reply.Encode());
    }
}
