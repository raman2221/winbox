using Winbox.Protocol;

namespace Winbox.Core.Tests.Protocol;

public sealed class ControlPathsTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void FileNames_FollowTheSharedFolderLayout()
    {
        var paths = new ControlPaths(@"C:\control");

        Assert.Equal(@"C:\control\requests\abc.req", paths.RequestFile("abc"));
        Assert.Equal(@"C:\control\replies\abc.reply", paths.ReplyFile("abc"));
        Assert.Equal(@"C:\control\replies\abc.png", paths.ImageFile("abc"));
        Assert.Equal(@"C:\control\guest.status", paths.StatusFile);
    }

    [Fact]
    public void EnsureCreated_CreatesRequestAndReplyFolders()
    {
        var paths = new ControlPaths(_temp.File("control"));

        paths.EnsureCreated();

        Assert.True(Directory.Exists(paths.RequestsDir));
        Assert.True(Directory.Exists(paths.RepliesDir));
    }

    [Fact]
    public void WriteAtomically_NewFile_WritesContentsAndLeavesNoTempFile()
    {
        var path = _temp.File("status");

        ControlPaths.WriteAtomically(path, "hello");

        Assert.Equal("hello", File.ReadAllText(path));
        Assert.Equal([path], Directory.GetFiles(_temp.Path));
    }

    [Fact]
    public void WriteAtomically_ExistingFile_ReplacesContents()
    {
        var path = _temp.File("status");
        File.WriteAllText(path, "old contents that are longer");

        ControlPaths.WriteAtomically(path, "new");

        Assert.Equal("new", File.ReadAllText(path));
    }
}
