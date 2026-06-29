using Bank.Services.Common;
using FluentAssertions;

namespace Bank.Tests.Common;

public class BulgarianIdentifierValidatorTests
{
    [Theory]
    [InlineData("9001010000")]
    [InlineData("0142011239")]
    public void IsValidEgn_ForValidEgn_ReturnsTrue(string egn)
    {
        BulgarianIdentifierValidator.IsValidEgn(egn).Should().BeTrue();
    }

    [Theory]
    [InlineData("9001010001")]
    [InlineData("9042310000")]
    [InlineData("900101000")]
    [InlineData("900101000A")]
    public void IsValidEgn_ForInvalidEgn_ReturnsFalse(string egn)
    {
        BulgarianIdentifierValidator.IsValidEgn(egn).Should().BeFalse();
    }

    [Theory]
    [InlineData("831650349")]
    [InlineData("100000086")]
    [InlineData("8316503490007")]
    [InlineData("8316503490055")]
    public void IsValidEik_ForValidEik_ReturnsTrue(string eik)
    {
        BulgarianIdentifierValidator.IsValidEik(eik).Should().BeTrue();
    }

    [Theory]
    [InlineData("831650348")]
    [InlineData("8316503490008")]
    [InlineData("123")]
    [InlineData("83165034A")]
    public void IsValidEik_ForInvalidEik_ReturnsFalse(string eik)
    {
        BulgarianIdentifierValidator.IsValidEik(eik).Should().BeFalse();
    }
}
