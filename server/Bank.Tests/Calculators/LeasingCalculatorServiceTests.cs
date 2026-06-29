using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Calculators;
using Bank.Services.Calculators;
using FluentAssertions;

namespace Bank.Tests.Calculators;

public class LeasingCalculatorServiceTests
{
    private readonly LeasingCalculatorService _service;

    public LeasingCalculatorServiceTests()
    {
        _service = new LeasingCalculatorService();
    }

    #region Invalid data

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public async Task CalculateAsync_WithInvalidPrice_ShouldThrowException(decimal price)
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = price,
            DownPayment = 5000m,
            LeasingTerm = 36,
            MonthlyPayment = 800m
        };

        await Assert.ThrowsAsync<BankException>(() => _service.CalculateAsync(request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-12)]
    public async Task CalculateAsync_WithInvalidLeasingTerm_ShouldThrowException(int term)
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 30000m,
            DownPayment = 5000m,
            LeasingTerm = term,
            MonthlyPayment = 800m
        };

        await Assert.ThrowsAsync<BankException>(() => _service.CalculateAsync(request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task CalculateAsync_WithInvalidMonthlyPayment_ShouldThrowException(decimal payment)
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 30000m,
            DownPayment = 5000m,
            LeasingTerm = 36,
            MonthlyPayment = payment
        };

        await Assert.ThrowsAsync<BankException>(() => _service.CalculateAsync(request));
    }

    [Fact]
    public async Task CalculateAsync_WithDownPaymentEqualToPrice_ShouldThrowException()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 30000m,
            DownPayment = 30000m,
            LeasingTerm = 36,
            MonthlyPayment = 800m
        };

        await Assert.ThrowsAsync<BankException>(() => _service.CalculateAsync(request));
    }

    [Fact]
    public async Task CalculateAsync_WithDownPaymentGreaterThanPrice_ShouldThrowException()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 30000m,
            DownPayment = 35000m,
            LeasingTerm = 36,
            MonthlyPayment = 800m
        };

        await Assert.ThrowsAsync<BankException>(() => _service.CalculateAsync(request));
    }

    [Fact]
    public async Task CalculateAsync_WithNegativeDownPayment_ShouldThrowException()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 30000m,
            DownPayment = -1000m,
            LeasingTerm = 36,
            MonthlyPayment = 800m
        };

        await Assert.ThrowsAsync<BankException>(() => _service.CalculateAsync(request));
    }

    [Fact]
    public async Task CalculateAsync_WithExcessiveLeasingTerm_ShouldThrowException()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 30000m,
            DownPayment = 5000m,
            LeasingTerm = 601,
            MonthlyPayment = 800m
        };

        await Assert.ThrowsAsync<BankException>(() => _service.CalculateAsync(request));
    }

    #endregion

    #region Others

    [Fact]
    public async Task CalculateAsync_WithValidBasicRequest_ShouldReturnCorrectResponse()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 30000m,
            DownPayment = 5000m,
            LeasingTerm = 36,
            MonthlyPayment = 800m
        };

        var result = await _service.CalculateAsync(request);

        result.Should().NotBeNull();
        result.TotalCost.Should().Be(30000m);
        result.EffectiveInterestRate.Should().BeGreaterThan(0);
        result.TotalPaid.Should().BeGreaterThan(request.PriceWithVAT);
        result.TotalMarkup.Should().BeGreaterThan(0);
        result.MarkupPercentage.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CalculateAsync_WithProcessingFeePercent_ShouldCalculateCorrectly()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 30000m,
            DownPayment = 5000m,
            LeasingTerm = 36,
            MonthlyPayment = 800m,
            ProcessingFee = new Fee { Type = FeeType.Percent, Value = 2m }
        };

        var result = await _service.CalculateAsync(request);

        decimal expectedProcessingFee = 30000m * 0.02m;
        result.ProcessingFeeAmount.Should().Be(expectedProcessingFee);
        result.TotalPaid.Should().Be(request.DownPayment + (request.MonthlyPayment * request.LeasingTerm) + expectedProcessingFee);
    }

    [Fact]
    public async Task CalculateAsync_WithProcessingFeeCurrency_ShouldCalculateCorrectly()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 30000m,
            DownPayment = 5000m,
            LeasingTerm = 36,
            MonthlyPayment = 800m,
            ProcessingFee = new Fee { Type = FeeType.Currency, Value = 500m }
        };

        var result = await _service.CalculateAsync(request);

        result.ProcessingFeeAmount.Should().Be(500m);
        result.TotalPaid.Should().Be(request.DownPayment + (request.MonthlyPayment * request.LeasingTerm) + 500m);
    }

    [Fact]
    public async Task CalculateAsync_WithNoProcessingFee_ShouldHaveZeroFee()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 30000m,
            DownPayment = 5000m,
            LeasingTerm = 36,
            MonthlyPayment = 800m
        };

        var result = await _service.CalculateAsync(request);

        result.ProcessingFeeAmount.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateAsync_TotalPaidShouldBeCorrect()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 20000m,
            DownPayment = 4000m,
            LeasingTerm = 24,
            MonthlyPayment = 750m,
            ProcessingFee = new Fee { Type = FeeType.Currency, Value = 300m }
        };

        var result = await _service.CalculateAsync(request);

        decimal expectedTotal = request.DownPayment + (request.MonthlyPayment * request.LeasingTerm) + 300m;
        result.TotalPaid.Should().Be(expectedTotal);
    }

    [Fact]
    public async Task CalculateAsync_TotalMarkupShouldBeCorrect()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 20000m,
            DownPayment = 4000m,
            LeasingTerm = 24,
            MonthlyPayment = 750m,
            ProcessingFee = new Fee { Type = FeeType.Currency, Value = 300m }
        };

        var result = await _service.CalculateAsync(request);

        result.TotalMarkup.Should().Be(2300m);
    }

    [Fact]
    public async Task CalculateAsync_MarkupPercentageShouldBeCorrect()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 20000m,
            DownPayment = 4000m,
            LeasingTerm = 24,
            MonthlyPayment = 750m,
            ProcessingFee = new Fee { Type = FeeType.Currency, Value = 300m }
        };

        var result = await _service.CalculateAsync(request);

        result.MarkupPercentage.Should().Be(11.50m);
    }

    [Fact]
    public async Task CalculateAsync_EffectiveInterestRateShouldBeReasonable()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 30000m,
            DownPayment = 5000m,
            LeasingTerm = 36,
            MonthlyPayment = 800m
        };

        var result = await _service.CalculateAsync(request);

        result.EffectiveInterestRate.Should().BeGreaterThan(0);
        result.EffectiveInterestRate.Should().BeLessThan(100);
    }

    [Fact]
    public async Task CalculateAsync_WithHigherMonthlyPayment_ShouldHaveHigherInterestRate()
    {
        var requestLow = new LeasingCalculatorRequest
        {
            PriceWithVAT = 30000m,
            DownPayment = 5000m,
            LeasingTerm = 36,
            MonthlyPayment = 750m
        };

        var requestHigh = new LeasingCalculatorRequest
        {
            PriceWithVAT = 30000m,
            DownPayment = 5000m,
            LeasingTerm = 36,
            MonthlyPayment = 850m
        };

        var resultLow = await _service.CalculateAsync(requestLow);
        var resultHigh = await _service.CalculateAsync(requestHigh);

        resultHigh.EffectiveInterestRate.Should().BeGreaterThan(resultLow.EffectiveInterestRate);
    }

    [Fact]
    public async Task CalculateAsync_WithZeroDownPayment_ShouldCalculateCorrectly()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 30000m,
            DownPayment = 0m,
            LeasingTerm = 36,
            MonthlyPayment = 900m
        };

        var result = await _service.CalculateAsync(request);

        result.Should().NotBeNull();
        result.TotalCost.Should().Be(30000m);
        result.TotalPaid.Should().Be(request.MonthlyPayment * request.LeasingTerm);
    }

    [Fact]
    public async Task CalculateAsync_ResultsShouldBeRoundedTo2Decimals()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 30000m,
            DownPayment = 5000m,
            LeasingTerm = 36,
            MonthlyPayment = 800m,
            ProcessingFee = new Fee { Type = FeeType.Percent, Value = 2.5m }
        };

        var result = await _service.CalculateAsync(request);

        result.ProcessingFeeAmount.Should().Be(Math.Round(result.ProcessingFeeAmount, 2));
        result.TotalPaid.Should().Be(Math.Round(result.TotalPaid, 2));
        result.TotalMarkup.Should().Be(Math.Round(result.TotalMarkup, 2));
        result.MarkupPercentage.Should().Be(Math.Round(result.MarkupPercentage, 2));
        result.EffectiveInterestRate.Should().Be(Math.Round(result.EffectiveInterestRate, 2));
    }

    [Fact]
    public async Task CalculateAsync_WithShortTerm_ShouldCalculateCorrectly()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 10000m,
            DownPayment = 2000m,
            LeasingTerm = 12,
            MonthlyPayment = 700m
        };

        var result = await _service.CalculateAsync(request);

        result.Should().NotBeNull();
        result.TotalCost.Should().Be(10000m);
        result.TotalPaid.Should().Be(2000m + (700m * 12));
        result.EffectiveInterestRate.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CalculateAsync_WithLongTerm_ShouldCalculateCorrectly()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 50000m,
            DownPayment = 10000m,
            LeasingTerm = 60,
            MonthlyPayment = 800m
        };

        var result = await _service.CalculateAsync(request);

        result.Should().NotBeNull();
        result.TotalCost.Should().Be(50000m);
        result.TotalPaid.Should().Be(10000m + (800m * 60));
        result.EffectiveInterestRate.Should().BeGreaterThan(0);
    }

    #endregion

    #region Real test from MoitePari.bg examples

    [Fact]
    public async Task CalculateAsync_ShouldReturnCorrectTotals()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 3000m,
            DownPayment = 300m,
            LeasingTerm = 24,
            MonthlyPayment = 150m,
            ProcessingFee = new Fee { Type = FeeType.Currency, Value = 10m }
        };

        var result = await _service.CalculateAsync(request);

        result.Should().NotBeNull();
        result.TotalCost.Should().Be(3000m);

        result.ProcessingFeeAmount.Should().Be(10m);
        result.TotalPaid.Should().Be(3910m);
        result.TotalMarkup.Should().Be(910m);
        result.MarkupPercentage.Should().Be(30.33m);
        result.EffectiveInterestRate.Should().BeApproximately(34.10m, 0.01m);
    }

    [Fact]
    public async Task CalculateAsync_NoDownPaymentNoFee()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 1200m,
            DownPayment = 0m,
            LeasingTerm = 12,
            MonthlyPayment = 110m,
            ProcessingFee = null
        };

        var result = await _service.CalculateAsync(request);

        result.Should().NotBeNull();
        result.TotalCost.Should().Be(1200m);

        result.ProcessingFeeAmount.Should().Be(0m);
        result.TotalPaid.Should().Be(1320m);
        result.TotalMarkup.Should().Be(120m);
        result.MarkupPercentage.Should().Be(10.00m);

        result.EffectiveInterestRate.Should().Be(19.53m);
    }

    [Fact]
    public async Task CalculateAsync_LongTermWithDownPaymentAndFee()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 5000m,
            DownPayment = 1000m,
            LeasingTerm = 36,
            MonthlyPayment = 140m,
            ProcessingFee = new Fee { Type = FeeType.Currency, Value = 50m }
        };

        var result = await _service.CalculateAsync(request);

        result.Should().NotBeNull();
        result.TotalCost.Should().Be(5000m);

        result.ProcessingFeeAmount.Should().Be(50m);
        result.TotalPaid.Should().Be(6090m);
        result.TotalMarkup.Should().Be(1090m);
        result.MarkupPercentage.Should().Be(21.80m);

        result.EffectiveInterestRate.Should().BeApproximately(17.90m, 0.01m);
    }

    [Fact]
    public async Task CalculateAsync_Decimals()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 799.99m,
            DownPayment = 199.99m,
            LeasingTerm = 10,
            MonthlyPayment = 70.55m,
            ProcessingFee = new Fee { Type = FeeType.Currency, Value = 15m }
        };

        var result = await _service.CalculateAsync(request);

        result.Should().NotBeNull();
        result.TotalCost.Should().Be(799.99m);

        result.ProcessingFeeAmount.Should().Be(15m);
        result.TotalPaid.Should().Be(920.49m);
        result.TotalMarkup.Should().Be(120.50m);
        result.MarkupPercentage.Should().Be(15.06m);

        result.ProcessingFeeAmount.Should().Be(Math.Round(result.ProcessingFeeAmount, 2));
        result.TotalPaid.Should().Be(Math.Round(result.TotalPaid, 2));
        result.TotalMarkup.Should().Be(Math.Round(result.TotalMarkup, 2));
        result.MarkupPercentage.Should().Be(Math.Round(result.MarkupPercentage, 2));
        result.EffectiveInterestRate.Should().Be(Math.Round(result.EffectiveInterestRate, 2));
    }

    [Fact]
    public async Task CalculateAsync_ShortTerm()
    {
        var request = new LeasingCalculatorRequest
        {
            PriceWithVAT = 2500m,
            DownPayment = 250m,
            LeasingTerm = 6,
            MonthlyPayment = 400m,
            ProcessingFee = new Fee { Type = FeeType.Currency, Value = 25m }
        };

        var result = await _service.CalculateAsync(request);

        result.Should().NotBeNull();
        result.TotalCost.Should().Be(2500m);

        result.ProcessingFeeAmount.Should().Be(25m);
        result.TotalPaid.Should().Be(2675m);
        result.TotalMarkup.Should().Be(175m);
        result.MarkupPercentage.Should().Be(7.00m);

        result.EffectiveInterestRate.Should().BeApproximately(29.95m, 0.01m);
    }

    #endregion
}
