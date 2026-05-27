using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.DB;
using Bank.DB.Entities;
using Bank.Services.Users;
using Microsoft.EntityFrameworkCore;

namespace Bank.Services.Credits;

public class CreditRepricingService : ICreditRepricingService
{
    private readonly AppDbContext dbContext;
    private readonly IUserService userService;
    private readonly IRepaymentPlanCalculator repaymentPlanCalculator;
    private readonly IVipPricingPolicy vipPricingPolicy;

    public CreditRepricingService(
        AppDbContext dbContext,
        IUserService userService,
        IRepaymentPlanCalculator repaymentPlanCalculator,
        IVipPricingPolicy vipPricingPolicy)
    {
        this.dbContext = dbContext;
        this.userService = userService;
        this.repaymentPlanCalculator = repaymentPlanCalculator;
        this.vipPricingPolicy = vipPricingPolicy;
    }

    public async Task RepriceActiveCreditsForCustomerAsync(long customerId, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(entity => entity.Id == customerId, cancellationToken)
            ?? throw new BankException("Customer was not found.", 404);

        var activeCredits = await dbContext.Credits
            .Include(credit => credit.CreditTypeCondition)
            .Include(credit => credit.Payments)
            .Where(credit => credit.CustomerId == customerId && credit.Status == CreditStatus.Active)
            .ToListAsync(cancellationToken);

        if (activeCredits.Count == 0)
        {
            return;
        }

        var hasChanges = false;
        foreach (var credit in activeCredits)
        {
            var pricing = vipPricingPolicy.Resolve(credit.CreditTypeCondition, customer.IsVip);
            if (pricing.AnnualInterestRate == credit.AppliedAnnualInterestRate)
            {
                continue;
            }

            var pendingPayments = credit.Payments
                .Where(payment => payment.Status == CreditPaymentStatus.Pending)
                .OrderBy(payment => payment.PaymentNumber)
                .ToArray();

            if (pendingPayments.Length == 0)
            {
                continue;
            }

            var firstPending = pendingPayments[0];
            var openingPrincipal = decimal.Round(firstPending.PrincipalPart + firstPending.RemainingPrincipalAfterPayment, 2, MidpointRounding.AwayFromZero);
            if (openingPrincipal <= 0m)
            {
                continue;
            }

            var rateBeforeChange = credit.AppliedAnnualInterestRate;
            var scheduleStart = firstPending.DueDate.AddMonths(-1);
            var recalculation = repaymentPlanCalculator.Calculate(openingPrincipal, pricing.AnnualInterestRate, pendingPayments.Length, scheduleStart);

            for (var index = 0; index < pendingPayments.Length; index++)
            {
                var targetPayment = pendingPayments[index];
                var sourcePayment = recalculation.Payments.ElementAt(index);

                targetPayment.PaymentAmount = sourcePayment.PaymentAmount;
                targetPayment.PrincipalPart = sourcePayment.PrincipalPart;
                targetPayment.InterestPart = sourcePayment.InterestPart;
                targetPayment.RemainingPrincipalAfterPayment = sourcePayment.RemainingPrincipalAfterPayment;
            }

            credit.AppliedAnnualInterestRate = pricing.AnnualInterestRate;
            credit.PlannedMonthlyPaymentAmount = recalculation.PlannedMonthlyPaymentAmount;
            credit.PricingChanges.Add(new CreditPricingChange
            {
                PreviousAnnualInterestRate = rateBeforeChange,
                NewAnnualInterestRate = pricing.AnnualInterestRate,
                EffectiveFromPaymentNumber = firstPending.PaymentNumber,
                Reason = PricingChangeReason.VipStatusChanged,
            });

            hasChanges = true;
        }

        if (hasChanges)
        {
            await dbContext.SaveChangesAsync(userService.LoggedInUserId, cancellationToken);
        }
    }
}
