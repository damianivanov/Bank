using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Calculators;
using Bank.Services.Calculators;
using FluentAssertions;

namespace Bank.Tests.Calculators;

public class RefinancingCalculatorServiceTests
{
    private readonly RefinancingCalculatorService _service;

    public RefinancingCalculatorServiceTests()
    {
        _service = new RefinancingCalculatorService();
    }

    [Fact]
    public async Task CalculateAsync_WithValidRequest_ShouldReturnCorrectResponse()
    {
        var request = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 100000m,
                AnnualRatePercent = 10m,
                TermMonths = 120,
                PaymentsMade = 24,
                PrepaymentFeePercent = 1m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 7m,
                OriginationFeePercent = 1m,
                OriginationFeeFixed = 500m
            }
        };

        var result = await _service.CalculateAsync(request);

        result.Should().NotBeNull();
        result.RemainingMonths.Should().Be(96);
        result.RemainingPrincipal.Should().BeGreaterThan(0);
        result.Current.Should().NotBeNull();
        result.New.Should().NotBeNull();
        result.Current.MonthlyPayment.Should().BeGreaterThan(0);
        result.New.MonthlyPayment.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CalculateAsync_WithLowerNewRate_ShouldShowPositiveSavings()
    {
        var request = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 100000m,
                AnnualRatePercent = 10m,
                TermMonths = 120,
                PaymentsMade = 24,
                PrepaymentFeePercent = 1m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 6m,
                OriginationFeePercent = 0.5m,
                OriginationFeeFixed = 200m
            }
        };

        var result = await _service.CalculateAsync(request);

        result.Savings.Should().BeGreaterThan(0);
        result.ShouldYouSwitch.Should().BeTrue();
        result.New.MonthlyPayment.Should().BeLessThan(result.Current.MonthlyPayment);
    }

    [Fact]
    public async Task CalculateAsync_WithHigherNewRate_ShouldShowNegativeSavings()
    {
        var request = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 100000m,
                AnnualRatePercent = 7m,
                TermMonths = 120,
                PaymentsMade = 24,
                PrepaymentFeePercent = 1m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 10m,
                OriginationFeePercent = 2m,
                OriginationFeeFixed = 1000m
            }
        };

        var result = await _service.CalculateAsync(request);

        result.Savings.Should().BeLessThan(0);
        result.ShouldYouSwitch.Should().BeFalse();
    }

    [Fact]
    public async Task CalculateAsync_RemainingMonthsShouldBeCorrect()
    {
        var request = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 50000m,
                AnnualRatePercent = 8m,
                TermMonths = 60,
                PaymentsMade = 15,
                PrepaymentFeePercent = 2m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 6m,
                OriginationFeePercent = 1m,
                OriginationFeeFixed = 300m
            }
        };

        var result = await _service.CalculateAsync(request);

        result.RemainingMonths.Should().Be(45);
        result.Current.TermMonths.Should().Be(60);
        result.New.TermMonths.Should().Be(45);
    }

    [Fact]
    public async Task CalculateAsync_CurrentLoanFeesShouldBePrepaymentFee()
    {
        var request = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 100000m,
                AnnualRatePercent = 8m,
                TermMonths = 120,
                PaymentsMade = 24,
                PrepaymentFeePercent = 2m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 6m,
                OriginationFeePercent = 1m,
                OriginationFeeFixed = 500m
            }
        };

        var result = await _service.CalculateAsync(request);

        decimal expectedPrepaymentFee = result.RemainingPrincipal * 0.02m;
        result.Current.Fees.Should().BeApproximately(expectedPrepaymentFee, 0.01m);
    }

    [Fact]
    public async Task CalculateAsync_NewLoanFeesShouldBeZeroInResponse()
    {
        var request = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 100000m,
                AnnualRatePercent = 8m,
                TermMonths = 120,
                PaymentsMade = 24,
                PrepaymentFeePercent = 2m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 6m,
                OriginationFeePercent = 1m,
                OriginationFeeFixed = 500m
            }
        };

        var result = await _service.CalculateAsync(request);

        result.New.Fees.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateAsync_WithZeroPaymentsMade_ShouldCalculateCorrectly()
    {
        var request = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 50000m,
                AnnualRatePercent = 9m,
                TermMonths = 60,
                PaymentsMade = 0,
                PrepaymentFeePercent = 1m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 7m,
                OriginationFeePercent = 0.5m,
                OriginationFeeFixed = 200m
            }
        };

        var result = await _service.CalculateAsync(request);

        result.RemainingMonths.Should().Be(60);
        result.RemainingPrincipal.Should().Be(request.CurrentLoan.Principal);
    }

    [Fact]
    public async Task CalculateAsync_WithZeroInterestRate_ShouldCalculateCorrectly()
    {
        var request = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 12000m,
                AnnualRatePercent = 0m,
                TermMonths = 12,
                PaymentsMade = 6,
                PrepaymentFeePercent = 0m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 0m,
                OriginationFeePercent = 0m,
                OriginationFeeFixed = 0m
            }
        };

        var result = await _service.CalculateAsync(request);

        result.Should().NotBeNull();
        result.RemainingMonths.Should().Be(6);

        decimal expectedMonthlyPayment = request.CurrentLoan.Principal / request.CurrentLoan.TermMonths;
        result.Current.MonthlyPayment.Should().BeApproximately(expectedMonthlyPayment, 0.01m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-12)]
    public async Task CalculateAsync_WithInvalidTermMonths_ShouldThrowException(int termMonths)
    {
        var request = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 100000m,
                AnnualRatePercent = 8m,
                TermMonths = termMonths,
                PaymentsMade = 0,
                PrepaymentFeePercent = 1m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 6m,
                OriginationFeePercent = 1m,
                OriginationFeeFixed = 500m
            }
        };

        await Assert.ThrowsAsync<BankException>(() => _service.CalculateAsync(request));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(121)]
    public async Task CalculateAsync_WithInvalidPaymentsMade_ShouldThrowException(int paymentsMade)
    {
        var request = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 100000m,
                AnnualRatePercent = 8m,
                TermMonths = 120,
                PaymentsMade = paymentsMade,
                PrepaymentFeePercent = 1m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 6m,
                OriginationFeePercent = 1m,
                OriginationFeeFixed = 500m
            }
        };

        await Assert.ThrowsAsync<BankException>(() => _service.CalculateAsync(request));
    }

    [Fact]
    public async Task CalculateAsync_RemainingPrincipalShouldDecrease()
    {
        var baseRequest = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 100000m,
                AnnualRatePercent = 8m,
                TermMonths = 120,
                PaymentsMade = 0,
                PrepaymentFeePercent = 1m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 6m,
                OriginationFeePercent = 1m,
                OriginationFeeFixed = 500m
            }
        };

        var result0 = await _service.CalculateAsync(baseRequest);

        baseRequest.CurrentLoan.PaymentsMade = 24;
        var result24 = await _service.CalculateAsync(baseRequest);

        baseRequest.CurrentLoan.PaymentsMade = 60;
        var result60 = await _service.CalculateAsync(baseRequest);

        result0.RemainingPrincipal.Should().BeGreaterThan(result24.RemainingPrincipal);
        result24.RemainingPrincipal.Should().BeGreaterThan(result60.RemainingPrincipal);
    }

    [Fact]
    public async Task CalculateAsync_WithAllPaymentsMade_ShouldHaveZeroRemaining()
    {
        var request = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 10000m,
                AnnualRatePercent = 8m,
                TermMonths = 12,
                PaymentsMade = 12,
                PrepaymentFeePercent = 1m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 6m,
                OriginationFeePercent = 1m,
                OriginationFeeFixed = 500m
            }
        };

        var result = await _service.CalculateAsync(request);

        result.RemainingMonths.Should().Be(0);
        result.RemainingPrincipal.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateAsync_ResultsShouldBeRoundedTo2Decimals()
    {
        var request = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 100000m,
                AnnualRatePercent = 8.567m,
                TermMonths = 120,
                PaymentsMade = 24,
                PrepaymentFeePercent = 1.234m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 6.789m,
                OriginationFeePercent = 1.111m,
                OriginationFeeFixed = 555.555m
            }
        };

        var result = await _service.CalculateAsync(request);

        result.RemainingPrincipal.Should().Be(Math.Round(result.RemainingPrincipal, 2));
        result.Current.MonthlyPayment.Should().Be(Math.Round(result.Current.MonthlyPayment, 2));
        result.Current.Fees.Should().Be(Math.Round(result.Current.Fees, 2));
        result.Current.TotalToPay.Should().Be(Math.Round(result.Current.TotalToPay, 2));
        result.New.MonthlyPayment.Should().Be(Math.Round(result.New.MonthlyPayment, 2));
        result.New.TotalToPay.Should().Be(Math.Round(result.New.TotalToPay, 2));
        result.Savings.Should().Be(Math.Round(result.Savings, 2));
    }

    [Fact]
    public async Task CalculateAsync_SavingsShouldEqualDifferenceBetweenTotals()
    {
        var request = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 100000m,
                AnnualRatePercent = 10m,
                TermMonths = 120,
                PaymentsMade = 24,
                PrepaymentFeePercent = 1m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 7m,
                OriginationFeePercent = 1m,
                OriginationFeeFixed = 500m
            }
        };

        var result = await _service.CalculateAsync(request);

        decimal expectedSavings = result.Current.TotalToPay - result.New.TotalToPay;
        result.Savings.Should().BeApproximately(expectedSavings, 0.01m);
    }

    [Fact]
    public async Task CalculateAsync_WithNoPrepaymentFee_ShouldHaveZeroCurrentFees()
    {
        var request = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 100000m,
                AnnualRatePercent = 8m,
                TermMonths = 120,
                PaymentsMade = 24,
                PrepaymentFeePercent = 0m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 6m,
                OriginationFeePercent = 1m,
                OriginationFeeFixed = 500m
            }
        };

        var result = await _service.CalculateAsync(request);

        result.Current.Fees.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateAsync_WithNoNewLoanFees_ShouldCalculateCorrectly()
    {
        var request = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 100000m,
                AnnualRatePercent = 10m,
                TermMonths = 120,
                PaymentsMade = 24,
                PrepaymentFeePercent = 1m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 7m,
                OriginationFeePercent = 0m,
                OriginationFeeFixed = 0m
            }
        };

        var result = await _service.CalculateAsync(request);

        result.Should().NotBeNull();
        result.New.MonthlyPayment.Should().BeGreaterThan(0);
        result.New.TotalToPay.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CalculateAsync_WithShortRemainingTerm_ShouldCalculateCorrectly()
    {
        var request = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 10000m,
                AnnualRatePercent = 12m,
                TermMonths = 12,
                PaymentsMade = 9,
                PrepaymentFeePercent = 0.5m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 8m,
                OriginationFeePercent = 0.5m,
                OriginationFeeFixed = 100m
            }
        };

        var result = await _service.CalculateAsync(request);

        result.RemainingMonths.Should().Be(3);
        result.Should().NotBeNull();
        result.Current.MonthlyPayment.Should().BeGreaterThan(0);
        result.New.MonthlyPayment.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CalculateAsync_ShouldYouSwitchLogic_ShouldBeCorrect()
    {
        var goodDealRequest = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 100000m,
                AnnualRatePercent = 12m,
                TermMonths = 120,
                PaymentsMade = 24,
                PrepaymentFeePercent = 0.5m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 6m,
                OriginationFeePercent = 0.5m,
                OriginationFeeFixed = 200m
            }
        };

        var badDealRequest = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 100000m,
                AnnualRatePercent = 6m,
                TermMonths = 120,
                PaymentsMade = 24,
                PrepaymentFeePercent = 3m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 12m,
                OriginationFeePercent = 2m,
                OriginationFeeFixed = 2000m
            }
        };

        var goodResult = await _service.CalculateAsync(goodDealRequest);
        var badResult = await _service.CalculateAsync(badDealRequest);

        goodResult.ShouldYouSwitch.Should().BeTrue();
        goodResult.Savings.Should().BeGreaterThan(0);

        badResult.ShouldYouSwitch.Should().BeFalse();
        badResult.Savings.Should().BeLessThan(0);
    }

    [Fact]
    public async Task CalculateAsync_WithMoitePariExample_ShouldMatchExpectedResults()
    {
        var request = new RefinancingCalculatorRequest
        {
            CurrentLoan = new CurrentLoanInput
            {
                Principal = 300000m,
                AnnualRatePercent = 2.5m,
                TermMonths = 200,
                PaymentsMade = 50,
                PrepaymentFeePercent = 1m
            },
            NewLoan = new NewLoanInput
            {
                AnnualRatePercent = 2.2m,
                OriginationFeePercent = 1m,
                OriginationFeeFixed = 200m
            }
        };

        var result = await _service.CalculateAsync(request);

        result.RemainingMonths.Should().Be(150);
        result.Current.MonthlyPayment.Should().BeApproximately(1835.68m, 0.01m);
        result.New.MonthlyPayment.Should().BeApproximately(1803.07m, 0.01m);
        result.Current.TotalToPay.Should().BeApproximately(275351.76m, 1m);
        result.New.TotalToPay.Should().BeApproximately(276022.98m, 1m);
        result.Savings.Should().BeApproximately(-671.22m, 1m);
    }
}
