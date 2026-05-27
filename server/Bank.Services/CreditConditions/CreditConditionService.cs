using Bank.Core.JsonModels.Bank.CreditConditions;
using Bank.DB;
using Microsoft.EntityFrameworkCore;

namespace Bank.Services.CreditConditions;

public class CreditConditionService : ICreditConditionService
{
    private readonly AppDbContext dbContext;

    public CreditConditionService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
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
                IsActive = condition.IsActive,
            })
            .ToArrayAsync(cancellationToken);
    }
}
