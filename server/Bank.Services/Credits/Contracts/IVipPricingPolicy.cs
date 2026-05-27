using Bank.DB.Entities;

namespace Bank.Services.Credits;

public interface IVipPricingPolicy
{
    VipPricingSnapshot Resolve(CreditTypeCondition condition, bool isVipCustomer);
}
