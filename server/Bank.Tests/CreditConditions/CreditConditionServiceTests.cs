using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Bank.CreditConditions;
using Bank.DB;
using Bank.DB.Entities;
using Bank.Services.CreditConditions;
using Bank.Tests.Infrastructure;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;

namespace Bank.Tests.CreditConditions;

public class CreditConditionServiceTests
{
    private static CreditConditionService BuildService(AppDbContext dbContext) =>
        new(dbContext, new FakeUserService());

    private static async Task<CreditTypeCondition> SeedConditionAsync(AppDbContext dbContext)
    {
        var condition = new CreditTypeCondition
        {
            CreditType = CreditType.Consumer,
            Name = "Потребителски кредит",
            StandardAnnualInterestRate = 7.20m,
            VipAnnualInterestRate = 5.90m,
            MaximumAmount = 80000m,
            MaximumTermMonths = 120,
            StandardGrantingFee = 120m,
            VipGrantingFee = 60m,
            DefaultPaymentType = PaymentType.Annuity,
            PromoPeriodMonths = 3,
            StandardPromoRate = 4.90m,
            VipPromoRate = 3.90m,
            GracePeriodMonths = 0,
            StandardMonthlyManagementFee = 4m,
            VipMonthlyManagementFee = 2m,
            StandardAnnualManagementFee = 0m,
            VipAnnualManagementFee = 0m,
            IsActive = true,
        };

        dbContext.CreditTypeConditions.Add(condition);
        await dbContext.SaveChangesAsync();
        return condition;
    }

    private static UpdateCreditConditionRequest ValidRequest() => new()
    {
        StandardAnnualInterestRate = 6.50m,
        VipAnnualInterestRate = 5.10m,
        MaximumAmount = 90000m,
        MaximumTermMonths = 96,
        StandardGrantingFee = 150m,
        VipGrantingFee = 75m,
    };

    [Fact]
    public async Task UpdateCreditConditionAsync_UpdatesEditableFieldsAndLeavesRestUntouched()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var condition = await SeedConditionAsync(dbContext);
        var service = BuildService(dbContext);

        var result = await service.UpdateCreditConditionAsync(condition.Id, ValidRequest());

        var stored = await dbContext.CreditTypeConditions.AsNoTracking().SingleAsync(c => c.Id == condition.Id);

        using var _ = new AssertionScope();
        // Върнатият модел отразява новите стойности.
        result.StandardAnnualInterestRate.Should().Be(6.50m);
        result.VipAnnualInterestRate.Should().Be(5.10m);
        result.MaximumAmount.Should().Be(90000m);
        result.MaximumTermMonths.Should().Be(96);
        result.StandardGrantingFee.Should().Be(150m);
        result.VipGrantingFee.Should().Be(75m);
        // Записаното в базата също.
        stored.StandardAnnualInterestRate.Should().Be(6.50m);
        stored.MaximumTermMonths.Should().Be(96);
        stored.VipGrantingFee.Should().Be(75m);
        // Полета извън обхвата на редакцията остават непроменени.
        stored.Name.Should().Be("Потребителски кредит");
        stored.StandardMonthlyManagementFee.Should().Be(4m);
        stored.StandardPromoRate.Should().Be(4.90m);
        stored.PromoPeriodMonths.Should().Be(3);
        stored.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCreditConditionAsync_WhenConditionMissing_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var service = BuildService(dbContext);

        var act = () => service.UpdateCreditConditionAsync(999, ValidRequest());

        await act.Should().ThrowAsync<BankException>().Where(e => e.StatusCode == 404);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    public async Task UpdateCreditConditionAsync_WhenStandardRateOutOfRange_Throws(decimal rate)
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var condition = await SeedConditionAsync(dbContext);
        var service = BuildService(dbContext);

        var request = ValidRequest();
        request.StandardAnnualInterestRate = rate;

        await FluentActions.Awaiting(() => service.UpdateCreditConditionAsync(condition.Id, request))
            .Should().ThrowAsync<BankException>();
    }

    [Fact]
    public async Task UpdateCreditConditionAsync_WhenMaximumAmountNotPositive_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var condition = await SeedConditionAsync(dbContext);
        var service = BuildService(dbContext);

        var request = ValidRequest();
        request.MaximumAmount = 0m;

        await FluentActions.Awaiting(() => service.UpdateCreditConditionAsync(condition.Id, request))
            .Should().ThrowAsync<BankException>();
    }

    [Fact]
    public async Task UpdateCreditConditionAsync_WhenMaximumTermNotPositive_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var condition = await SeedConditionAsync(dbContext);
        var service = BuildService(dbContext);

        var request = ValidRequest();
        request.MaximumTermMonths = 0;

        await FluentActions.Awaiting(() => service.UpdateCreditConditionAsync(condition.Id, request))
            .Should().ThrowAsync<BankException>();
    }

    [Fact]
    public async Task UpdateCreditConditionAsync_WhenFeeNegative_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var condition = await SeedConditionAsync(dbContext);
        var service = BuildService(dbContext);

        var request = ValidRequest();
        request.VipGrantingFee = -1m;

        await FluentActions.Awaiting(() => service.UpdateCreditConditionAsync(condition.Id, request))
            .Should().ThrowAsync<BankException>();
    }

    [Fact]
    public async Task UpdateCreditConditionAsync_WhenVipRateAboveStandard_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var condition = await SeedConditionAsync(dbContext);
        var service = BuildService(dbContext);

        var request = ValidRequest();
        request.StandardAnnualInterestRate = 6.00m;
        request.VipAnnualInterestRate = 7.00m;

        await FluentActions.Awaiting(() => service.UpdateCreditConditionAsync(condition.Id, request))
            .Should().ThrowAsync<BankException>();
    }

    [Fact]
    public async Task UpdateCreditConditionAsync_WhenVipFeeAboveStandard_Throws()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var condition = await SeedConditionAsync(dbContext);
        var service = BuildService(dbContext);

        var request = ValidRequest();
        request.StandardGrantingFee = 100m;
        request.VipGrantingFee = 120m;

        await FluentActions.Awaiting(() => service.UpdateCreditConditionAsync(condition.Id, request))
            .Should().ThrowAsync<BankException>();
    }

    [Fact]
    public async Task UpdateCreditConditionAsync_WhenVipEqualsStandard_Succeeds()
    {
        await using var dbContext = TestDbContextFactory.CreateContext(Guid.NewGuid().ToString("N"));
        var condition = await SeedConditionAsync(dbContext);
        var service = BuildService(dbContext);

        var request = ValidRequest();
        request.StandardAnnualInterestRate = 6.50m;
        request.VipAnnualInterestRate = 6.50m;
        request.StandardGrantingFee = 150m;
        request.VipGrantingFee = 150m;

        var result = await service.UpdateCreditConditionAsync(condition.Id, request);

        using var _ = new AssertionScope();
        result.VipAnnualInterestRate.Should().Be(6.50m);
        result.VipGrantingFee.Should().Be(150m);
    }
}
