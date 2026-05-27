using Bank.Core.Enums;
using Bank.DB.Entities;
using Bank.Services.Credits;
using Bank.Services.Users;
using Microsoft.EntityFrameworkCore;

namespace Bank.Tests;

public class CreditRepricingServiceTests
{
    [Fact]
    public async Task RepriceActiveCreditsForCustomerAsync_RecalculatesPendingPaymentsFromNextUnpaid()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var dbContext = TestDbContextFactory.CreateContext(databaseName);

        IUserService userService = new FakeUserService();
        var calculator = new RepaymentPlanCalculator();
        var vipPricingPolicy = new VipPricingPolicy();
        var repricingService = new CreditRepricingService(dbContext, userService, calculator, vipPricingPolicy);

        var customer = new Customer
        {
            CustomerType = CustomerType.Individual,
            FirstName = "Ivan",
            LastName = "Petrov",
            PersonalIdentifier = "1234567890",
            IsVip = false,
        };

        var condition = new CreditTypeCondition
        {
            CreditType = CreditType.Consumer,
            Name = "Consumer Test",
            StandardAnnualInterestRate = 12m,
            VipAnnualInterestRate = 6m,
            MaximumAmount = 100000m,
            MaximumTermMonths = 120,
            StandardGrantingFee = 100m,
            VipGrantingFee = 50m,
            IsActive = true,
        };

        var grantedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var initialPlan = calculator.Calculate(12000m, condition.StandardAnnualInterestRate, 12, grantedAt);

        var credit = new Credit
        {
            Customer = customer,
            CreditTypeCondition = condition,
            GrantedAmount = 12000m,
            TermMonths = 12,
            AppliedAnnualInterestRate = condition.StandardAnnualInterestRate,
            AppliedGrantingFee = condition.StandardGrantingFee,
            CustomerWasVipAtCreation = false,
            PlannedMonthlyPaymentAmount = initialPlan.PlannedMonthlyPaymentAmount,
            Status = CreditStatus.Active,
            GrantedAtUtc = grantedAt,
            Payments = initialPlan.Payments.Select(payment => new CreditPayment
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

        var firstPayment = credit.Payments.OrderBy(payment => payment.PaymentNumber).First();
        firstPayment.Status = CreditPaymentStatus.Paid;
        firstPayment.PaidAtUtc = grantedAt.AddMonths(1);
        var secondPaymentBefore = credit.Payments.Single(payment => payment.PaymentNumber == 2).InterestPart;
        var firstPaymentPrincipalBefore = firstPayment.PrincipalPart;

        customer.IsVip = true;
        await dbContext.SaveChangesAsync();

        await repricingService.RepriceActiveCreditsForCustomerAsync(customer.Id);

        var reloadedCredit = await dbContext.Credits
            .Include(entity => entity.Payments)
            .Include(entity => entity.PricingChanges)
            .FirstAsync(entity => entity.Id == credit.Id);

        var firstPaymentAfter = reloadedCredit.Payments.Single(payment => payment.PaymentNumber == 1);
        var secondPaymentAfter = reloadedCredit.Payments.Single(payment => payment.PaymentNumber == 2);

        Assert.Equal(6m, reloadedCredit.AppliedAnnualInterestRate);
        Assert.Equal(firstPaymentPrincipalBefore, firstPaymentAfter.PrincipalPart);
        Assert.True(secondPaymentAfter.InterestPart < secondPaymentBefore);
        Assert.Single(reloadedCredit.PricingChanges);
        Assert.Equal(2, reloadedCredit.PricingChanges.Single().EffectiveFromPaymentNumber);
    }
}
