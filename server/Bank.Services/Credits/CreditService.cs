using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Bank.Credits;
using Bank.Core.JsonModels.Calculators;
using Bank.Core.JsonModels.Common;
using Bank.Core.Settings;
using Bank.DB;
using Bank.DB.Entities;
using Bank.Services.Calculators;
using Bank.Services.Common;
using Bank.Services.Users;
using Microsoft.EntityFrameworkCore;

namespace Bank.Services.Credits;

public class CreditService : ICreditService
{
    private const int MaxPageSize = 100;

    private readonly AppDbContext dbContext;
    private readonly IUserService userService;
    private readonly ICreditCalculatorService creditCalculatorService;
    private readonly DemoOptions demoOptions;

    public CreditService(
        AppDbContext dbContext,
        IUserService userService,
        ICreditCalculatorService creditCalculatorService,
        DemoOptions demoOptions)
    {
        this.dbContext = dbContext;
        this.userService = userService;
        this.creditCalculatorService = creditCalculatorService;
        this.demoOptions = demoOptions;
    }

    public async Task<PagedResponse<CreditModel>> GetCreditsAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = dbContext.Credits
            .AsNoTracking()
            // Включваме Person/Company, защото MapCredit формира името на клиента от тях —
            // без тези Include колоната за име на клиента би била празна.
            .Include(credit => credit.Customer).ThenInclude(c => c.Person)
            .Include(credit => credit.Customer).ThenInclude(c => c.Company)
            .Include(credit => credit.CreditTypeCondition)
            .AsQueryable();

        var search = request.Search?.Trim().ToLower();
        if (!string.IsNullOrEmpty(search))
        {
            // Търсене по име на клиента (физическо или юридическо лице). Сравнението е без оглед на
            // регистъра — ToLower се превежда до SQL LOWER и работи еднакво и при InMemory тестовете.
            // Превежда се в SQL, затова филтрирането и страницирането се случват в базата, а не в паметта.
            query = query.Where(credit =>
                (credit.Customer.Person != null
                    && (credit.Customer.Person.FirstName + " " + credit.Customer.Person.LastName).ToLower().Contains(search))
                || (credit.Customer.Company != null && credit.Customer.Company.Name.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Ограничаваме страницата до наличния диапазон, за да не препълни int32 изчислението на отместването
        // (Skip) при огромна стойност за Page и да не се стигне до отрицателен OFFSET в SQL.
        var maxPage = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        if (page > maxPage)
        {
            page = maxPage;
        }

        var credits = await query
            // Вторичен ключ по Id, за да е страницирането детерминирано при еднакво GrantedAtUtc
            // (иначе един и същ запис може да се появи на две страници или да бъде пропуснат).
            .OrderByDescending(credit => credit.GrantedAtUtc)
            .ThenByDescending(credit => credit.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<CreditModel>
        {
            Items = credits.Select(MapCredit).ToArray(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<CreditDetailsModel> GetCreditAsync(long creditId, CancellationToken cancellationToken = default)
    {
        var credit = await QueryCreditDetails()
            .AsNoTracking()
            .FirstOrDefaultAsync(existingCredit => existingCredit.Id == creditId, cancellationToken)
            ?? throw new BankException("Кредитът не е намерен.", 404);

        return MapCreditDetails(credit, demoOptions.AllowPayingFutureInstallments);
    }

    public async Task<CreditDetailsModel> GetCreditForCustomerAsync(long customerId, long creditId, CancellationToken cancellationToken = default)
    {
        var credit = await QueryCreditDetails()
            .AsNoTracking()
            .FirstOrDefaultAsync(existingCredit => existingCredit.Id == creditId && existingCredit.CustomerId == customerId, cancellationToken)
            ?? throw new BankException("Кредитът не е намерен.", 404);

        return MapCreditDetails(credit, demoOptions.AllowPayingFutureInstallments);
    }

    // Тук проверяваме дали кредитът принадлежи на някой от клиентите в customerIds, за да не се разкриват кредити на други клиенти.
    public async Task<CreditDetailsModel> GetCreditForCustomersAsync(IReadOnlyCollection<long> customerIds, long creditId, CancellationToken cancellationToken = default)
    {
        var credit = await QueryCreditDetails()
            .AsNoTracking()
            .FirstOrDefaultAsync(existingCredit => existingCredit.Id == creditId && customerIds.Contains(existingCredit.CustomerId), cancellationToken)
            ?? throw new BankException("Кредитът не е намерен.", 404);

        return MapCreditDetails(credit, demoOptions.AllowPayingFutureInstallments);
    }

    public async Task<CreditDetailsModel> CreateCreditAsync(CreateCreditRequest request, CancellationToken cancellationToken = default)
    {
        var userId = userService.GetRequiredLoggedInUserId();

        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(existingCustomer => existingCustomer.Id == request.CustomerId, cancellationToken)
            ?? throw new BankException("Клиентът не е намерен.", 404);

        var creditCondition = await dbContext.CreditTypeConditions
            .FirstOrDefaultAsync(condition => condition.CreditType == request.CreditType && condition.IsActive, cancellationToken)
            ?? throw new BankException("Условието по кредита не е намерено или е неактивно.");

        if (request.GrantedAmount <= 0m)
        {
            throw new BankException("Отпуснатата сума трябва да е по-голяма от нула.");
        }

        if (request.GrantedAmount > creditCondition.MaximumAmount)
        {
            throw new BankException($"Отпуснатата сума надвишава максимално допустимата сума ({creditCondition.MaximumAmount:F2}).");
        }

        if (request.TermMonths <= 0)
        {
            throw new BankException("Срокът в месеци трябва да е по-голям от нула.");
        }

        if (request.TermMonths > creditCondition.MaximumTermMonths)
        {
            throw new BankException($"Срокът в месеци надвишава максимално допустимия срок ({creditCondition.MaximumTermMonths}).");
        }

        var grantedAtUtc = DateTime.UtcNow;
        var calculation = await creditCalculatorService.CalculateAsync(BuildCalculatorRequest(request));

        var scheduleItems = calculation.PaymentSchedule
            .Where(item => item.Month >= 1)
            .OrderBy(item => item.Month)
            .ToArray();
        var initialFees = calculation.PaymentSchedule.FirstOrDefault(item => item.Month == 0)?.Fees ?? 0m;

        var credit = new Credit
        {
            CustomerId = customer.Id,
            CreditTypeConditionId = creditCondition.Id,
            GrantedAmount = decimal.Round(request.GrantedAmount, 2, MidpointRounding.AwayFromZero),
            TermMonths = request.TermMonths,
            AppliedAnnualInterestRate = request.InterestRate,
            AppliedGrantingFee = initialFees,
            CustomerWasVipAtCreation = customer.IsVip,
            PlannedMonthlyPaymentAmount = calculation.AverageMonthlyPayment,
            Status = CreditStatus.Active,
            GrantedAtUtc = grantedAtUtc,
        };

        var scheduleStart = DateTime.SpecifyKind(grantedAtUtc, DateTimeKind.Utc).Date;
        credit.Installments = scheduleItems
            .Select(item => new CreditInstallment
            {
                InstallmentNumber = item.Month,
                DueDate = scheduleStart.AddMonths(item.Month),
                InstallmentAmount = item.Payment,
                PrincipalPart = item.Principal,
                InterestPart = item.Interest,
                RemainingPrincipalAfterPayment = decimal.Round(item.RemainingBalance - item.Principal, 2, MidpointRounding.AwayFromZero),
                FeePart = item.Fees,
                Status = CreditPaymentStatus.Pending,
            })
            .ToArray();

        credit.Terms = new List<CreditTerms>
        {
            BuildTerms(request, calculation, customer.IsVip, CreditTermsOrigin.Origination, effectiveFromPaymentNumber: 1),
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.Credits.Add(credit);
        await dbContext.SaveChangesAsync(userId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetCreditAsync(credit.Id, cancellationToken);
    }

    internal static CreditCalculatorRequest BuildCalculatorRequest(CreateCreditRequest request) => new()
    {
        LoanAmount = request.GrantedAmount,
        TermInMonths = request.TermMonths,
        InterestRate = request.InterestRate,
        PaymentType = request.PaymentType,
        PromoPeriod = request.PromoPeriod,
        PromoRate = request.PromoRate,
        GracePeriod = request.GracePeriod,
        ApplicationFee = request.ApplicationFee,
        ProcessingFee = request.ProcessingFee,
        OtherInitialFees = request.OtherInitialFees,
        AnnualManagementFee = request.AnnualManagementFee,
        OtherAnnualFees = request.OtherAnnualFees,
        MonthlyManagementFee = request.MonthlyManagementFee,
        OtherMonthlyFees = request.OtherMonthlyFees,
    };

    private static CreditTerms BuildTerms(
        CreateCreditRequest request,
        CreditCalculatorResponse calculation,
        bool wasVip,
        CreditTermsOrigin origin,
        int effectiveFromPaymentNumber)
    {
        return new CreditTerms
        {
            IsCurrent = true,
            EffectiveFromPaymentNumber = effectiveFromPaymentNumber,
            Origin = origin,
            PaymentType = request.PaymentType,
            BaseAnnualInterestRate = request.InterestRate,
            PromoPeriodMonths = request.PromoPeriod ?? 0,
            PromoAnnualInterestRate = request.PromoRate,
            GracePeriodMonths = request.GracePeriod ?? 0,
            Apr = calculation.APR,
            WasVipApplied = wasVip,
            PlannedMonthlyPaymentAmount = calculation.AverageMonthlyPayment,
            Fees = BuildFees(request),
        };
    }

    private static List<CreditTermsFee> BuildFees(CreateCreditRequest request)
    {
        var fees = new List<CreditTermsFee>();

        void Add(CreditFeeKind kind, Fee? fee)
        {
            if (fee is null)
            {
                return;
            }

            fees.Add(new CreditTermsFee { Kind = kind, Type = fee.Type, Value = fee.Value });
        }

        Add(CreditFeeKind.Application, request.ApplicationFee);
        Add(CreditFeeKind.Processing, request.ProcessingFee);
        Add(CreditFeeKind.OtherInitial, request.OtherInitialFees);
        Add(CreditFeeKind.MonthlyManagement, request.MonthlyManagementFee);
        Add(CreditFeeKind.OtherMonthly, request.OtherMonthlyFees);
        Add(CreditFeeKind.AnnualManagement, request.AnnualManagementFee);
        Add(CreditFeeKind.OtherAnnual, request.OtherAnnualFees);
        return fees;
    }

    private IQueryable<Credit> QueryCreditDetails()
    {
        return dbContext.Credits
            .Include(credit => credit.Customer)
            .Include(credit => credit.CreditTypeCondition)
            .Include(credit => credit.Installments)
            .Include(credit => credit.PricingChanges)
            // Зареждаме ВСИЧКИ версии на условията (не само текущата), за да изградим пълната
            // хронология на промените. MapCurrentTerms филтрира IsCurrent в паметта, така че остава коректно.
            .Include(credit => credit.Terms)
                .ThenInclude(terms => terms.Fees)
            .AsSplitQuery();
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

    private static CreditDetailsModel MapCreditDetails(Credit credit, bool allowPayingFutureInstallments)
    {
        var lastPricingChange = credit.PricingChanges
            .OrderByDescending(pricingChange => pricingChange.DateCreated)
            .FirstOrDefault();

        var nextPendingInstallment = credit.Installments
            .Where(installment => installment.Status == CreditPaymentStatus.Pending)
            .OrderBy(installment => installment.InstallmentNumber)
            .FirstOrDefault();

        // Дали клиентът може да плати следващата вноска СЕГА (активен кредит + настъпил падеж, или dev bypass).
        var canPayNextInstallment = credit.Status == CreditStatus.Active
            && nextPendingInstallment != null
            && InstallmentPaymentPolicy.IsInstallmentPayable(nextPendingInstallment.DueDate, DateTime.UtcNow, allowPayingFutureInstallments);

        // Тоталите се сумират от запазения погасителен план (а не се преизчисляват наживо), за да съвпадат
        // точно с реалните вноски след евентуален VIP репрайсинг. InstallmentAmount е главница+лихва без такси;
        // таксите се водят отделно (еднократна такса при отпускане + периодичните FeePart по вноските).
        var totalInterest = credit.Installments.Sum(installment => installment.InterestPart);
        var totalFees = credit.AppliedGrantingFee + credit.Installments.Sum(installment => installment.FeePart);
        var totalAmountWithFees = credit.Installments.Sum(installment => installment.InstallmentAmount) + totalFees;

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
            TotalInterest = totalInterest,
            TotalFees = totalFees,
            TotalAmountWithFees = totalAmountWithFees,
            Status = credit.Status,
            GrantedAtUtc = credit.GrantedAtUtc,
            RepaidAtUtc = credit.RepaidAtUtc,
            LastPricingChange = lastPricingChange == null ? null : MapPricingChange(lastPricingChange),
            CurrentTerms = MapCurrentTerms(credit),
            // Хронологията е подредена от най-новата към най-старата промяна, за да се чете отгоре надолу в прозореца.
            TermsHistory = credit.Terms
                .OrderByDescending(terms => terms.DateCreated)
                .ThenByDescending(terms => terms.EffectiveFromPaymentNumber)
                .Select(MapTermsHistory)
                .ToArray(),
            PricingChanges = credit.PricingChanges
                .OrderByDescending(pricingChange => pricingChange.DateCreated)
                .ThenByDescending(pricingChange => pricingChange.EffectiveFromPaymentNumber)
                .Select(MapPricingChange)
                .ToArray(),
            CanPayNextInstallment = canPayNextInstallment,
            Payments = credit.Installments
                .OrderBy(payment => payment.InstallmentNumber)
                .Select(MapPayment)
                .ToArray(),
        };
    }

    private static CreditTermsModel? MapCurrentTerms(Credit credit)
    {
        var terms = credit.Terms.FirstOrDefault(existingTerms => existingTerms.IsCurrent);
        if (terms == null)
        {
            return null;
        }

        return new CreditTermsModel
        {
            PaymentType = terms.PaymentType,
            BaseAnnualInterestRate = terms.BaseAnnualInterestRate,
            PromoPeriodMonths = terms.PromoPeriodMonths,
            PromoAnnualInterestRate = terms.PromoAnnualInterestRate,
            GracePeriodMonths = terms.GracePeriodMonths,
            Apr = terms.Apr,
            WasVipApplied = terms.WasVipApplied,
            PlannedMonthlyPaymentAmount = terms.PlannedMonthlyPaymentAmount,
            Fees = terms.Fees
                .Select(fee => new CreditTermsFeeModel { Kind = fee.Kind, Type = fee.Type, Value = fee.Value })
                .ToArray(),
        };
    }

    private static CreditTermsHistoryModel MapTermsHistory(CreditTerms terms)
    {
        return new CreditTermsHistoryModel
        {
            Origin = terms.Origin,
            IsCurrent = terms.IsCurrent,
            EffectiveFromPaymentNumber = terms.EffectiveFromPaymentNumber,
            DateCreated = terms.DateCreated,
            PaymentType = terms.PaymentType,
            BaseAnnualInterestRate = terms.BaseAnnualInterestRate,
            PromoPeriodMonths = terms.PromoPeriodMonths,
            PromoAnnualInterestRate = terms.PromoAnnualInterestRate,
            GracePeriodMonths = terms.GracePeriodMonths,
            Apr = terms.Apr,
            WasVipApplied = terms.WasVipApplied,
            PlannedMonthlyPaymentAmount = terms.PlannedMonthlyPaymentAmount,
            Fees = terms.Fees
                .Select(fee => new CreditTermsFeeModel { Kind = fee.Kind, Type = fee.Type, Value = fee.Value })
                .ToArray(),
        };
    }

    private static CreditPricingChangeModel MapPricingChange(CreditPricingChange pricingChange)
    {
        return new CreditPricingChangeModel
        {
            Id = pricingChange.Id,
            PreviousAnnualInterestRate = pricingChange.PreviousAnnualInterestRate,
            NewAnnualInterestRate = pricingChange.NewAnnualInterestRate,
            EffectiveFromPaymentNumber = pricingChange.EffectiveFromPaymentNumber,
            Reason = pricingChange.Reason,
            DateCreated = pricingChange.DateCreated,
        };
    }

    private static CreditPaymentModel MapPayment(CreditInstallment installment)
    {
        return new CreditPaymentModel
        {
            Id = installment.Id,
            PaymentNumber = installment.InstallmentNumber,
            DueDate = installment.DueDate,
            PaymentAmount = installment.InstallmentAmount,
            PrincipalPart = installment.PrincipalPart,
            InterestPart = installment.InterestPart,
            RemainingPrincipalAfterPayment = installment.RemainingPrincipalAfterPayment,
            FeePart = installment.FeePart,
            Status = installment.Status,
            PaidAtUtc = installment.PaidAtUtc,
        };
    }
}
