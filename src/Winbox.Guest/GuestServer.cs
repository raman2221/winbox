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

    // Longer than the host's reply timeout: anything older was abandoned, and replaying a stale click is worse
    // than dropping it.
    private static readonly TimeSpan RequestExpiry = TimeSpan.FromSeconds(20);

    private readonly GuestLog _log = new(paths.LogFile);

    public void Run()
    {
        paths.EnsureCreated();
        StartHeartbeat();
        using var watcher = WatchRequests();
        while (true)
        {
            Survive(HandlePendingRequests);
            Thread.Sleep(PollInterval);
        }
    }

    /// <summary>
    /// Keeps a change notification open on the requests folder. Over Windows Sandbox's mapped-folder share the
    /// directory listing is otherwise cached, and new requests stayed invisible for up to ~20 s. An open watcher
    /// keeps that cache current (measured in a real sandbox: ~20 s down to 10-30 ms). Polling still does the work.
    /// </summary>
    private FileSystemWatcher WatchRequests() => new(paths.RequestsDir) { EnableRaisingEvents = true };

    /// <summary>Beats on its own thread so a long, human-paced action never makes the guest look dead.</summary>
    private void StartHeartbeat()
    {
        var thread = new Thread(() =>
        {
            while (true)
            {
                Survive(WriteHeartbeat);
                Thread.Sleep(HeartbeatInterval);
            }
        }) { IsBackground = true, Name = "heartbeat" };
        thread.Start();
    }

    private void WriteHeartbeat()
    {
        var size = screen.Size;
        var status = new GuestStatus(DateTimeOffset.UtcNow, size.Width, size.Height, Version);
        ControlPaths.WriteAtomically(paths.StatusFile, status.Encode());
    }

    private void HandlePendingRequests()
    {
        var requests = Directory.GetFiles(paths.RequestsDir, "*.req").OrderBy(File.GetCreationTimeUtc).ToList();
        foreach (var file in requests) Survive(() => HandleRequest(file));
    }

    /// <summary>A locked or vanished request file is skipped and retried on the next poll.</summary>
    private void HandleRequest(string file)
    {
        var id = Path.GetFileNameWithoutExtension(file);
        var isExpired = DateTime.UtcNow - File.GetCreationTimeUtc(file) > RequestExpiry;
        var line = File.ReadAllText(file);
        File.Delete(file);
        var reply = isExpired ? Reply.Failure("Request expired before the guest reached it") : Execute(id, line);
        ControlPaths.WriteAtomically(paths.ReplyFile(id), reply.Encode());
    }

    private Reply Execute(string id, string line) => executor.Execute(paths.ImageFile(id), line);

    /// <summary>One bad pass (a locked file, a share hiccup) must never end the helper.</summary>
    private void Survive(Action step)
    {
        try
        {
            step();
        }
        catch (Exception error)
        {
            _log.Write(error);
        }
    }
}
