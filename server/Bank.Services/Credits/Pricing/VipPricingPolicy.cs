using Bank.DB.Entities;

namespace Bank.Services.Credits;

public class VipPricingPolicy : IVipPricingPolicy
{
    public VipPricingSnapshot Resolve(CreditTypeCondition condition, bool isVipCustomer)
    {
        if (isVipCustomer)
        {
            return new VipPricingSnapshot(condition.VipAnnualInterestRate, condition.VipGrantingFee, true);
        }

        return new VipPricingSnapshot(condition.StandardAnnualInterestRate, condition.StandardGrantingFee, false);
    }
}
