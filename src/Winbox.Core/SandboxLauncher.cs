using System.Diagnostics;
using Winbox.Protocol;

namespace Winbox.Core;

/// <summary>Starts and stops the Windows Sandbox instance that hosts the agent's desktop.</summary>
public sealed class SandboxLauncher(WinboxPaths paths, GuestClient guest)
{
    private static readonly TimeSpan BootTimeout = TimeSpan.FromMinutes(3);

    // Process names differ between the inbox Sandbox and the newer Store-updated one.
    private static readonly string[] SandboxProcessNames =
        ["WindowsSandbox", "WindowsSandboxClient", "WindowsSandboxRemoteSession", "WindowsSandboxServer"];

    public static string SandboxExe => Path.Combine(Environment.SystemDirectory, "WindowsSandbox.exe");

    public static bool IsSandboxInstalled() => File.Exists(SandboxExe);

    public static bool IsSandboxRunning() =>
        SandboxProcessNames.Any(name => Process.GetProcessesByName(name).Length > 0);

    public async Task StartAsync(SandboxOptions options, IProgress<string> progress, CancellationToken cancel)
    {
        if (!IsSandboxInstalled()) throw new WinboxException(SetupAdvice.SandboxMissing);
        if (IsSandboxRunning()) throw new WinboxException("A Windows Sandbox is already running. Close it first.");
        PrepareHostFolders();
        await File.WriteAllTextAsync(paths.ConfigFile, SandboxConfig.ToXml(paths, options), cancel);
        Process.Start(new ProcessStartInfo(paths.ConfigFile) { UseShellExecute = true });
        progress.Report("Sandbox launched. Waiting for Windows to boot and the helper to check in...");
        await WaitForGuestAsync(cancel);
    }

    public async Task StopAsync(CancellationToken cancel)
    {
        if (!guest.IsGuestAlive()) throw new GuestUnavailableException();
        await guest.SendAsync(new ShutdownCommand(), cancel);
    }

    private void PrepareHostFolders()
    {
        ResetDirectory(paths.Control.Root);
        paths.Control.EnsureCreated();
        ResetDirectory(paths.ToolsDir);
        CopyGuestHelper();
    }

    private void CopyGuestHelper()
    {
        var source = WinboxPaths.BundledGuestDir;
        if (!File.Exists(Path.Combine(source, WinboxPaths.GuestExeName)))
            throw new WinboxException($"The guest helper is missing from {source}. Reinstall winbox.");
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(paths.ToolsDir, Path.GetFileName(file)));
    }

    private static void ResetDirectory(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        Directory.CreateDirectory(path);
    }

    private async Task WaitForGuestAsync(CancellationToken cancel)
    {
        var deadline = DateTime.UtcNow + BootTimeout;
        while (!guest.IsGuestAlive())
        {
            if (DateTime.UtcNow > deadline)
                throw new WinboxException("The sandbox started, but the helper never checked in. Is the window open?");
            await Task.Delay(500, cancel);
        }
    }
}
