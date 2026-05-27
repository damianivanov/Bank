namespace Bank.Services.Credits;

public readonly record struct VipPricingSnapshot(decimal AnnualInterestRate, decimal GrantingFee, bool IsVipApplied);
