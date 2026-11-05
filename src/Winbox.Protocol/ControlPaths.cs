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

    public string RequestFile(string id) => Path.Combine(RequestsDir, id + ".cmd");
    public string ReplyFile(string id) => Path.Combine(RepliesDir, id + ".reply");
    public string ImageFile(string id) => Path.Combine(RepliesDir, id + ".png");

    public void EnsureCreated()
    {
        Directory.CreateDirectory(RequestsDir);
        Directory.CreateDirectory(RepliesDir);
    }

    /// <summary>Writes via a temp file and rename, so the reader never sees a half-written file.</summary>
    public static void WriteAtomically(string path, string contents)
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, contents);
        if (File.Exists(path)) File.Delete(path);
        File.Move(temp, path);
    }
}
