using Winbox.Protocol;

namespace Winbox.Core.Tests.Protocol;

public class ReplyTests
{
    [Theory]
    [InlineData(true, "1920 1080")]
    [InlineData(true, "")]
    [InlineData(false, "Access is denied")]
    [InlineData(false, "fichier introuvable: é ✓")]
    public void Decode_OfEncodedReply_ReturnsEqualReply(bool ok, string message)
    {
        var reply = ok ? Reply.Success(message) : Reply.Failure(message);

        var decoded = Reply.Decode(reply.Encode());

        Assert.Equal(reply, decoded);
    }

    [Fact]
    public void Success_WithoutMessage_HasEmptyMessage()
    {
        var reply = Reply.Success();

        Assert.True(reply.Ok);
        Assert.Equal("", reply.Message);
    }

    [Fact]
    public void Encode_MessageWithNewline_StaysOnOneLine()
    {
        var reply = Reply.Failure("first\nsecond");

        var encoded = reply.Encode();

        Assert.Equal("err first second", encoded);
    }

    [Fact]
    public void Decode_LineWithTrailingNewline_TrimsTheMessage()
    {
        var decoded = Reply.Decode("ok 640 480\r\n");

        Assert.Equal(Reply.Success("640 480"), decoded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("maybe")]
    [InlineData("okay then")]
    [InlineData("errand ran")]
    [InlineData(" ok leading space")]
    public void Decode_UnknownStatusWord_ThrowsProtocolException(string line)
    {
        Assert.Throws<ProtocolException>(() => Reply.Decode(line));
    }
}
