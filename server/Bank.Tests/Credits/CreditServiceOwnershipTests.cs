using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.DB.Entities;
using Bank.Services.Calculators;
using Bank.Services.Credits;
using Bank.Services.Users;
using Bank.Tests.Infrastructure;
using FluentAssertions;

namespace Bank.Tests.Credits;

public class CreditServiceOwnershipTests
{
    private static readonly DateTime GrantedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetCreditForCustomerAsync_WhenCallerOwnsTheCredit_ReturnsIt()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var creditService = BuildCreditService(dbContext, out var calculator);
        var condition = await SeedConditionAsync(dbContext);
        var owner = await SeedCustomerAsync(dbContext, CustomerType.Company);
        var credit = await SeedCreditAsync(dbContext, calculator, condition, owner, 5000m, 10m, 6);

        var result = await creditService.GetCreditForCustomerAsync(owner.Id, credit.Id);

        result.Id.Should().Be(credit.Id);
        result.CustomerId.Should().Be(owner.Id);
        result.Payments.Should().HaveCount(6);
    }

    [Fact]
    public async Task GetCreditForCustomerAsync_WhenCreditBelongsToAnotherCustomer_ThrowsNotFound()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var creditService = BuildCreditService(dbContext, out var calculator);
        var condition = await SeedConditionAsync(dbContext);
        var owner = await SeedCustomerAsync(dbContext, CustomerType.Company);
        var otherCustomer = await SeedCustomerAsync(dbContext, CustomerType.Individual);
        var credit = await SeedCreditAsync(dbContext, calculator, condition, owner, 5000m, 10m, 6);

        var act = () => creditService.GetCreditForCustomerAsync(otherCustomer.Id, credit.Id);

        (await act.Should().ThrowAsync<BankException>())
            .Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetCreditForCustomerAsync_ForUnknownCredit_ThrowsNotFound()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var creditService = BuildCreditService(dbContext, out _);
        var owner = await SeedCustomerAsync(dbContext, CustomerType.Individual);

        var act = () => creditService.GetCreditForCustomerAsync(owner.Id, 999);

        (await act.Should().ThrowAsync<BankException>())
            .Which.StatusCode.Should().Be(404);
    }

    private static CreditService BuildCreditService(Bank.DB.AppDbContext dbContext, out RepaymentPlanCalculator calculator)
    {
        IUserService userService = new FakeUserService();
        calculator = new RepaymentPlanCalculator();
        return new CreditService(dbContext, userService, new CreditCalculatorService(TimeProvider.System), new Bank.Core.Settings.DemoOptions());
    }

    private static async Task<Customer> SeedCustomerAsync(Bank.DB.AppDbContext dbContext, CustomerType customerType)
    {
        var customer = customerType == CustomerType.Company
            ? new Customer
            {
                CustomerType = CustomerType.Company,
                Company = new Company { Name = "Acme OOD", Eik = Guid.NewGuid().ToString("N")[..9] },
                IsVip = false,
            }
            : new Customer
            {
                CustomerType = CustomerType.Individual,
                Person = new Person { FirstName = "Maria", LastName = "Georgieva", Egn = Guid.NewGuid().ToString("N")[..10] },
                IsVip = false,
            };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        return customer;
    }

    private static async Task<CreditTypeCondition> SeedConditionAsync(Bank.DB.AppDbContext dbContext)
    {
        var condition = new CreditTypeCondition
        {
            CreditType = CreditType.Consumer,
            Name = "Condition " + Guid.NewGuid().ToString("N")[..6],
            StandardAnnualInterestRate = 10m,
            VipAnnualInterestRate = 9m,
            MaximumAmount = 100000m,
            MaximumTermMonths = 120,
            StandardGrantingFee = 100m,
            VipGrantingFee = 50m,
            IsActive = true,
        };

        dbContext.CreditTypeConditions.Add(condition);
        await dbContext.SaveChangesAsync();
        return condition;
    }

    private static async Task<Credit> SeedCreditAsync(
        Bank.DB.AppDbContext dbContext,
        RepaymentPlanCalculator calculator,
        CreditTypeCondition condition,
        Customer customer,
        decimal principal,
        decimal annualRate,
        int termMonths)
    {
        var plan = calculator.Calculate(principal, annualRate, termMonths, GrantedAt);

        var credit = new Credit
        {
            CustomerId = customer.Id,
            CreditTypeConditionId = condition.Id,
            GrantedAmount = principal,
            TermMonths = termMonths,
            AppliedAnnualInterestRate = annualRate,
            AppliedGrantingFee = condition.StandardGrantingFee,
            CustomerWasVipAtCreation = false,
            PlannedMonthlyPaymentAmount = plan.PlannedMonthlyPaymentAmount,
            Status = CreditStatus.Active,
            GrantedAtUtc = GrantedAt,
            Installments = plan.Payments.Select(p => new CreditInstallment
            {
                InstallmentNumber = p.PaymentNumber,
                DueDate = p.DueDate,
                InstallmentAmount = p.PaymentAmount,
                PrincipalPart = p.PrincipalPart,
                InterestPart = p.InterestPart,
                RemainingPrincipalAfterPayment = p.RemainingPrincipalAfterPayment,
                Status = CreditPaymentStatus.Pending,
            }).ToArray(),
        };

        dbContext.Credits.Add(credit);
        await dbContext.SaveChangesAsync();
        return credit;
    }
}
