using Winbox.Protocol;

namespace Winbox.Core;

/// <summary>Where winbox keeps its files on the host, and where they appear inside the sandbox.</summary>
public sealed class WinboxPaths(string root)
{
    public const string GuestToolsDir = @"C:\winbox\tools";
    public const string GuestControlDir = @"C:\winbox\control";
    public const string GuestExeName = "Winbox.Guest.exe";

    public static WinboxPaths Default() =>
        new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "winbox"));

    public string Root { get; } = root;
    public string ToolsDir => Path.Combine(Root, "tools");
    public string ConfigFile => Path.Combine(Root, "winbox.wsb");
    public ControlPaths Control => new(Path.Combine(Root, "control"));

    /// <summary>The guest helper build that ships next to the winbox executable.</summary>
    public static string BundledGuestDir => Path.Combine(AppContext.BaseDirectory, "guest");
}
