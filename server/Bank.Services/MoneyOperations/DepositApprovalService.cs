using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Bank.MoneyOperations;
using Bank.Core.JsonModels.Common;
using Bank.DB;
using Bank.DB.Entities;
using Bank.Services.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Bank.Services.MoneyOperations;

public class DepositApprovalService : IDepositApprovalService
{
    private const int MaxConcurrencyAttempts = 5;
    private const int MaxPageSize = 100;

    private readonly AppDbContext dbContext;
    private readonly IUserService userService;
    private readonly IAccountLedger accountLedger;

    public DepositApprovalService(AppDbContext dbContext, IUserService userService, IAccountLedger accountLedger)
    {
        this.dbContext = dbContext;
        this.userService = userService;
        this.accountLedger = accountLedger;
    }

    public async Task<PagedResponse<DepositRequestQueueModel>> GetDepositRequestsAsync(
        DepositRequestStatus? status,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = dbContext.DepositRequests
            .AsNoTracking()
            .Include(d => d.BankAccount)
                .ThenInclude(a => a.Customer)
                    .ThenInclude(c => c.Person)
            .Include(d => d.BankAccount)
                .ThenInclude(a => a.Customer)
                    .ThenInclude(c => c.Company)
            .AsSplitQuery()
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        var search = request.Search?.Trim().ToLower();
        if (!string.IsNullOrEmpty(search))
        {
            // Търсене по IBAN на сметката или по име на клиента (физическо или юридическо лице). Сравнението е
            // без оглед на регистъра — ToLower се превежда до SQL LOWER и работи еднакво и при InMemory тестовете.
            // Превежда се в SQL, затова филтрирането и страницирането се случват в базата, а не в паметта.
            query = query.Where(d =>
                d.BankAccount.IBAN.ToLower().Contains(search)
                || (d.BankAccount.Customer.Person != null
                    && (d.BankAccount.Customer.Person.FirstName + " " + d.BankAccount.Customer.Person.LastName).ToLower().Contains(search))
                || (d.BankAccount.Customer.Company != null && d.BankAccount.Customer.Company.Name.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Ограничаваме страницата до наличния диапазон, за да не препълни int32 изчислението на отместването
        // (Skip) при огромна стойност за Page и да не се стигне до отрицателен OFFSET в SQL.
        var maxPage = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        if (page > maxPage)
        {
            page = maxPage;
        }

        var requests = await query
            // Вторичен ключ по Id, за да е страницирането детерминирано при еднакво DateCreated
            // (иначе един и същ запис може да се появи на две страници или да бъде пропуснат).
            .OrderByDescending(d => d.DateCreated)
            .ThenByDescending(d => d.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<DepositRequestQueueModel>
        {
            Items = requests.Select(MoneyOperationMappings.MapDepositRequestQueue).ToArray(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<AccountOperationResultModel> ApproveAsync(long depositRequestId, CancellationToken cancellationToken = default)
    {
        var staffUserId = userService.GetRequiredLoggedInUserId();

        // Детерминиран ключ: повторно одобрение на същата заявка не може да кредитира салдото втори път.
        var idempotencyKey = $"deposit-approval:{depositRequestId}";

        var existing = await dbContext.MoneyTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing != null)
        {
            return await BuildExistingDepositResultAsync(existing, cancellationToken);
        }

        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var request = await dbContext.DepositRequests
                .FirstOrDefaultAsync(d => d.Id == depositRequestId, cancellationToken)
                ?? throw new BankException("Заявката за депозит не е намерена.", 404);

            if (request.Status == DepositRequestStatus.Approved)
            {
                var duplicate = await dbContext.MoneyTransactions
                    .AsNoTracking()
                    .FirstAsync(t => t.IdempotencyKey == idempotencyKey, cancellationToken);
                return await BuildExistingDepositResultAsync(duplicate, cancellationToken);
            }

            if (request.Status != DepositRequestStatus.Pending)
            {
                throw new BankException("Само чакащи заявки за депозит могат да бъдат одобрени.");
            }

            var account = await dbContext.BankAccounts
                .FirstAsync(a => a.Id == request.BankAccountId, cancellationToken);

            if (account.Status != BankAccountStatus.Active)
            {
                throw new BankException("Банковата сметка не е активна.");
            }

            var transaction = accountLedger.Record(new LedgerEntry
            {
                Account = account,
                Type = MoneyTransactionType.Deposit,
                Amount = request.Amount,
                IdempotencyKey = idempotencyKey,
                DepositRequestId = request.Id,
            });

            request.Status = DepositRequestStatus.Approved;
            request.ReviewedById = staffUserId;
            request.ReviewedAtUtc = DateTime.UtcNow;

            try
            {
                await dbContext.SaveChangesAsync(staffUserId, cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                dbContext.ChangeTracker.Clear();
                var duplicate = await dbContext.MoneyTransactions
                    .AsNoTracking()
                    .FirstAsync(t => t.IdempotencyKey == idempotencyKey, cancellationToken);
                return MoneyOperationMappings.BuildAccountResult(account.IBAN, duplicate);
            }

            return MoneyOperationMappings.BuildAccountResult(account.IBAN, transaction);
        }, cancellationToken);
    }

    public async Task<DepositRequestQueueModel> RejectAsync(
        long depositRequestId,
        DepositRejectRequest request,
        CancellationToken cancellationToken = default)
    {
        var staffUserId = userService.GetRequiredLoggedInUserId();
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        return await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var depositRequest = await dbContext.DepositRequests
                .Include(d => d.BankAccount)
                    .ThenInclude(a => a.Customer)
                        .ThenInclude(c => c.Person)
                .Include(d => d.BankAccount)
                    .ThenInclude(a => a.Customer)
                        .ThenInclude(c => c.Company)
                .AsSplitQuery()
                .FirstOrDefaultAsync(d => d.Id == depositRequestId, cancellationToken)
                ?? throw new BankException("Заявката за депозит не е намерена.", 404);

            if (depositRequest.Status != DepositRequestStatus.Pending)
            {
                throw new BankException("Само чакащи заявки за депозит могат да бъдат отхвърлени.");
            }

            depositRequest.Status = DepositRequestStatus.Rejected;
            depositRequest.ReviewNote = note;
            depositRequest.ReviewedById = staffUserId;
            depositRequest.ReviewedAtUtc = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(staffUserId, cancellationToken);

            return MoneyOperationMappings.MapDepositRequestQueue(depositRequest);
        }, cancellationToken);
    }

    private async Task<AccountOperationResultModel> BuildExistingDepositResultAsync(
        MoneyTransaction transaction,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.BankAccounts
            .AsNoTracking()
            .FirstAsync(a => a.Id == transaction.BankAccountId, cancellationToken);

        return MoneyOperationMappings.BuildAccountResult(account.IBAN, transaction);
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
                dbContext.ChangeTracker.Clear();
            }
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException
            && sqlException.Number is 2601 or 2627;
    }
}
