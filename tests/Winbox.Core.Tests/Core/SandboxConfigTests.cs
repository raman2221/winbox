using System.Xml.Linq;

namespace Winbox.Core.Tests.Core;

public class SandboxConfigTests
{
    private static readonly WinboxPaths Paths = new(@"C:\Users\agent\AppData\Local\winbox");

    [Fact]
    public void ToXml_ToolsFolder_IsMappedReadOnly()
    {
        var folder = MappedFolder(Render(), WinboxPaths.GuestToolsDir);

        Assert.Equal(Paths.ToolsDir, folder.Element("HostFolder")?.Value);
        Assert.Equal("true", folder.Element("ReadOnly")?.Value);
    }

    [Fact]
    public void ToXml_ControlFolder_IsMappedWritable()
    {
        var folder = MappedFolder(Render(), WinboxPaths.GuestControlDir);

        Assert.Equal(Paths.Control.Root, folder.Element("HostFolder")?.Value);
        Assert.Equal("false", folder.Element("ReadOnly")?.Value);
    }

    [Fact]
    public void ToXml_LogonCommand_StartsGuestHelperOnControlFolder()
    {
        var command = Render().Element("LogonCommand")?.Element("Command")?.Value;

        Assert.Equal(@"C:\winbox\tools\Winbox.Guest.exe C:\winbox\control", command);
    }

    [Theory]
    [InlineData(true, "Enable")]
    [InlineData(false, "Disable")]
    public void ToXml_NetworkingOption_SetsNetworkingElement(bool networking, string expected)
    {
        var config = Render(new SandboxOptions(Networking: networking));

        Assert.Equal(expected, config.Element("Networking")?.Value);
    }

    [Fact]
    public void ToXml_MemoryOption_SetsMemoryInMegabytes()
    {
        var config = Render(new SandboxOptions(MemoryMb: 8192));

        Assert.Equal("8192", config.Element("MemoryInMB")?.Value);
    }

    [Theory]
    [InlineData("ClipboardRedirection")]
    [InlineData("AudioInput")]
    [InlineData("VideoInput")]
    [InlineData("PrinterRedirection")]
    public void ToXml_HostDevice_IsDisabled(string element)
    {
        Assert.Equal("Disable", Render().Element(element)?.Value);
    }

    private static XElement Render(SandboxOptions? options = null) =>
        XElement.Parse(SandboxConfig.ToXml(Paths, options ?? new SandboxOptions()));

    private static XElement MappedFolder(XElement config, string sandboxFolder) =>
        Assert.Single(
            config.Elements("MappedFolders").Elements("MappedFolder"),
            folder => folder.Element("SandboxFolder")?.Value == sandboxFolder);
}
