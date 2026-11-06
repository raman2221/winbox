using System;
using System.IO;

namespace Winbox.Guest;

/// <summary>
/// Appends failures to a log in the shared control folder, so problems inside the sandbox are visible
/// from the host. Logging itself must never become a new way to crash the helper.
/// </summary>
internal sealed class GuestLog(string path)
{
    private const long MaxBytes = 1_000_000;

    // A file locked for minutes fails ~90 times a second; one line per minute says the same thing.
    private static readonly TimeSpan RepeatWindow = TimeSpan.FromMinutes(1);

    private readonly object _gate = new();
    private string _lastMessage = "";
    private DateTime _lastWrittenAt = DateTime.MinValue;

    public void Write(Exception error)
    {
        var message = $"{error.GetType().Name}: {error.Message}";
        lock (_gate)
        {
            if (IsRepeat(message)) return;
            _lastMessage = message;
            _lastWrittenAt = DateTime.UtcNow;
            TryAppend(message);
        }
    }

    private bool IsRepeat(string message) =>
        message == _lastMessage && DateTime.UtcNow - _lastWrittenAt < RepeatWindow;

    private void TryAppend(string message)
    {
        try
        {
            RotateIfFull();
            File.AppendAllText(path, $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>Keeps the newest entries by moving a full log aside, rather than going silent.</summary>
    private void RotateIfFull()
    {
        if (!File.Exists(path) || new FileInfo(path).Length < MaxBytes) return;
        var previous = path + ".1";
        if (File.Exists(previous)) File.Delete(previous);
        File.Move(path, previous);
    }
}
