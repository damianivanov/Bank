using Bank.Services.Common;

namespace Bank.Tests;

public class BulgarianIdentifierValidatorTests
{
    [Theory]
    [InlineData("9001010000")]
    [InlineData("0142011239")]
    public void IsValidEgn_ReturnsTrue_ForValidEgn(string egn)
    {
        Assert.True(BulgarianIdentifierValidator.IsValidEgn(egn));
    }

    [Theory]
    [InlineData("9001010001")]
    [InlineData("9042310000")]
    [InlineData("900101000")]
    [InlineData("900101000A")]
    public void IsValidEgn_ReturnsFalse_ForInvalidEgn(string egn)
    {
        Assert.False(BulgarianIdentifierValidator.IsValidEgn(egn));
    }

    [Theory]
    [InlineData("831650349")]
    [InlineData("100000086")]
    [InlineData("8316503490007")]
    [InlineData("8316503490055")]
    public void IsValidEik_ReturnsTrue_ForValidEik(string eik)
    {
        Assert.True(BulgarianIdentifierValidator.IsValidEik(eik));
    }

    [Theory]
    [InlineData("831650348")]
    [InlineData("8316503490008")]
    [InlineData("123")]
    [InlineData("83165034A")]
    public void IsValidEik_ReturnsFalse_ForInvalidEik(string eik)
    {
        Assert.False(BulgarianIdentifierValidator.IsValidEik(eik));
    }
}
