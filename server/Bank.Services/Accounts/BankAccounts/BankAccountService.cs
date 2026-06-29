using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Bank.Accounts;
using Bank.Core.JsonModels.Common;
using Bank.DB;
using Bank.DB.Entities;
using Bank.Services.Accounts.Iban;
using Bank.Services.Common;
using Bank.Services.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Bank.Services.Accounts.BankAccounts;

public class BankAccountService : IBankAccountService
{
    private const int GenerateIbanMaxAttempts = 32;
    private const int MaxPageSize = 100;

    private readonly AppDbContext dbContext;
    private readonly IIbanGenerator ibanGenerator;
    private readonly IUserService userService;

    public BankAccountService(AppDbContext dbContext, IIbanGenerator ibanGenerator, IUserService userService)
    {
        this.dbContext = dbContext;
        this.ibanGenerator = ibanGenerator;
        this.userService = userService;
    }

    public async Task<PagedResponse<BankAccountModel>> GetAccountsAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = dbContext.BankAccounts
            .AsNoTracking()
            .Include(ba => ba.Customer).ThenInclude(c => c.Person)
            .Include(ba => ba.Customer).ThenInclude(c => c.Company)
            .AsQueryable();

        var search = request.Search?.Trim().ToLower();
        if (!string.IsNullOrEmpty(search))
        {
            // Търсене по IBAN или по име на клиента (физическо или юридическо лице).
            query = query.Where(ba =>
                ba.IBAN.ToLower().Contains(search)
                || (ba.Customer.Person != null
                    && (ba.Customer.Person.FirstName + " " + ba.Customer.Person.LastName).ToLower().Contains(search))
                || (ba.Customer.Company != null && ba.Customer.Company.Name.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Ограничаваме страницата до наличния брой страници.
        var maxPage = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        if (page > maxPage)
        {
            page = maxPage;
        }

        var accounts = await query
            // Вторичен ключ по Id, за да е страницирането детерминирано при еднакво OpenedAtUtc
            // (иначе един и същ запис може да се появи на две страници или да бъде пропуснат).
            .OrderByDescending(ba => ba.OpenedAtUtc)
            .ThenByDescending(ba => ba.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<BankAccountModel>
        {
            Items = accounts.Select(MapAccount).ToArray(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<BankAccountDetailsModel> GetAccountAsync(long accountId, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.BankAccounts
            .AsNoTracking()
            .Include(ba => ba.Customer).ThenInclude(c => c.Person)
            .Include(ba => ba.Customer).ThenInclude(c => c.Company)
            .FirstOrDefaultAsync(ba => ba.Id == accountId, cancellationToken)
            ?? throw new BankException("Банковата сметка не е намерена.", 404);

        return MapAccountDetails(account);
    }

    public async Task<BankAccountDetailsModel> CreateAccountAsync(CreateBankAccountRequest request, CancellationToken cancellationToken = default)
    {
        var userId = userService.GetRequiredLoggedInUserId();

        if (request.OpeningBalance < 0)
        {
            throw new BankException("Началното салдо не може да е отрицателно.");
        }

        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(person => person.Id == request.CustomerId, cancellationToken)
            ?? throw new BankException("Клиентът не е намерен.", 404);

        var account = new BankAccount
        {
            Balance = decimal.Round(request.OpeningBalance, 2, MidpointRounding.AwayFromZero),
            Status = BankAccountStatus.Active,
            CustomerId = customer.Id,
            OpenedAtUtc = DateTime.UtcNow,
        };

        await PersistWithUniqueIbanAsync(account, userId, cancellationToken);

        return await GetAccountAsync(account.Id, cancellationToken);
    }

    public async Task<BankAccountDetailsModel> CloseAccountAsync(long accountId, CancellationToken cancellationToken = default)
    {
        var userId = userService.GetRequiredLoggedInUserId();

        var account = await dbContext.BankAccounts
            .Include(ba => ba.Customer).ThenInclude(c => c.Person)
            .Include(ba => ba.Customer).ThenInclude(c => c.Company)
            .FirstOrDefaultAsync(ba => ba.Id == accountId, cancellationToken)
            ?? throw new BankException("Банковата сметка не е намерена.", 404);

        if (account.Status == BankAccountStatus.Closed)
        {
            throw new BankException("Банковата сметка вече е закрита.");
        }

        if (account.Balance != 0m)
        {
            throw new BankException("Банкова сметка с ненулево салдо не може да бъде закрита.");
        }

        account.Status = BankAccountStatus.Closed;
        account.ClosedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(userId, cancellationToken);
        return MapAccountDetails(account);
    }

    private static BankAccountModel MapAccount(BankAccount account)
    {
        return new BankAccountModel
        {
            Id = account.Id,
            Iban = account.IBAN,
            Balance = account.Balance,
            Status = account.Status,
            CustomerId = account.CustomerId,
            CustomerDisplayName = CustomerDisplayNameFormatter.BuildDisplayName(account.Customer),
            OpenedAtUtc = account.OpenedAtUtc,
            ClosedAtUtc = account.ClosedAtUtc,
        };
    }

    private static BankAccountDetailsModel MapAccountDetails(BankAccount account)
    {
        return new BankAccountDetailsModel
        {
            Id = account.Id,
            Iban = account.IBAN,
            Balance = account.Balance,
            Status = account.Status,
            CustomerId = account.CustomerId,
            CustomerDisplayName = CustomerDisplayNameFormatter.BuildDisplayName(account.Customer),
            OpenedAtUtc = account.OpenedAtUtc,
            ClosedAtUtc = account.ClosedAtUtc,
        };
    }

    private async Task PersistWithUniqueIbanAsync(BankAccount account, long userId, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < GenerateIbanMaxAttempts; attempt++)
        {
            account.IBAN = await GenerateUniqueIbanAsync(cancellationToken);

            try
            {
                dbContext.BankAccounts.Add(account);
                await dbContext.SaveChangesAsync(userId, cancellationToken);
                return;
            }
            catch (DbUpdateException exception) when (IsDuplicateIbanViolation(exception))
            {
                dbContext.Entry(account).State = EntityState.Detached;
            }
        }

        throw new BankException("Неуспешно генериране на уникален IBAN. Моля, опитайте отново.");
    }

    private static bool IsDuplicateIbanViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException
            && sqlException.Number is 2601 or 2627;
    }

    private async Task<string> GenerateUniqueIbanAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < GenerateIbanMaxAttempts; attempt++)
        {
            var generatedIban = ibanGenerator.Generate();
            var normalizedIban = IbanValidator.Normalize(generatedIban);

            if (!IbanValidator.IsValid(normalizedIban))
            {
                continue;
            }

            var ibanExists = await dbContext.BankAccounts
                .AsNoTracking()
                .AnyAsync(account => account.IBAN == normalizedIban, cancellationToken);

            if (!ibanExists)
            {
                return normalizedIban;
            }
        }

        throw new BankException("Неуспешно генериране на уникален IBAN. Моля, опитайте отново.");
    }
}
