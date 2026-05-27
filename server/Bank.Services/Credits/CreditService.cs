using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Bank.Credits;
using Bank.DB;
using Bank.DB.Entities;
using Bank.Services.Common;
using Bank.Services.Users;
using Microsoft.EntityFrameworkCore;

namespace Bank.Services.Credits;

public class CreditService : ICreditService
{
    private readonly AppDbContext dbContext;
    private readonly IUserService userService;
    private readonly IRepaymentPlanCalculator repaymentPlanCalculator;
    private readonly IVipPricingPolicy vipPricingPolicy;

    public CreditService(
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

    public async Task<IReadOnlyCollection<CreditModel>> GetCreditsAsync(CancellationToken cancellationToken = default)
    {
        var credits = await dbContext.Credits
            .AsNoTracking()
            .Include(credit => credit.Customer)
            .Include(credit => credit.CreditTypeCondition)
            .OrderByDescending(credit => credit.GrantedAtUtc)
            .ToListAsync(cancellationToken);

        return credits.Select(MapCredit).ToArray();
    }

    public async Task<CreditDetailsModel> GetCreditAsync(long creditId, CancellationToken cancellationToken = default)
    {
        var credit = await dbContext.Credits
            .AsNoTracking()
            .Include(entity => entity.Customer)
            .Include(entity => entity.CreditTypeCondition)
            .Include(entity => entity.Payments)
            .Include(entity => entity.PricingChanges)
            .FirstOrDefaultAsync(entity => entity.Id == creditId, cancellationToken)
            ?? throw new BankException("Credit was not found.", 404);

        return MapCreditDetails(credit);
    }

    public async Task<CreditDetailsModel> CreateCreditAsync(CreateCreditRequest request, CancellationToken cancellationToken = default)
    {
        var userId = userService.GetRequiredLoggedInUserId();

        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(entity => entity.Id == request.CustomerId, cancellationToken)
            ?? throw new BankException("Customer was not found.", 404);

        var creditCondition = await dbContext.CreditTypeConditions
            .FirstOrDefaultAsync(entity => entity.CreditType == request.CreditType && entity.IsActive, cancellationToken)
            ?? throw new BankException("Credit condition was not found or is inactive.");

        if (request.GrantedAmount <= 0m)
        {
            throw new BankException("Granted amount must be greater than zero.");
        }

        if (request.GrantedAmount > creditCondition.MaximumAmount)
        {
            throw new BankException($"Granted amount exceeds the maximum allowed amount ({creditCondition.MaximumAmount:F2}).");
        }

        if (request.TermMonths <= 0)
        {
            throw new BankException("Term months must be greater than zero.");
        }

        if (request.TermMonths > creditCondition.MaximumTermMonths)
        {
            throw new BankException($"Term months exceed the maximum allowed term ({creditCondition.MaximumTermMonths}).");
        }

        var pricing = vipPricingPolicy.Resolve(creditCondition, customer.IsVip);
        var grantedAtUtc = DateTime.UtcNow;
        var calculation = repaymentPlanCalculator.Calculate(request.GrantedAmount, pricing.AnnualInterestRate, request.TermMonths, grantedAtUtc);

        var credit = new Credit
        {
            CustomerId = customer.Id,
            CreditTypeConditionId = creditCondition.Id,
            GrantedAmount = decimal.Round(request.GrantedAmount, 2, MidpointRounding.AwayFromZero),
            TermMonths = request.TermMonths,
            AppliedAnnualInterestRate = pricing.AnnualInterestRate,
            AppliedGrantingFee = pricing.GrantingFee,
            CustomerWasVipAtCreation = pricing.IsVipApplied,
            PlannedMonthlyPaymentAmount = calculation.PlannedMonthlyPaymentAmount,
            Status = CreditStatus.Active,
            GrantedAtUtc = grantedAtUtc,
        };

        credit.Payments = calculation.Payments
            .Select(payment => new CreditPayment
            {
                PaymentNumber = payment.PaymentNumber,
                DueDate = payment.DueDate,
                PaymentAmount = payment.PaymentAmount,
                PrincipalPart = payment.PrincipalPart,
                InterestPart = payment.InterestPart,
                RemainingPrincipalAfterPayment = payment.RemainingPrincipalAfterPayment,
                Status = CreditPaymentStatus.Pending,
            })
            .ToArray();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.Credits.Add(credit);
        await dbContext.SaveChangesAsync(userId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetCreditAsync(credit.Id, cancellationToken);
    }

    public async Task<CreditDetailsModel> PayPaymentAsync(long creditId, long paymentId, CancellationToken cancellationToken = default)
    {
        var userId = userService.GetRequiredLoggedInUserId();

        var credit = await dbContext.Credits
            .Include(entity => entity.Customer)
            .Include(entity => entity.CreditTypeCondition)
            .Include(entity => entity.Payments)
            .Include(entity => entity.PricingChanges)
            .FirstOrDefaultAsync(entity => entity.Id == creditId, cancellationToken)
            ?? throw new BankException("Credit was not found.", 404);

        if (credit.Status != CreditStatus.Active)
        {
            throw new BankException("Only active credits can accept credit payments.");
        }

        var nextPendingPayment = credit.Payments
            .OrderBy(payment => payment.PaymentNumber)
            .FirstOrDefault(payment => payment.Status == CreditPaymentStatus.Pending);

        if (nextPendingPayment == null)
        {
            throw new BankException("Credit has no pending payments.");
        }

        if (nextPendingPayment.Id != paymentId)
        {
            throw new BankException("Only the next pending payment can be paid.");
        }

        nextPendingPayment.Status = CreditPaymentStatus.Paid;
        nextPendingPayment.PaidAtUtc = DateTime.UtcNow;

        if (credit.Payments.All(payment => payment.Status == CreditPaymentStatus.Paid))
        {
            credit.Status = CreditStatus.Repaid;
            credit.RepaidAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(userId, cancellationToken);
        return MapCreditDetails(credit);
    }

    private static CreditModel MapCredit(Credit credit)
    {
        return new CreditModel
        {
            Id = credit.Id,
            CustomerId = credit.CustomerId,
            CustomerDisplayName = CustomerDisplayNameFormatter.BuildDisplayName(credit.Customer),
            CreditType = credit.CreditTypeCondition.CreditType,
            GrantedAmount = credit.GrantedAmount,
            TermMonths = credit.TermMonths,
            AppliedAnnualInterestRate = credit.AppliedAnnualInterestRate,
            AppliedGrantingFee = credit.AppliedGrantingFee,
            CustomerWasVipAtCreation = credit.CustomerWasVipAtCreation,
            PlannedMonthlyPaymentAmount = credit.PlannedMonthlyPaymentAmount,
            Status = credit.Status,
            GrantedAtUtc = credit.GrantedAtUtc,
            RepaidAtUtc = credit.RepaidAtUtc,
        };
    }

    private static CreditDetailsModel MapCreditDetails(Credit credit)
    {
        var lastPricingChange = credit.PricingChanges
            .OrderByDescending(change => change.DateCreated)
            .FirstOrDefault();

        return new CreditDetailsModel
        {
            Id = credit.Id,
            CustomerId = credit.CustomerId,
            CustomerDisplayName = CustomerDisplayNameFormatter.BuildDisplayName(credit.Customer),
            CreditType = credit.CreditTypeCondition.CreditType,
            GrantedAmount = credit.GrantedAmount,
            TermMonths = credit.TermMonths,
            AppliedAnnualInterestRate = credit.AppliedAnnualInterestRate,
            AppliedGrantingFee = credit.AppliedGrantingFee,
            CustomerWasVipAtCreation = credit.CustomerWasVipAtCreation,
            PlannedMonthlyPaymentAmount = credit.PlannedMonthlyPaymentAmount,
            CurrentAnnualInterestRate = credit.AppliedAnnualInterestRate,
            Status = credit.Status,
            GrantedAtUtc = credit.GrantedAtUtc,
            RepaidAtUtc = credit.RepaidAtUtc,
            LastPricingChange = lastPricingChange == null
                ? null
                : new CreditPricingChangeModel
                {
                    Id = lastPricingChange.Id,
                    PreviousAnnualInterestRate = lastPricingChange.PreviousAnnualInterestRate,
                    NewAnnualInterestRate = lastPricingChange.NewAnnualInterestRate,
                    EffectiveFromPaymentNumber = lastPricingChange.EffectiveFromPaymentNumber,
                    Reason = lastPricingChange.Reason,
                    DateCreated = lastPricingChange.DateCreated,
                },
            Payments = credit.Payments
                .OrderBy(payment => payment.PaymentNumber)
                .Select(payment => new CreditPaymentModel
                {
                    Id = payment.Id,
                    PaymentNumber = payment.PaymentNumber,
                    DueDate = payment.DueDate,
                    PaymentAmount = payment.PaymentAmount,
                    PrincipalPart = payment.PrincipalPart,
                    InterestPart = payment.InterestPart,
                    RemainingPrincipalAfterPayment = payment.RemainingPrincipalAfterPayment,
                    Status = payment.Status,
                    PaidAtUtc = payment.PaidAtUtc,
                })
                .ToArray(),
        };
    }
}
