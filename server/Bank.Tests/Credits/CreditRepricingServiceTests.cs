using Bank.Core.Enums;
using Bank.Core.JsonModels.Bank.Credits;
using Bank.DB;
using Bank.DB.Entities;
using Bank.Services.Calculators;
using Bank.Services.Credits;
using Bank.Tests.Infrastructure;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;

namespace Bank.Tests.Credits;

public class CreditRepricingServiceTests
{
    private static CreditService BuildCreditService(AppDbContext dbContext) =>
        new(dbContext, new FakeUserService(), new CreditCalculatorService(TimeProvider.System), new Bank.Core.Settings.DemoOptions());

    private static CreditRepricingService BuildRepricingService(AppDbContext dbContext) =>
        new(dbContext, new FakeUserService(), new CreditCalculatorService(TimeProvider.System));

    [Fact]
    public async Task RepriceActiveCreditsForCustomerAsync_WhenCustomerBecomesVip_AddsNewCurrentTermsAndRewritesPending()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedConsumerConditionAsync(dbContext); // Стандартно 12%, VIP 6%
        var customer = await SeedCustomerAsync(dbContext, isVip: false);

        var credit = await BuildCreditService(dbContext).CreateCreditAsync(new CreateCreditRequest
        {
            CustomerId = customer.Id,
            CreditType = CreditType.Consumer,
            GrantedAmount = 12000m,
            TermMonths = 12,
            InterestRate = 12m,
            PaymentType = PaymentType.Annuity,
        });

        var secondInterestBefore = credit.Payments.Single(payment => payment.PaymentNumber == 2).InterestPart;

        var customerEntity = await dbContext.Customers.FirstAsync(person => person.Id == customer.Id);
        customerEntity.IsVip = true;
        await dbContext.SaveChangesAsync();

        await BuildRepricingService(dbContext).RepriceActiveCreditsForCustomerAsync(customer.Id);

        var terms = await dbContext.CreditTerms
            .Where(existingTerms => existingTerms.CreditId == credit.Id)
            .OrderBy(existingTerms => existingTerms.Id)
            .ToListAsync();
        var secondInterestAfter = await dbContext.CreditInstallments
            .Where(payment => payment.CreditId == credit.Id && payment.InstallmentNumber == 2)
            .Select(payment => payment.InterestPart)
            .FirstAsync();

        using var _ = new AssertionScope();
        terms.Should().HaveCount(2);
        terms.Single(existingTerms => existingTerms.IsCurrent).Origin.Should().Be(CreditTermsOrigin.VipRepricing);
        terms.Single(existingTerms => existingTerms.IsCurrent).BaseAnnualInterestRate.Should().Be(6m);
        terms.Single(existingTerms => !existingTerms.IsCurrent).Origin.Should().Be(CreditTermsOrigin.Origination);
        secondInterestAfter.Should().BeLessThan(secondInterestBefore,
            "по-ниската VIP лихва намалява лихвата върху оставащата главница");
        (await dbContext.CreditPricingChanges.CountAsync(change => change.CreditId == credit.Id))
            .Should().Be(1);
    }

    [Fact]
    public async Task RepriceActiveCreditsForCustomerAsync_WhenRateUnchanged_DoesNothing()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        await SeedConsumerConditionAsync(dbContext);
        var customer = await SeedCustomerAsync(dbContext, isVip: false);

        var credit = await BuildCreditService(dbContext).CreateCreditAsync(new CreateCreditRequest
        {
            CustomerId = customer.Id,
            CreditType = CreditType.Consumer,
            GrantedAmount = 12000m,
            TermMonths = 12,
            InterestRate = 12m, // вече стандартната ставка; оставането нон-VIP не променя нищо
            PaymentType = PaymentType.Annuity,
        });

        await BuildRepricingService(dbContext).RepriceActiveCreditsForCustomerAsync(customer.Id);

        (await dbContext.CreditTerms.CountAsync(terms => terms.CreditId == credit.Id)).Should().Be(1);
        (await dbContext.CreditPricingChanges.CountAsync(change => change.CreditId == credit.Id)).Should().Be(0);
    }

    private static async Task SeedConsumerConditionAsync(AppDbContext dbContext)
    {
        dbContext.CreditTypeConditions.Add(new CreditTypeCondition
        {
            CreditType = CreditType.Consumer,
            Name = "Consumer",
            StandardAnnualInterestRate = 12m,
            VipAnnualInterestRate = 6m,
            MaximumAmount = 100000m,
            MaximumTermMonths = 120,
            StandardGrantingFee = 100m,
            VipGrantingFee = 50m,
            IsActive = true,
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task<Customer> SeedCustomerAsync(AppDbContext dbContext, bool isVip)
    {
        var customer = new Customer
        {
            CustomerType = CustomerType.Individual,
            Person = new Person { FirstName = "Ivan", LastName = "Petrov", Egn = Guid.NewGuid().ToString("N")[..10] },
            IsVip = isVip,
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        return customer;
    }
}
