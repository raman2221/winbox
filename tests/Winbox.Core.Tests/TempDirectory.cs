namespace Winbox.Core.Tests;

/// <summary>A fresh directory under the system temp folder, deleted on dispose.</summary>
public sealed class TempDirectory : IDisposable
{
    private static readonly string TestsRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "winbox-tests");

    public string Path { get; } =
        Directory.CreateDirectory(System.IO.Path.Combine(TestsRoot, Guid.NewGuid().ToString("N"))).FullName;

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
