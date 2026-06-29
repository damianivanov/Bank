using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Bank.Credits;
using Bank.Core.JsonModels.Bank.MoneyOperations;
using Bank.Core.JsonModels.Common;
using Bank.DB;
using Bank.DB.Entities;
using Bank.Core.Settings;
using Bank.Services.Credits;
using Bank.Services.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Bank.Services.MoneyOperations;

public class MoneyOperationService : IMoneyOperationService
{
    private const int MaxConcurrencyAttempts = 5;
    private const int MaxPageSize = 100;

    private readonly AppDbContext dbContext;
    private readonly IUserService userService;
    private readonly IAccountLedger accountLedger;
    private readonly DemoOptions demoOptions;

    public MoneyOperationService(AppDbContext dbContext, IUserService userService, IAccountLedger accountLedger, DemoOptions demoOptions)
    {
        this.dbContext = dbContext;
        this.userService = userService;
        this.accountLedger = accountLedger;
        this.demoOptions = demoOptions;
    }

    public async Task<DepositRequestModel> RequestDepositAsync(
        IReadOnlyCollection<long> accessibleCustomerIds,
        long accountId,
        DepositRequestCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = userService.GetRequiredLoggedInUserId();
        var account = await ResolveOwnedAccountAsync(accessibleCustomerIds, accountId, cancellationToken);
        EnsureAccountIsActive(account);

        var idempotencyKey = $"deposit-req:{request.IdempotencyKey}";
        var requestHash = IdempotencyHash.Compute("deposit-request",
            ("account", IdempotencyHash.Id(account.Id)),
            ("amount", IdempotencyHash.Amount(request.Amount)));

        var existing = await dbContext.DepositRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing != null)
        {
            EnsureSameRequest(existing.RequestHash, requestHash);
            return MapDepositRequest(existing, account.IBAN);
        }

        var depositRequest = new DepositRequest
        {
            BankAccountId = account.Id,
            Amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
            Status = DepositRequestStatus.Pending,
            IdempotencyKey = idempotencyKey,
            RequestHash = requestHash,
        };

        dbContext.DepositRequests.Add(depositRequest);

        try
        {
            await dbContext.SaveChangesAsync(userId, cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // Едновременно повторно изпращане — другата заявка е спечелила. Връщаме нея (идемпотентно).
            dbContext.ChangeTracker.Clear();
            var duplicate = await dbContext.DepositRequests
                .AsNoTracking()
                .FirstAsync(d => d.IdempotencyKey == idempotencyKey, cancellationToken);
            EnsureSameRequest(duplicate.RequestHash, requestHash);
            return MapDepositRequest(duplicate, account.IBAN);
        }

        return MapDepositRequest(depositRequest, account.IBAN);
    }

    public async Task<AccountOperationResultModel> WithdrawAsync(
        IReadOnlyCollection<long> accessibleCustomerIds,
        long accountId,
        WithdrawalCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = userService.GetRequiredLoggedInUserId();
        var account = await ResolveOwnedAccountAsync(accessibleCustomerIds, accountId, cancellationToken);
        var accountIban = account.IBAN;
        var idempotencyKey = $"wd:{request.IdempotencyKey}";
        var requestHash = IdempotencyHash.Compute("withdraw",
            ("account", IdempotencyHash.Id(accountId)),
            ("amount", IdempotencyHash.Amount(request.Amount)));

        var existing = await FindTransactionByKeyAsync(idempotencyKey, cancellationToken);
        if (existing != null)
        {
            EnsureSameRequest(existing.RequestHash, requestHash);
            return BuildAccountResult(accountIban, existing);
        }

        var amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero);

        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var trackedAccount = await dbContext.BankAccounts
                .FirstAsync(a => a.Id == accountId, cancellationToken);

            EnsureAccountIsActive(trackedAccount);
            if (trackedAccount.Balance < amount)
            {
                throw new BankException("Недостатъчна наличност за това теглене.");
            }

            var transaction = accountLedger.Record(new LedgerEntry
            {
                Account = trackedAccount,
                Type = MoneyTransactionType.Withdrawal,
                Amount = amount,
                IdempotencyKey = idempotencyKey,
                RequestHash = requestHash,
            });

            try
            {
                await dbContext.SaveChangesAsync(userId, cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                dbContext.ChangeTracker.Clear();
                var duplicate = await dbContext.MoneyTransactions
                    .AsNoTracking()
                    .FirstAsync(t => t.IdempotencyKey == idempotencyKey, cancellationToken);
                EnsureSameRequest(duplicate.RequestHash, requestHash);
                return BuildAccountResult(accountIban, duplicate);
            }

            return BuildAccountResult(accountIban, transaction);
        }, cancellationToken);
    }

    public async Task<CreditInstallmentPaymentResultModel> PayCreditInstallmentAsync(
        IReadOnlyCollection<long> accessibleCustomerIds,
        long creditId,
        PayCreditInstallmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = userService.GetRequiredLoggedInUserId();

        var credit = await dbContext.Credits
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == creditId, cancellationToken)
            ?? throw new BankException("Кредитът не е намерен.", 404);

        if (!accessibleCustomerIds.Contains(credit.CustomerId))
        {
            throw new BankException("Кредитът не е намерен.", 404);
        }

        if (credit.Status != CreditStatus.Active)
        {
            throw new BankException("Само активни кредити могат да приемат погасителни вноски.");
        }

        var idempotencyKey = $"pay:{request.IdempotencyKey}";
        // Отпечатъкът е върху суровото тяло на заявката — при retry клиентът трябва да прати идентично тяло
        // (null остава null). Ако оригиналът е с FundingAccountId=null (авто-избор), а retry-ят подаде явно
        // същата сметка, отпечатъците се различават и се връща 409. Посоката е безопасна (никога тих replay на
        // различна операция). Нарочно НЕ резолвираме сметката преди хеша, за да не вържем отпечатъка към
        // изменимото "коя сметка е активна сега" и да не пускаме резолвиращите валидации по чистия replay път.
        var requestHash = IdempotencyHash.Compute("pay-installment",
            ("credit", IdempotencyHash.Id(creditId)),
            ("funding", IdempotencyHash.OptionalId(request.FundingAccountId)));

        var existing = await FindTransactionByKeyAsync(idempotencyKey, cancellationToken);
        if (existing != null)
        {
            EnsureSameRequest(existing.RequestHash, requestHash);
            return await BuildPaymentResultFromTransactionAsync(existing, cancellationToken);
        }

        var fundingAccount = await ResolveFundingAccountAsync(credit.CustomerId, request.FundingAccountId, cancellationToken);

        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var trackedCredit = await dbContext.Credits
                .FirstAsync(c => c.Id == creditId, cancellationToken);

            if (trackedCredit.Status != CreditStatus.Active)
            {
                throw new BankException("Само активни кредити могат да приемат погасителни вноски.");
            }

            var nextInstallment = await dbContext.CreditInstallments
                .Where(p => p.CreditId == creditId && p.Status == CreditPaymentStatus.Pending)
                .OrderBy(p => p.InstallmentNumber)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new BankException("Кредитът няма предстоящи вноски.");

            // Клиентът може да погаси само вноска, чийто падеж е настъпил (текущия месец или просрочена).
            // В Development това може да се изключи чрез AllowPayingFutureInstallments, за да се тества без да се чакат реалните дати.
            if (!InstallmentPaymentPolicy.IsInstallmentPayable(
                    nextInstallment.DueDate, DateTime.UtcNow, demoOptions.AllowPayingFutureInstallments))
            {
                throw new BankException(
                    "Все още не е настъпил падежът на следващата вноска. Можете да платите само вноска за текущия месец.");
            }

            var trackedAccount = await dbContext.BankAccounts
                .FirstAsync(a => a.Id == fundingAccount.Id, cancellationToken);

            EnsureAccountIsActive(trackedAccount);
            if (trackedAccount.Balance < nextInstallment.InstallmentAmount)
            {
                throw new BankException("Недостатъчна наличност за плащане на тази вноска.");
            }

            var transaction = accountLedger.Record(new LedgerEntry
            {
                Account = trackedAccount,
                Type = MoneyTransactionType.CreditPayment,
                Amount = nextInstallment.InstallmentAmount,
                IdempotencyKey = idempotencyKey,
                CreditId = creditId,
                CreditPaymentId = nextInstallment.Id,
                RequestHash = requestHash,
            });

            nextInstallment.Status = CreditPaymentStatus.Paid;
            nextInstallment.PaidAtUtc = DateTime.UtcNow;

            // Кредитът е напълно погасен, щом няма друга предстояща вноска (DB още вижда текущата като Pending,
            // затова я изключваме по Id — същата логика като back-office плащането).
            var hasMorePending = await dbContext.CreditInstallments
                .AnyAsync(p => p.CreditId == creditId
                    && p.Status == CreditPaymentStatus.Pending
                    && p.Id != nextInstallment.Id, cancellationToken);

            if (!hasMorePending)
            {
                trackedCredit.Status = CreditStatus.Repaid;
                trackedCredit.RepaidAtUtc = DateTime.UtcNow;
            }

            try
            {
                await dbContext.SaveChangesAsync(userId, cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                dbContext.ChangeTracker.Clear();
                var duplicate = await dbContext.MoneyTransactions
                    .AsNoTracking()
                    .FirstAsync(t => t.IdempotencyKey == idempotencyKey, cancellationToken);
                EnsureSameRequest(duplicate.RequestHash, requestHash);
                return await BuildPaymentResultFromTransactionAsync(duplicate, cancellationToken);
            }

            return new CreditInstallmentPaymentResultModel
            {
                CreditId = trackedCredit.Id,
                CreditStatus = trackedCredit.Status,
                CreditRepaidAtUtc = trackedCredit.RepaidAtUtc,
                Payment = MapPayment(nextInstallment),
                AccountId = trackedAccount.Id,
                AccountIban = trackedAccount.IBAN,
                NewBalance = transaction.BalanceAfter,
                Transaction = MapTransaction(transaction),
            };
        }, cancellationToken);
    }

    public async Task<PagedResponse<MoneyTransactionModel>> GetAccountTransactionsAsync(
        IReadOnlyCollection<long> accessibleCustomerIds,
        long accountId,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        // Проверката за собственост е критична за сигурността и трябва да мине ПРЕДИ да върнем каквото и да е,
        // за да не може клиент да види движенията по чужда сметка чрез страницирането.
        await ResolveOwnedAccountAsync(accessibleCustomerIds, accountId, cancellationToken);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = dbContext.MoneyTransactions
            .AsNoTracking()
            .Where(t => t.BankAccountId == accountId);

        var totalCount = await query.CountAsync(cancellationToken);

        // Ограничаваме страницата до наличния диапазон, за да не препълни int32 изчислението на отместването
        // (Skip) при огромна стойност за Page и да не се стигне до отрицателен OFFSET в SQL.
        var maxPage = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        if (page > maxPage)
        {
            page = maxPage;
        }

        var transactions = await query
            // Вторичен ключ по Id, за да е страницирането детерминирано при еднакво DateCreated
            // (иначе едно и също движение може да се появи на две страници или да бъде пропуснато).
            .OrderByDescending(t => t.DateCreated)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<MoneyTransactionModel>
        {
            Items = transactions.Select(MapTransaction).ToArray(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<IReadOnlyCollection<DepositRequestModel>> GetMyDepositRequestsAsync(
        IReadOnlyCollection<long> accessibleCustomerIds,
        CancellationToken cancellationToken = default)
    {
        var requests = await dbContext.DepositRequests
            .AsNoTracking()
            .Include(d => d.BankAccount)
            .Where(d => accessibleCustomerIds.Contains(d.BankAccount.CustomerId))
            .OrderByDescending(d => d.DateCreated)
            .ThenByDescending(d => d.Id)
            .ToListAsync(cancellationToken);

        return requests.Select(d => MapDepositRequest(d, d.BankAccount.IBAN)).ToArray();
    }

    private async Task<BankAccount> ResolveOwnedAccountAsync(
        IReadOnlyCollection<long> accessibleCustomerIds,
        long accountId,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.BankAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken)
            ?? throw new BankException("Банковата сметка не е намерена.", 404);

        if (!accessibleCustomerIds.Contains(account.CustomerId))
        {
            throw new BankException("Банковата сметка не е намерена.", 404);
        }

        return account;
    }

    private async Task<BankAccount> ResolveFundingAccountAsync(
        long customerId,
        long? fundingAccountId,
        CancellationToken cancellationToken)
    {
        if (fundingAccountId.HasValue)
        {
            var requestedAccount = await dbContext.BankAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == fundingAccountId.Value, cancellationToken)
                ?? throw new BankException("Сметката за финансиране не е намерена.", 404);

            if (requestedAccount.CustomerId != customerId)
            {
                throw new BankException("Сметката за финансиране трябва да принадлежи на същия клиент като кредита.");
            }

            EnsureAccountIsActive(requestedAccount);
            return requestedAccount;
        }

        var activeAccounts = await dbContext.BankAccounts
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId && a.Status == BankAccountStatus.Active)
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken);

        if (activeAccounts.Count == 0)
        {
            throw new BankException("Няма активна сметка, от която да се плати тази вноска.");
        }

        if (activeAccounts.Count > 1)
        {
            throw new BankException("Налични са няколко сметки. Моля, изберете от коя сметка да се извърши плащането.");
        }

        return activeAccounts[0];
    }

    private async Task<MoneyTransaction?> FindTransactionByKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        return await dbContext.MoneyTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    private async Task<CreditInstallmentPaymentResultModel> BuildPaymentResultFromTransactionAsync(
        MoneyTransaction transaction,
        CancellationToken cancellationToken)
    {
        var installment = await dbContext.CreditInstallments
            .AsNoTracking()
            .FirstAsync(p => p.Id == transaction.CreditPaymentId!.Value, cancellationToken);

        var credit = await dbContext.Credits
            .AsNoTracking()
            .FirstAsync(c => c.Id == transaction.CreditId!.Value, cancellationToken);

        var account = await dbContext.BankAccounts
            .AsNoTracking()
            .FirstAsync(a => a.Id == transaction.BankAccountId, cancellationToken);

        return new CreditInstallmentPaymentResultModel
        {
            CreditId = credit.Id,
            CreditStatus = credit.Status,
            CreditRepaidAtUtc = credit.RepaidAtUtc,
            Payment = MapPayment(installment),
            AccountId = account.Id,
            AccountIban = account.IBAN,
            NewBalance = transaction.BalanceAfter,
            Transaction = MapTransaction(transaction),
        };
    }

    private async Task<T> ExecuteWithConcurrencyRetryAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation();
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyAttempts)
            {
                // Друго движение е променило салдото между четенето и записа. Изчистваме промените и
                // повтаряме с прясно прочетено салдо — така 100 паралелни тегления не могат да преразходват.
                dbContext.ChangeTracker.Clear();
            }
        }
    }

    private static void EnsureAccountIsActive(BankAccount account)
    {
        if (account.Status != BankAccountStatus.Active)
        {
            throw new BankException("Банковата сметка не е активна.");
        }
    }

    // Същ idempotency ключ, но различен отпечатък = повторно използване на ключа с друго тяло на заявката.
    // Това е конфликт (409), а не честен retry. Празен записан отпечатък = ред отпреди въвеждането на
    // RequestHash; него не блокираме (обратна съвместимост).
    private static void EnsureSameRequest(string storedHash, string incomingHash)
    {
        if (!string.IsNullOrEmpty(storedHash) && storedHash != incomingHash)
        {
            throw new BankException(
                "Същият ключ за идемпотентност вече е използван с различни данни на заявката.", 409);
        }
    }

    // Тънки делегати към споделените мапъри, за да остане единствен източник на истината MoneyOperationMappings
    // и същевременно да работят method-group извикванията (Select(MapTransaction)) по-горе.
    private static AccountOperationResultModel BuildAccountResult(string accountIban, MoneyTransaction transaction)
        => MoneyOperationMappings.BuildAccountResult(accountIban, transaction);

    private static DepositRequestModel MapDepositRequest(DepositRequest depositRequest, string accountIban)
        => MoneyOperationMappings.MapDepositRequest(depositRequest, accountIban);

    private static MoneyTransactionModel MapTransaction(MoneyTransaction transaction)
        => MoneyOperationMappings.MapTransaction(transaction);

    private static CreditPaymentModel MapPayment(CreditInstallment installment)
        => MoneyOperationMappings.MapPayment(installment);

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException
            && sqlException.Number is 2601 or 2627;
    }
}
