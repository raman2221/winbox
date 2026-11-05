namespace Winbox.Cli.Commands;

/// <summary>Detects Windows Terminal, the console host that can render the viewer's Sixel frames.</summary>
internal static class WindowsTerminal
{
    private const string ExeName = "wt.exe";

    public static bool IsInstalled() => IsCurrentHost() || SearchPath().Any(File.Exists);

    // Windows Terminal sets WT_SESSION in every shell it hosts.
    private static bool IsCurrentHost() => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WT_SESSION"));

    private static IEnumerable<string> SearchPath() =>
        (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(directory => Path.Combine(directory, ExeName));
}
