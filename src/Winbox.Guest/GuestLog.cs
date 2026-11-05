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

    public void Write(Exception error)
    {
        try
        {
            if (File.Exists(path) && new FileInfo(path).Length > MaxBytes) return;
            File.AppendAllText(path, $"{DateTime.UtcNow:O} {error.GetType().Name}: {error.Message}{Environment.NewLine}");
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
