using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.DB.Entities;
using Bank.Services.Credits;
using Bank.Services.Users;
using Microsoft.EntityFrameworkCore;

namespace Bank.Tests;

public class CreditServicePaymentTests
{
    [Fact]
    public async Task PayPaymentAsync_RejectsSkippingPayments()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var dbContext = TestDbContextFactory.CreateContext(databaseName);

        IUserService userService = new FakeUserService();
        var calculator = new RepaymentPlanCalculator();
        var creditService = new CreditService(dbContext, userService, calculator, new VipPricingPolicy());
        var credit = await SeedCreditAsync(dbContext, calculator, 5000m, 10m, 6);

        var secondPaymentId = credit.Payments.OrderBy(payment => payment.PaymentNumber).Skip(1).First().Id;
        var exception = await Assert.ThrowsAsync<BankException>(() => creditService.PayPaymentAsync(credit.Id, secondPaymentId));

        Assert.Equal("Only the next pending payment can be paid.", exception.Message);
    }

    [Fact]
    public async Task PayPaymentAsync_MarksCreditAsRepaidAfterFinalPayment()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var dbContext = TestDbContextFactory.CreateContext(databaseName);

        IUserService userService = new FakeUserService();
        var calculator = new RepaymentPlanCalculator();
        var creditService = new CreditService(dbContext, userService, calculator, new VipPricingPolicy());
        var credit = await SeedCreditAsync(dbContext, calculator, 3000m, 9m, 3);

        foreach (var payment in credit.Payments.OrderBy(payment => payment.PaymentNumber))
        {
            await creditService.PayPaymentAsync(credit.Id, payment.Id);
        }

        var reloadedCredit = await dbContext.Credits
            .Include(entity => entity.Payments)
            .FirstAsync(entity => entity.Id == credit.Id);

        Assert.Equal(CreditStatus.Repaid, reloadedCredit.Status);
        Assert.NotNull(reloadedCredit.RepaidAtUtc);
        Assert.All(reloadedCredit.Payments, payment => Assert.Equal(CreditPaymentStatus.Paid, payment.Status));
    }

    private static async Task<Credit> SeedCreditAsync(
        Bank.DB.AppDbContext dbContext,
        RepaymentPlanCalculator calculator,
        decimal principal,
        decimal annualRate,
        int termMonths)
    {
        var customer = new Customer
        {
            CustomerType = CustomerType.Individual,
            FirstName = "Maria",
            LastName = "Georgieva",
            PersonalIdentifier = Guid.NewGuid().ToString("N")[..10],
            IsVip = false,
        };

        var condition = new CreditTypeCondition
        {
            CreditType = CreditType.Consumer,
            Name = "Condition " + Guid.NewGuid().ToString("N")[..6],
            StandardAnnualInterestRate = annualRate,
            VipAnnualInterestRate = annualRate - 1m,
            MaximumAmount = 100000m,
            MaximumTermMonths = 120,
            StandardGrantingFee = 100m,
            VipGrantingFee = 50m,
            IsActive = true,
        };

        var grantedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var plan = calculator.Calculate(principal, annualRate, termMonths, grantedAt);

        var credit = new Credit
        {
            Customer = customer,
            CreditTypeCondition = condition,
            GrantedAmount = principal,
            TermMonths = termMonths,
            AppliedAnnualInterestRate = annualRate,
            AppliedGrantingFee = condition.StandardGrantingFee,
            CustomerWasVipAtCreation = false,
            PlannedMonthlyPaymentAmount = plan.PlannedMonthlyPaymentAmount,
            Status = CreditStatus.Active,
            GrantedAtUtc = grantedAt,
            Payments = plan.Payments.Select(payment => new CreditPayment
            {
                PaymentNumber = payment.PaymentNumber,
                DueDate = payment.DueDate,
                PaymentAmount = payment.PaymentAmount,
                PrincipalPart = payment.PrincipalPart,
                InterestPart = payment.InterestPart,
                RemainingPrincipalAfterPayment = payment.RemainingPrincipalAfterPayment,
                Status = CreditPaymentStatus.Pending,
            }).ToArray(),
        };

        dbContext.Credits.Add(credit);
        await dbContext.SaveChangesAsync();
        return credit;
    }
}
