namespace Winbox.Core.Tests.Core;

public class SetupAdviceTests
{
    [Theory]
    [InlineData("Core")]
    [InlineData("CoreN")]
    [InlineData("CoreSingleLanguage")]
    [InlineData("CoreCountrySpecific")]
    public void IsHomeEdition_HomeEditionId_ReturnsTrue(string editionId)
    {
        Assert.True(SetupAdvice.IsHomeEdition(editionId));
    }

    [Theory]
    [InlineData("Professional")]
    [InlineData("Enterprise")]
    [InlineData("Education")]
    [InlineData("")]
    public void IsHomeEdition_OtherEditionId_ReturnsFalse(string editionId)
    {
        Assert.False(SetupAdvice.IsHomeEdition(editionId));
    }
}
