using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Calculators;
using Bank.Services.Calculators;
using FluentAssertions;

namespace Bank.Tests.Calculators;

public class CreditCalculatorServiceTests
{
    private readonly CreditCalculatorService _service;

    public CreditCalculatorServiceTests()
    {
        _service = new CreditCalculatorService(TimeProvider.System);
    }

    private static CreditCalculatorRequest BuildFormRequest() =>
        new()
        {
            LoanAmount = 200000m,
            TermInMonths = 300,
            InterestRate = 2.5m,
            PaymentType = PaymentType.Annuity,

            PromoPeriod = 3,
            PromoRate = 2m,
            GracePeriod = 3,

            ApplicationFee = new Fee { Type = FeeType.Currency, Value = 300m },
            OtherAnnualFees = new Fee { Type = FeeType.Currency, Value = 300m },
            OtherMonthlyFees = new Fee { Type = FeeType.Currency, Value = 20m },
        };

    [Fact]
    public async Task CalculateAsync_WithBuildFormRequest_ReturnsValidSchedule()
    {
        var request = BuildFormRequest();

        var result = await _service.CalculateAsync(request);

        result.Should().NotBeNull();
        result.PaymentSchedule.Should().HaveCount(301);
        result.PaymentSchedule[0].Month.Should().Be(0);
        result.PaymentSchedule[0].RemainingBalance.Should().Be(request.LoanAmount);
        result.PaymentSchedule[0].Fees.Should().Be(300m);
        result.TotalPayments.Should().BeGreaterThan(request.LoanAmount);
        result.TotalInterest.Should().BeGreaterThan(0);
        result.AverageMonthlyPayment.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CalculateAsync_WithBuildFormRequest_GracePeriodPaysOnlyInterest()
    {
        var request = BuildFormRequest();
        var result = await _service.CalculateAsync(request);

        var graceMonths = result.PaymentSchedule
            .Where(s => s.Month >= 1 && s.Month <= request.GracePeriod!.Value);

        graceMonths.Should().OnlyContain(s => s.Principal == 0m && s.Payment == s.Interest);
    }

    [Fact]
    public async Task CalculateAsync_WithBuildFormRequest_TotalPrincipalEqualsLoanAmount()
    {
        var request = BuildFormRequest();
        var result = await _service.CalculateAsync(request);

        var totalPrincipal = result.PaymentSchedule
            .Where(s => s.Month >= 1)
            .Sum(s => s.Principal);

        totalPrincipal.Should().BeApproximately(request.LoanAmount, 0.01m);
    }

    [Fact]
    public async Task CalculateAsync_WithBuildFormRequest_FeesMatchPerMonthRules()
    {
        var request = BuildFormRequest();
        var result = await _service.CalculateAsync(request);

        result.PaymentSchedule[0].Fees.Should().Be(300m);

        foreach (var item in result.PaymentSchedule.Where(s => s.Month >= 1))
        {
            var hasAnnualFee = item.Month > 1 && (item.Month - 1) % 12 == 0;
            item.Fees.Should().Be(hasAnnualFee ? 320m : 20m);
        }
    }

    [Fact]
    public async Task CalculateAsync_WithBuildFormRequest_TotalFeesEqualsApplicationPlusMonthlyPlusAnnual()
    {
        var request = BuildFormRequest();
        var result = await _service.CalculateAsync(request);

        var annualFeeCount = Enumerable.Range(1, request.TermInMonths)
            .Count(m => m > 1 && (m - 1) % 12 == 0);
        var expected = 300m + (request.TermInMonths * 20m) + (300m * annualFeeCount);

        result.TotalFees.Should().BeApproximately(expected, 1m);
    }

    [Fact]
    public async Task CalculateAsync_WithBuildFormRequest_RemainingBalanceDecreasesToZero()
    {
        var request = BuildFormRequest();
        var result = await _service.CalculateAsync(request);

        var schedule = result.PaymentSchedule.Where(s => s.Month >= 1).ToList();
        for (var i = 0; i < schedule.Count - 1; i++)
            schedule[i].RemainingBalance.Should().BeGreaterThanOrEqualTo(schedule[i + 1].RemainingBalance);

        schedule[^1].RemainingBalance.Should().BeApproximately(schedule[^1].Principal, 0.01m);
    }

    [Fact]
    public async Task CalculateAsync_WithBuildFormRequest_APRIsPositiveAndBounded()
    {
        var request = BuildFormRequest();
        var result = await _service.CalculateAsync(request);

        result.APR.Should().BeInRange(0.01m, 50m);
    }

    [Fact]
    public async Task CalculateAsync_WithBuildFormRequest_CashFlowSignsCorrect()
    {
        var request = BuildFormRequest();
        var result = await _service.CalculateAsync(request);

        result.PaymentSchedule[0].CashFlow.Should().Be(request.LoanAmount - 300m);
        result.PaymentSchedule.Skip(1).Should().AllSatisfy(s => s.CashFlow.Should().BeLessThan(0));
    }

    [Fact]
    public async Task CalculateAsync_WithBuildFormRequest_TotalAmountWithFeesIsPaymentsPlusFees()
    {
        var request = BuildFormRequest();
        var result = await _service.CalculateAsync(request);

        result.TotalAmountWithFees.Should().Be(result.TotalPayments + result.TotalFees);
    }

    [Fact]
    public async Task CalculateAsync_WithValidBasicRequest_ShouldReturnCorrectResponse()
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 12,
            InterestRate = 10m,
            PaymentType = PaymentType.Annuity
        };

        var result = await _service.CalculateAsync(request);

        result.Should().NotBeNull();
        result.PaymentSchedule.Should().HaveCount(13);
        result.TotalInterest.Should().BeGreaterThan(0);
        result.TotalPayments.Should().BeGreaterThan(request.LoanAmount);
        result.AverageMonthlyPayment.Should().BeGreaterThan(0);
        result.PaymentSchedule[0].Month.Should().Be(0);
        result.PaymentSchedule[0].RemainingBalance.Should().Be(request.LoanAmount);
    }

    [Fact]
    public async Task CalculateAsync_WithAnnuityPayment_AllPaymentsShouldBeEqual()
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 12,
            InterestRate = 10m,
            PaymentType = PaymentType.Annuity
        };

        var result = await _service.CalculateAsync(request);

        var payments = result.PaymentSchedule
            .Where(s => s.Month >= 1)
            .Select(s => s.Payment)
            .ToList();

        var firstPayment = payments.First();
        payments.Take(payments.Count - 1).Should().AllSatisfy(p =>
            Math.Abs(p - firstPayment).Should().BeLessThan(0.01m));
    }

    [Fact]
    public async Task CalculateAsync_WithDecliningPayment_PaymentsShouldDecrease()
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 12,
            InterestRate = 10m,
            PaymentType = PaymentType.Declining
        };

        var result = await _service.CalculateAsync(request);

        var payments = result.PaymentSchedule
            .Where(s => s.Month >= 1)
            .Select(s => s.Payment)
            .ToList();

        for (int i = 0; i < payments.Count - 1; i++)
        {
            payments[i].Should().BeGreaterThanOrEqualTo(payments[i + 1]);
        }
    }

    [Fact]
    public async Task CalculateAsync_WithPromoPeriod_ShouldApplyPromoRate()
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 12,
            InterestRate = 10m,
            PaymentType = PaymentType.Annuity,
            PromoPeriod = 3,
            PromoRate = 5m
        };

        var result = await _service.CalculateAsync(request);

        result.PaymentSchedule.Should().HaveCount(13);

        var totalInterest = result.PaymentSchedule
            .Where(s => s.Month >= 1)
            .Sum(s => s.Interest);

        var requestNoPromo = new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 12,
            InterestRate = 10m,
            PaymentType = PaymentType.Annuity
        };
        var resultNoPromo = await _service.CalculateAsync(requestNoPromo);
        var totalInterestNoPromo = resultNoPromo.PaymentSchedule
            .Where(s => s.Month >= 1)
            .Sum(s => s.Interest);

        totalInterest.Should().BeLessThan(totalInterestNoPromo);
    }

    [Fact]
    public async Task CalculateAsync_WithGracePeriod_ShouldPayOnlyInterest()
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 12,
            InterestRate = 10m,
            PaymentType = PaymentType.Annuity,
            GracePeriod = 2
        };

        var result = await _service.CalculateAsync(request);

        var gracePeriodSchedule = result.PaymentSchedule
            .Where(s => s.Month >= 1 && s.Month <= 2)
            .ToList();

        foreach (var item in gracePeriodSchedule)
        {
            item.Principal.Should().Be(0m);
            item.Payment.Should().Be(item.Interest);
        }
    }

    [Fact]
    public async Task CalculateAsync_WithApplicationFeePercent_ShouldCalculateFees()
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 12,
            InterestRate = 10m,
            PaymentType = PaymentType.Annuity,
            ApplicationFee = new Fee { Type = FeeType.Percent, Value = 2m }
        };

        var result = await _service.CalculateAsync(request);

        result.TotalFees.Should().Be(200m);
        result.PaymentSchedule[0].Fees.Should().Be(200m);
    }

    [Fact]
    public async Task CalculateAsync_WithApplicationFeeCurrency_ShouldCalculateFees()
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 12,
            InterestRate = 10m,
            PaymentType = PaymentType.Annuity,
            ApplicationFee = new Fee { Type = FeeType.Currency, Value = 150m }
        };

        var result = await _service.CalculateAsync(request);

        result.TotalFees.Should().Be(150m);
        result.PaymentSchedule[0].Fees.Should().Be(150m);
    }

    [Fact]
    public async Task CalculateAsync_WithMultipleInitialFees_ShouldSumAllFees()
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 12,
            InterestRate = 10m,
            PaymentType = PaymentType.Annuity,
            ApplicationFee = new Fee { Type = FeeType.Currency, Value = 100m },
            ProcessingFee = new Fee { Type = FeeType.Percent, Value = 1m },
            OtherInitialFees = new Fee { Type = FeeType.Currency, Value = 50m }
        };

        var result = await _service.CalculateAsync(request);

        decimal expectedInitialFees = 100m + (10000m * 0.01m) + 50m;
        result.PaymentSchedule[0].Fees.Should().Be(expectedInitialFees);
    }

    [Fact]
    public async Task CalculateAsync_WithMonthlyManagementFee_ShouldApplyEveryMonth()
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 12,
            InterestRate = 10m,
            PaymentType = PaymentType.Annuity,
            MonthlyManagementFee = new Fee { Type = FeeType.Currency, Value = 10m }
        };

        var result = await _service.CalculateAsync(request);

        var monthlyFeesSum = result.PaymentSchedule
            .Where(s => s.Month >= 1)
            .Sum(s => s.Fees);

        monthlyFeesSum.Should().BeApproximately(120m, 1m);
    }

    [Fact]
    public async Task CalculateAsync_WithAnnualFee_ShouldApplyOncePerYear()
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 24,
            InterestRate = 10m,
            PaymentType = PaymentType.Annuity,
            AnnualManagementFee = new Fee { Type = FeeType.Currency, Value = 100m }
        };

        var result = await _service.CalculateAsync(request);

        result.PaymentSchedule[13].Fees.Should().Be(100m);

        result.PaymentSchedule[1].Fees.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateAsync_WithZeroInterestRate_ShouldCalculateCorrectly()
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 12000m,
            TermInMonths = 12,
            InterestRate = 0m,
            PaymentType = PaymentType.Annuity
        };

        var result = await _service.CalculateAsync(request);

        result.TotalInterest.Should().Be(0m);
        result.TotalPayments.Should().Be(request.LoanAmount);

        var expectedPayment = request.LoanAmount / request.TermInMonths;
        result.AverageMonthlyPayment.Should().BeApproximately(expectedPayment, 0.01m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task CalculateAsync_WithInvalidLoanAmount_ShouldThrowException(decimal loanAmount)
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = loanAmount,
            TermInMonths = 12,
            InterestRate = 10m,
            PaymentType = PaymentType.Annuity
        };

        await Assert.ThrowsAsync<BankException>(() => _service.CalculateAsync(request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-12)]
    public async Task CalculateAsync_WithInvalidTerm_ShouldThrowException(int termInMonths)
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = termInMonths,
            InterestRate = 10m,
            PaymentType = PaymentType.Annuity
        };

        await Assert.ThrowsAsync<BankException>(() => _service.CalculateAsync(request));
    }

    [Fact]
    public async Task CalculateAsync_WithExcessiveTerm_ShouldThrowException()
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 601,
            InterestRate = 10m,
            PaymentType = PaymentType.Annuity
        };

        await Assert.ThrowsAsync<BankException>(() => _service.CalculateAsync(request));
    }

    [Fact]
    public async Task CalculateAsync_WithInvalidInterestRate_ShouldThrowException()
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 12,
            InterestRate = -5m,
            PaymentType = PaymentType.Annuity
        };

        await Assert.ThrowsAsync<BankException>(() => _service.CalculateAsync(request));
    }

    [Fact]
    public async Task CalculateAsync_ShouldHaveCorrectCashFlows()
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 12,
            InterestRate = 10m,
            PaymentType = PaymentType.Annuity
        };

        var result = await _service.CalculateAsync(request);

        result.PaymentSchedule[0].CashFlow.Should().BeGreaterThan(0);

        result.PaymentSchedule.Skip(1).Should().AllSatisfy(s =>
            s.CashFlow.Should().BeLessThan(0));
    }

    [Fact]
    public async Task CalculateAsync_RemainingBalanceShouldDecreaseToZero()
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 12,
            InterestRate = 10m,
            PaymentType = PaymentType.Annuity
        };

        var result = await _service.CalculateAsync(request);

        var schedule = result.PaymentSchedule.Where(s => s.Month >= 1).ToList();

        for (int i = 0; i < schedule.Count - 1; i++)
        {
            schedule[i].RemainingBalance.Should().BeGreaterThan(schedule[i + 1].RemainingBalance);
        }

        schedule.Last().RemainingBalance.Should().BeApproximately(schedule.Last().Principal, 0.001m);
    }

    [Fact]
    public async Task CalculateAsync_TotalPrincipalShouldEqualLoanAmount()
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 12,
            InterestRate = 10m,
            PaymentType = PaymentType.Annuity
        };

        var result = await _service.CalculateAsync(request);

        var totalPrincipal = result.PaymentSchedule
            .Where(s => s.Month >= 1)
            .Sum(s => s.Principal);

        totalPrincipal.Should().BeApproximately(request.LoanAmount, 0.01m);
    }

    [Fact]
    public async Task CalculateAsync_APRShoulMatchTheOriginalSite()
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 12,
            InterestRate = 10m,
            PaymentType = PaymentType.Annuity,
        };

        var result = await _service.CalculateAsync(request);

        result.APR.Should().BeApproximately(10.4710m, 0.0001m);
    }

    [Fact]
    public async Task CalculateAsync_WithComplexScenario_ShouldCalculateCorrectly()
    {
        var request = new CreditCalculatorRequest
        {
            LoanAmount = 50000m,
            TermInMonths = 60,
            InterestRate = 8m,
            PaymentType = PaymentType.Annuity,
            PromoPeriod = 6,
            PromoRate = 5m,
            GracePeriod = 3,
            ApplicationFee = new Fee { Type = FeeType.Percent, Value = 2m },
            ProcessingFee = new Fee { Type = FeeType.Currency, Value = 200m },
            MonthlyManagementFee = new Fee { Type = FeeType.Currency, Value = 5m },
            AnnualManagementFee = new Fee { Type = FeeType.Currency, Value = 50m }
        };

        var result = await _service.CalculateAsync(request);

        result.Should().NotBeNull();
        result.PaymentSchedule.Should().HaveCount(61);
        result.TotalPayments.Should().BeGreaterThan(request.LoanAmount);
        result.TotalFees.Should().BeGreaterThan(0);
        result.TotalAmountWithFees.Should().Be(result.TotalPayments + result.TotalFees);
        result.APR.Should().BeGreaterThan(0);

        for (int month = 1; month <= 3; month++)
        {
            result.PaymentSchedule[month].Principal.Should().Be(0m);
        }

        for (int month = 4; month <= 6; month++)
        {
            result.PaymentSchedule[month].Principal.Should().BeGreaterThan(0m);
        }
    }
}
