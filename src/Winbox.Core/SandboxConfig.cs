using System.Xml.Linq;

namespace Winbox.Core;

public sealed record SandboxOptions(int MemoryMb = 4096, bool Networking = true);

/// <summary>Builds the .wsb file that tells Windows Sandbox what to share and what to run at logon.</summary>
public static class SandboxConfig
{
    public static string ToXml(WinboxPaths paths, SandboxOptions options)
    {
        var logonCommand = $@"{WinboxPaths.GuestToolsDir}\{WinboxPaths.GuestExeName} {WinboxPaths.GuestControlDir}";
        var config = new XElement("Configuration",
            new XElement("MemoryInMB", options.MemoryMb),
            new XElement("Networking", options.Networking ? "Enable" : "Disable"),
            new XElement("vGPU", "Enable"),
            // Host clipboard, audio, camera and printers stay out of the agent's reach.
            new XElement("ClipboardRedirection", "Disable"),
            new XElement("AudioInput", "Disable"),
            new XElement("VideoInput", "Disable"),
            new XElement("PrinterRedirection", "Disable"),
            new XElement("MappedFolders",
                MappedFolder(paths.ToolsDir, WinboxPaths.GuestToolsDir, readOnly: true),
                MappedFolder(paths.Control.Root, WinboxPaths.GuestControlDir, readOnly: false)),
            new XElement("LogonCommand", new XElement("Command", logonCommand)));
        return config.ToString();
    }

    private static XElement MappedFolder(string hostPath, string sandboxPath, bool readOnly) =>
        new("MappedFolder",
            new XElement("HostFolder", hostPath),
            new XElement("SandboxFolder", sandboxPath),
            new XElement("ReadOnly", readOnly ? "true" : "false"));
}
