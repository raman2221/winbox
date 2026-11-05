using System.IO;

namespace Winbox.Protocol;

/// <summary>
/// Layout of the shared control folder. The host drops request files, the guest answers with reply files,
/// and the guest refreshes a status file as a heartbeat.
/// </summary>
public sealed class ControlPaths(string root)
{
    public string Root { get; } = root;
    public string RequestsDir => Path.Combine(Root, "requests");
    public string RepliesDir => Path.Combine(Root, "replies");
    public string StatusFile => Path.Combine(Root, "guest.status");
    public string LogFile => Path.Combine(Root, "guest.log");

    public string RequestFile(string id) => Path.Combine(RequestsDir, id + ".cmd");
    public string ReplyFile(string id) => Path.Combine(RepliesDir, id + ".reply");
    public string ImageFile(string id) => Path.Combine(RepliesDir, id + ".png");

    public void EnsureCreated()
    {
        Directory.CreateDirectory(RequestsDir);
        Directory.CreateDirectory(RepliesDir);
    }

    /// <summary>
    /// Writes via a temp file and a single replace, so a reader never sees a half-written file and there is
    /// no moment where the file is missing (delete-then-move failed whenever a reader held the file open).
    /// </summary>
    public static void WriteAtomically(string path, string contents)
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, contents);
        if (File.Exists(path)) File.Replace(temp, path, null);
        else File.Move(temp, path);
    }

    /// <summary>Reads with full sharing, so the reader never blocks the writer's replace.</summary>
    public static string ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
