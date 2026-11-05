using Winbox.Protocol;

namespace Winbox.Core.Tests.Protocol;

public class GuestStatusTests
{
    private static readonly DateTimeOffset WrittenAt = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Decode_OfEncodedStatus_ReturnsEqualStatus()
    {
        var status = new GuestStatus(WrittenAt, 1920, 1080, "0.1.0");

        var decoded = GuestStatus.Decode(status.Encode());

        Assert.Equal(status, decoded);
    }

    [Theory]
    [InlineData(-500, true)]
    [InlineData(0, true)]
    [InlineData(2999, true)]
    [InlineData(3000, false)]
    [InlineData(60_000, false)]
    public void IsAliveAt_ElapsedMilliseconds_IsAliveOnlyBeforeStaleBoundary(int elapsedMs, bool expected)
    {
        var status = new GuestStatus(WrittenAt, 800, 600, "0.1.0");

        var alive = status.IsAliveAt(WrittenAt.AddMilliseconds(elapsedMs));

        Assert.Equal(expected, alive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1 2 3")]
    [InlineData("1 2 3 4 5")]
    [InlineData("noon 1920 1080 0.1.0")]
    [InlineData("1700000000000 wide 1080 0.1.0")]
    [InlineData("1700000000000 1920 1080.5 0.1.0")]
    public void Decode_MalformedLine_ThrowsProtocolException(string line)
    {
        Assert.Throws<ProtocolException>(() => GuestStatus.Decode(line));
    }
}
