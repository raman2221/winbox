using Winbox.Protocol;

namespace Winbox.Core.Tests.Protocol;

public class ProgramLineTests
{
    [Theory]
    [InlineData("msedge https://x.com/?a=1&b=2", "msedge", "https://x.com/?a=1&b=2")]
    [InlineData("cmd /k echo %PATH% | more", "cmd", "/k echo %PATH% | more")]
    [InlineData("notepad \"C:\\some file.txt\"", "notepad", "\"C:\\some file.txt\"")]
    public void Split_ProgramAndArguments_KeepsArgumentsVerbatim(string line, string program, string arguments)
    {
        var split = ProgramLine.Split(line);

        Assert.Equal((program, arguments), split);
    }

    [Fact]
    public void Split_QuotedProgramPath_DropsTheQuotesAroundIt()
    {
        var split = ProgramLine.Split("\"C:\\My Tools\\app.exe\"  --flag \"x y\"");

        Assert.Equal(("C:\\My Tools\\app.exe", "--flag \"x y\""), split);
    }

    [Theory]
    [InlineData("notepad")]
    [InlineData("  notepad \t ")]
    [InlineData("\"notepad\"")]
    public void Split_ProgramAlone_HasNoArguments(string line)
    {
        Assert.Equal(("notepad", ""), ProgramLine.Split(line));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\"\" --flag")]
    [InlineData("\"C:\\My Tools\\app.exe --flag")]
    public void Split_NoUsableProgram_ThrowsProtocolException(string line)
    {
        Assert.Throws<ProtocolException>(() => ProgramLine.Split(line));
    }
}
