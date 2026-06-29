using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Bank.Credits;
using Bank.Core.JsonModels.Calculators;
using Bank.DB;
using Bank.DB.Entities;
using Bank.Services.Calculators;
using Bank.Services.Credits;
using Bank.Tests.Infrastructure;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Bank.Tests.Credits;

public class CreditServiceCreateTests
{
    private static CreditService BuildService(AppDbContext dbContext) =>
        new(dbContext, new FakeUserService(), new CreditCalculatorService(TimeProvider.System), new Bank.Core.Settings.DemoOptions());

    [Fact]
    public async Task CreateCreditAsync_WithValidRequest_GrantsActiveCreditWithFullSchedule()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedConsumerConditionAsync(dbContext);
        var customer = await SeedCustomerAsync(dbContext, isVip: false);
        var service = BuildService(dbContext);

        var result = await service.CreateCreditAsync(new CreateCreditRequest
        {
            CustomerId = customer.Id,
            CreditType = CreditType.Consumer,
            GrantedAmount = 10000m,
            TermMonths = 12,
            InterestRate = 8.5m,
            PaymentType = PaymentType.Annuity,
        });

        using var _ = new AssertionScope();
        result.Status.Should().Be(CreditStatus.Active);
        result.GrantedAmount.Should().Be(10000m);
        result.TermMonths.Should().Be(12);
        result.PlannedMonthlyPaymentAmount.Should().BeGreaterThan(0m);
        result.Payments.Should().HaveCount(12);
        result.Payments.Sum(p => p.PrincipalPart).Should().Be(10000m);
        result.Payments.OrderBy(p => p.PaymentNumber).Last()
            .RemainingPrincipalAfterPayment.Should().Be(0m);
        result.AppliedAnnualInterestRate.Should().Be(8.5m);
        result.CustomerWasVipAtCreation.Should().BeFalse();
        result.CurrentTerms.Should().NotBeNull();
        result.CurrentTerms!.BaseAnnualInterestRate.Should().Be(8.5m);
    }

    [Fact]
    public async Task CreateCreditAsync_ForVipCustomer_PersistsSentRateAndFee()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedConsumerConditionAsync(dbContext);
        var customer = await SeedCustomerAsync(dbContext, isVip: true);
        var service = BuildService(dbContext);

        var result = await service.CreateCreditAsync(new CreateCreditRequest
        {
            CustomerId = customer.Id,
            CreditType = CreditType.Consumer,
            GrantedAmount = 10000m,
            TermMonths = 12,
            InterestRate = 7.5m,
            PaymentType = PaymentType.Annuity,
            ProcessingFee = new Fee { Type = FeeType.Currency, Value = 60m },
        });

        using var _ = new AssertionScope();
        result.AppliedAnnualInterestRate.Should().Be(7.5m);
        result.AppliedGrantingFee.Should().Be(60m);
        result.CustomerWasVipAtCreation.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCreditAsync_BooksScheduleIdenticalToCalculator()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedConsumerConditionAsync(dbContext);
        var customer = await SeedCustomerAsync(dbContext, isVip: false);
        var service = BuildService(dbContext);

        var request = new CreateCreditRequest
        {
            CustomerId = customer.Id,
            CreditType = CreditType.Consumer,
            GrantedAmount = 12000m,
            TermMonths = 24,
            InterestRate = 8.5m,
            PaymentType = PaymentType.Annuity,
            PromoPeriod = 6,
            PromoRate = 4.9m,
            MonthlyManagementFee = new Fee { Type = FeeType.Currency, Value = 5m },
        };

        var created = await service.CreateCreditAsync(request);

        var calculator = new CreditCalculatorService(TimeProvider.System);
        var expected = await calculator.CalculateAsync(new CreditCalculatorRequest
        {
            LoanAmount = 12000m,
            TermInMonths = 24,
            InterestRate = 8.5m,
            PaymentType = PaymentType.Annuity,
            PromoPeriod = 6,
            PromoRate = 4.9m,
            MonthlyManagementFee = new Fee { Type = FeeType.Currency, Value = 5m },
        });
        var expectedMonths = expected.PaymentSchedule.Where(item => item.Month >= 1).OrderBy(item => item.Month).ToArray();

        using var _ = new AssertionScope();
        created.Payments.Should().HaveCount(24);
        var payments = created.Payments.OrderBy(payment => payment.PaymentNumber).ToArray();
        for (var index = 0; index < payments.Length; index++)
        {
            payments[index].PaymentAmount.Should().Be(expectedMonths[index].Payment);
            payments[index].PrincipalPart.Should().Be(expectedMonths[index].Principal);
            payments[index].InterestPart.Should().Be(expectedMonths[index].Interest);
            payments[index].FeePart.Should().Be(expectedMonths[index].Fees);
        }
    }

    [Fact]
    public async Task GetCreditAsync_ReturnsTotalsAggregatedFromScheduleMatchingCalculator()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedConsumerConditionAsync(dbContext);
        var customer = await SeedCustomerAsync(dbContext, isVip: false);
        var service = BuildService(dbContext);

        var request = new CreateCreditRequest
        {
            CustomerId = customer.Id,
            CreditType = CreditType.Consumer,
            GrantedAmount = 12000m,
            TermMonths = 24,
            InterestRate = 8.5m,
            PaymentType = PaymentType.Annuity,
            ProcessingFee = new Fee { Type = FeeType.Currency, Value = 120m },
            MonthlyManagementFee = new Fee { Type = FeeType.Currency, Value = 5m },
        };

        var created = await service.CreateCreditAsync(request);
        var credit = await service.GetCreditAsync(created.Id);

        var calculator = new CreditCalculatorService(TimeProvider.System);
        var expected = await calculator.CalculateAsync(new CreditCalculatorRequest
        {
            LoanAmount = 12000m,
            TermInMonths = 24,
            InterestRate = 8.5m,
            PaymentType = PaymentType.Annuity,
            ProcessingFee = new Fee { Type = FeeType.Currency, Value = 120m },
            MonthlyManagementFee = new Fee { Type = FeeType.Currency, Value = 5m },
        });

        using var _ = new AssertionScope();
        credit.TotalInterest.Should().Be(expected.TotalInterest);
        credit.TotalFees.Should().Be(expected.TotalFees);
        credit.TotalAmountWithFees.Should().Be(expected.TotalAmountWithFees);
    }

    [Fact]
    public async Task CreateCreditAsync_WhenAmountExceedsMaximum_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedConsumerConditionAsync(dbContext);
        var customer = await SeedCustomerAsync(dbContext, isVip: false);
        var service = BuildService(dbContext);

        var act = () => service.CreateCreditAsync(new CreateCreditRequest
        {
            CustomerId = customer.Id,
            CreditType = CreditType.Consumer,
            GrantedAmount = 50001m,
            TermMonths = 12,
            InterestRate = 8.5m,
            PaymentType = PaymentType.Annuity,
        });

        (await act.Should().ThrowAsync<BankException>())
            .WithMessage("*надвишава максимално допустимата сума*");
    }

    [Fact]
    public async Task CreateCreditAsync_WhenTermExceedsMaximum_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedConsumerConditionAsync(dbContext);
        var customer = await SeedCustomerAsync(dbContext, isVip: false);
        var service = BuildService(dbContext);

        var act = () => service.CreateCreditAsync(new CreateCreditRequest
        {
            CustomerId = customer.Id,
            CreditType = CreditType.Consumer,
            GrantedAmount = 10000m,
            TermMonths = 85,
            InterestRate = 8.5m,
            PaymentType = PaymentType.Annuity,
        });

        (await act.Should().ThrowAsync<BankException>())
            .WithMessage("*надвишава максимално допустимия срок*");
    }

    [Fact]
    public async Task CreateCreditAsync_ForUnknownCustomer_ThrowsNotFound()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedConsumerConditionAsync(dbContext);
        var service = BuildService(dbContext);

        var act = () => service.CreateCreditAsync(new CreateCreditRequest
        {
            CustomerId = 999,
            CreditType = CreditType.Consumer,
            GrantedAmount = 10000m,
            TermMonths = 12,
            InterestRate = 8.5m,
            PaymentType = PaymentType.Annuity,
        });

        (await act.Should().ThrowAsync<BankException>())
            .Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateCreditAsync_WhenNoActiveConditionForType_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedConsumerConditionAsync(dbContext);
        var customer = await SeedCustomerAsync(dbContext, isVip: false);
        var service = BuildService(dbContext);

        var act = () => service.CreateCreditAsync(new CreateCreditRequest
        {
            CustomerId = customer.Id,
            CreditType = CreditType.Mortgage,
            GrantedAmount = 10000m,
            TermMonths = 12,
            InterestRate = 4.5m,
            PaymentType = PaymentType.Annuity,
        });

        (await act.Should().ThrowAsync<BankException>())
            .WithMessage("*не е намерено или е неактивно*");
    }

    private static async Task SeedConsumerConditionAsync(AppDbContext dbContext)
    {
        dbContext.CreditTypeConditions.Add(new CreditTypeCondition
        {
            CreditType = CreditType.Consumer,
            Name = "Consumer",
            StandardAnnualInterestRate = 8.5m,
            VipAnnualInterestRate = 7.5m,
            MaximumAmount = 50000m,
            MaximumTermMonths = 84,
            StandardGrantingFee = 120m,
            VipGrantingFee = 60m,
            IsActive = true,
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task<Customer> SeedCustomerAsync(AppDbContext dbContext, bool isVip)
    {
        var customer = new Customer
        {
            CustomerType = CustomerType.Individual,
            Person = new Person { FirstName = "Petar", LastName = "Dimitrov", Egn = Guid.NewGuid().ToString("N")[..10] },
            IsVip = isVip,
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        return customer;
    }
}
