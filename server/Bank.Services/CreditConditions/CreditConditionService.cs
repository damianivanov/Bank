using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Bank.CreditConditions;
using Bank.Core.Validation;
using Bank.DB;
using Bank.DB.Entities;
using Bank.Services.Users;
using Microsoft.EntityFrameworkCore;

namespace Bank.Services.CreditConditions;

public class CreditConditionService : ICreditConditionService
{
    private readonly AppDbContext dbContext;
    private readonly IUserService userService;

    public CreditConditionService(AppDbContext dbContext, IUserService userService)
    {
        this.dbContext = dbContext;
        this.userService = userService;
    }

    public async Task<IReadOnlyCollection<CreditTypeConditionModel>> GetCreditConditionsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.CreditTypeConditions
            .AsNoTracking()
            .OrderBy(condition => condition.CreditType)
            .Select(condition => new CreditTypeConditionModel
            {
                Id = condition.Id,
                CreditType = condition.CreditType,
                Name = condition.Name,
                StandardAnnualInterestRate = condition.StandardAnnualInterestRate,
                VipAnnualInterestRate = condition.VipAnnualInterestRate,
                MaximumAmount = condition.MaximumAmount,
                MaximumTermMonths = condition.MaximumTermMonths,
                StandardGrantingFee = condition.StandardGrantingFee,
                VipGrantingFee = condition.VipGrantingFee,
                DefaultPaymentType = condition.DefaultPaymentType,
                PromoPeriodMonths = condition.PromoPeriodMonths,
                StandardPromoRate = condition.StandardPromoRate,
                VipPromoRate = condition.VipPromoRate,
                GracePeriodMonths = condition.GracePeriodMonths,
                StandardMonthlyManagementFee = condition.StandardMonthlyManagementFee,
                VipMonthlyManagementFee = condition.VipMonthlyManagementFee,
                StandardAnnualManagementFee = condition.StandardAnnualManagementFee,
                VipAnnualManagementFee = condition.VipAnnualManagementFee,
                IsActive = condition.IsActive,
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PublicCreditConditionModel>> GetPublicCreditConditionsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.CreditTypeConditions
            .AsNoTracking()
            .Where(condition => condition.IsActive)
            .OrderBy(condition => condition.CreditType)
            .Select(condition => new PublicCreditConditionModel
            {
                Id = condition.Id,
                CreditType = condition.CreditType,
                Name = condition.Name,
                StandardAnnualInterestRate = condition.StandardAnnualInterestRate,
                MaximumAmount = condition.MaximumAmount,
                MaximumTermMonths = condition.MaximumTermMonths,
                DefaultPaymentType = condition.DefaultPaymentType,
                PromoPeriodMonths = condition.PromoPeriodMonths,
                StandardPromoRate = condition.StandardPromoRate,
                GracePeriodMonths = condition.GracePeriodMonths,
                StandardGrantingFee = condition.StandardGrantingFee,
                StandardMonthlyManagementFee = condition.StandardMonthlyManagementFee,
                StandardAnnualManagementFee = condition.StandardAnnualManagementFee,
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task<CreditTypeConditionModel> UpdateCreditConditionAsync(long id, UpdateCreditConditionRequest request, CancellationToken cancellationToken = default)
    {
        var userId = userService.GetRequiredLoggedInUserId();

        var condition = await dbContext.CreditTypeConditions
            .FirstOrDefaultAsync(condition => condition.Id == id, cancellationToken)
            ?? throw new BankException("Кредитното условие не е намерено.", 404);

        Validate(request);

        condition.StandardAnnualInterestRate = request.StandardAnnualInterestRate;
        condition.VipAnnualInterestRate = request.VipAnnualInterestRate;
        condition.MaximumAmount = request.MaximumAmount;
        condition.MaximumTermMonths = request.MaximumTermMonths;
        condition.StandardGrantingFee = request.StandardGrantingFee;
        condition.VipGrantingFee = request.VipGrantingFee;

        await dbContext.SaveChangesAsync(userId, cancellationToken);

        return MapToModel(condition);
    }

    private static void Validate(UpdateCreditConditionRequest request)
    {
        if (request.StandardAnnualInterestRate < 0m || request.StandardAnnualInterestRate > 100m
            || request.VipAnnualInterestRate < 0m || request.VipAnnualInterestRate > 100m)
        {
            throw new BankException("Лихвеният процент трябва да е между 0 и 100.");
        }

        if (request.MaximumAmount <= 0m)
        {
            throw new BankException("Максималната сума трябва да е положителна.");
        }

        if (request.MaximumTermMonths < 1 || request.MaximumTermMonths > CalculatorLimits.MaxTermMonths)
        {
            throw new BankException($"Максималният срок трябва да е между 1 и {CalculatorLimits.MaxTermMonths} месеца.");
        }

        if (request.StandardGrantingFee < 0m || request.VipGrantingFee < 0m)
        {
            throw new BankException("Таксите не могат да са отрицателни.");
        }

        // VIP никога не трябва да е по-неизгодна от стандартната.
        if (request.VipAnnualInterestRate > request.StandardAnnualInterestRate)
        {
            throw new BankException("VIP лихвата не може да е по-висока от стандартната.");
        }

        if (request.VipGrantingFee > request.StandardGrantingFee)
        {
            throw new BankException("VIP таксата не може да е по-висока от стандартната.");
        }
    }

    private static CreditTypeConditionModel MapToModel(CreditTypeCondition condition)
    {
        return new CreditTypeConditionModel
        {
            Id = condition.Id,
            CreditType = condition.CreditType,
            Name = condition.Name,
            StandardAnnualInterestRate = condition.StandardAnnualInterestRate,
            VipAnnualInterestRate = condition.VipAnnualInterestRate,
            MaximumAmount = condition.MaximumAmount,
            MaximumTermMonths = condition.MaximumTermMonths,
            StandardGrantingFee = condition.StandardGrantingFee,
            VipGrantingFee = condition.VipGrantingFee,
            DefaultPaymentType = condition.DefaultPaymentType,
            PromoPeriodMonths = condition.PromoPeriodMonths,
            StandardPromoRate = condition.StandardPromoRate,
            VipPromoRate = condition.VipPromoRate,
            GracePeriodMonths = condition.GracePeriodMonths,
            StandardMonthlyManagementFee = condition.StandardMonthlyManagementFee,
            VipMonthlyManagementFee = condition.VipMonthlyManagementFee,
            StandardAnnualManagementFee = condition.StandardAnnualManagementFee,
            VipAnnualManagementFee = condition.VipAnnualManagementFee,
            IsActive = condition.IsActive,
        };
    }
}
