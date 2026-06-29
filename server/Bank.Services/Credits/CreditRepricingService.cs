using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Calculators;
using Bank.DB;
using Bank.DB.Entities;
using Bank.Services.Calculators;
using Bank.Services.Users;
using Microsoft.EntityFrameworkCore;

namespace Bank.Services.Credits;

public class CreditRepricingService : ICreditRepricingService
{
    private readonly AppDbContext dbContext;
    private readonly IUserService userService;
    private readonly ICreditCalculatorService creditCalculatorService;

    public CreditRepricingService(
        AppDbContext dbContext,
        IUserService userService,
        ICreditCalculatorService creditCalculatorService)
    {
        this.dbContext = dbContext;
        this.userService = userService;
        this.creditCalculatorService = creditCalculatorService;
    }

    public async Task RepriceActiveCreditsForCustomerAsync(long customerId, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(person => person.Id == customerId, cancellationToken)
            ?? throw new BankException("Клиентът не е намерен.", 404);

        // Зареждаме само текущите условия и предстоящите вноски; платените са непроменими. 30-годишен
        // ипотечен кредит може да има 360 реда, затова взимаме само предстоящата част.
        var activeCredits = await dbContext.Credits
            .Include(credit => credit.CreditTypeCondition)
            .Include(credit => credit.Terms.Where(terms => terms.IsCurrent)).ThenInclude(terms => terms.Fees)
            .Include(credit => credit.Installments.Where(payment => payment.Status == CreditPaymentStatus.Pending))
            .Include(credit => credit.PricingChanges)
            .Where(credit => credit.CustomerId == customerId && credit.Status == CreditStatus.Active)
            .ToListAsync(cancellationToken);

        if (activeCredits.Count == 0)
        {
            return;
        }

        var hasChanges = false;
        foreach (var credit in activeCredits)
        {
            if (await TryRepriceAsync(credit, customer.IsVip))
            {
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await dbContext.SaveChangesAsync(userService.LoggedInUserId, cancellationToken);
        }
    }

    private async Task<bool> TryRepriceAsync(Credit credit, bool isVip)
    {
        var currentTerms = credit.Terms.FirstOrDefault(terms => terms.IsCurrent);
        if (currentTerms == null)
        {
            return false;
        }

        var condition = credit.CreditTypeCondition;
        var newBaseRate = isVip ? condition.VipAnnualInterestRate : condition.StandardAnnualInterestRate;
        if (newBaseRate == currentTerms.BaseAnnualInterestRate)
        {
            return false;
        }

        var pendingPayments = credit.Installments
            .Where(payment => payment.Status == CreditPaymentStatus.Pending)
            .OrderBy(payment => payment.InstallmentNumber)
            .ToArray();
        if (pendingPayments.Length == 0)
        {
            return false;
        }

        var firstPending = pendingPayments[0];
        var openingPrincipal = decimal.Round(firstPending.PrincipalPart + firstPending.RemainingPrincipalAfterPayment, 2, MidpointRounding.AwayFromZero);
        if (openingPrincipal <= 0m)
        {
            return false;
        }

        var elapsedMonths = firstPending.InstallmentNumber - 1;
        var remainingPromo = Math.Max(0, currentTerms.PromoPeriodMonths - elapsedMonths);
        var remainingGrace = Math.Max(0, currentTerms.GracePeriodMonths - elapsedMonths);
        var newPromoRate = isVip ? condition.VipPromoRate : condition.StandardPromoRate;
        var monthlyFee = isVip ? condition.VipMonthlyManagementFee : condition.StandardMonthlyManagementFee;
        var annualFee = isVip ? condition.VipAnnualManagementFee : condition.StandardAnnualManagementFee;

        var calcRequest = new CreditCalculatorRequest
        {
            LoanAmount = openingPrincipal,
            TermInMonths = pendingPayments.Length,
            InterestRate = newBaseRate,
            PaymentType = currentTerms.PaymentType,
            PromoPeriod = remainingPromo > 0 ? remainingPromo : null,
            PromoRate = remainingPromo > 0 ? newPromoRate : null,
            GracePeriod = remainingGrace > 0 ? remainingGrace : null,
            MonthlyManagementFee = monthlyFee > 0 ? new Fee { Type = FeeType.Currency, Value = monthlyFee } : null,
            AnnualManagementFee = annualFee > 0 ? new Fee { Type = FeeType.Currency, Value = annualFee } : null,
        };

        var recalculation = await creditCalculatorService.CalculateAsync(calcRequest);
        var newItems = recalculation.PaymentSchedule
            .Where(item => item.Month >= 1)
            .OrderBy(item => item.Month)
            .ToArray();

        for (var index = 0; index < pendingPayments.Length; index++)
        {
            var target = pendingPayments[index];
            var source = newItems[index];
            target.InstallmentAmount = source.Payment;
            target.PrincipalPart = source.Principal;
            target.InterestPart = source.Interest;
            target.RemainingPrincipalAfterPayment = decimal.Round(source.RemainingBalance - source.Principal, 2, MidpointRounding.AwayFromZero);
            target.FeePart = source.Fees;
        }

        currentTerms.IsCurrent = false;

        var newTerms = new CreditTerms
        {
            CreditId = credit.Id,
            IsCurrent = true,
            EffectiveFromPaymentNumber = firstPending.InstallmentNumber,
            Origin = CreditTermsOrigin.VipRepricing,
            PaymentType = currentTerms.PaymentType,
            BaseAnnualInterestRate = newBaseRate,
            PromoPeriodMonths = remainingPromo,
            PromoAnnualInterestRate = remainingPromo > 0 ? newPromoRate : null,
            GracePeriodMonths = remainingGrace,
            Apr = recalculation.APR,
            WasVipApplied = isVip,
            PlannedMonthlyPaymentAmount = recalculation.AverageMonthlyPayment,
            Fees = new List<CreditTermsFee>(),
        };
        if (monthlyFee > 0)
        {
            newTerms.Fees.Add(new CreditTermsFee { Kind = CreditFeeKind.MonthlyManagement, Type = FeeType.Currency, Value = monthlyFee });
        }
        if (annualFee > 0)
        {
            newTerms.Fees.Add(new CreditTermsFee { Kind = CreditFeeKind.AnnualManagement, Type = FeeType.Currency, Value = annualFee });
        }
        credit.Terms.Add(newTerms);

        credit.AppliedAnnualInterestRate = newBaseRate;
        credit.PlannedMonthlyPaymentAmount = recalculation.AverageMonthlyPayment;
        credit.PricingChanges.Add(new CreditPricingChange
        {
            PreviousAnnualInterestRate = currentTerms.BaseAnnualInterestRate,
            NewAnnualInterestRate = newBaseRate,
            EffectiveFromPaymentNumber = firstPending.InstallmentNumber,
            Reason = PricingChangeReason.VipStatusChanged,
        });

        return true;
    }
}
