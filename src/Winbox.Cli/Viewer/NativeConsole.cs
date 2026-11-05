using System.Runtime.InteropServices;
using Winbox.Core;

namespace Winbox.Cli.Viewer;

/// <summary>The few Win32 console calls .NET doesn't expose: reading and setting console modes.</summary>
internal static class NativeConsole
{
    public const int StandardInput = -10;
    public const int StandardOutput = -11;

    public static IntPtr Handle(int standardDevice) => GetStdHandle(standardDevice);

    public static uint ModeOf(IntPtr handle)
    {
        if (GetConsoleMode(handle, out var mode)) return mode;
        throw new WinboxException(
            "`winbox view` needs an interactive terminal such as Windows Terminal (input or output is redirected).");
    }

    public static void SetMode(IntPtr handle, uint mode)
    {
        if (SetConsoleMode(handle, mode)) return;
        throw new WinboxException($"Couldn't switch the console mode (Win32 error {Marshal.GetLastPInvokeError()}).");
    }

    /// <summary>Best-effort restore used while unwinding, where throwing would hide the original error.</summary>
    public static void TrySetMode(IntPtr handle, uint mode) => SetConsoleMode(handle, mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int standardDevice);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr handle, out uint mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr handle, uint mode);
}
