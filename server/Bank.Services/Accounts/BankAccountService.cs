using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Bank.Accounts;
using Bank.DB;
using Bank.DB.Entities;
using Bank.Services.Common;
using Bank.Services.Users;
using Microsoft.EntityFrameworkCore;

namespace Bank.Services.Accounts;

public class BankAccountService : IBankAccountService
{
    private const int GenerateIbanMaxAttempts = 32;

    private readonly AppDbContext dbContext;
    private readonly IIbanGenerator ibanGenerator;
    private readonly IUserService userService;

    public BankAccountService(AppDbContext dbContext, IIbanGenerator ibanGenerator, IUserService userService)
    {
        this.dbContext = dbContext;
        this.ibanGenerator = ibanGenerator;
        this.userService = userService;
    }

    public async Task<IReadOnlyCollection<BankAccountModel>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await dbContext.BankAccounts
            .AsNoTracking()
            .Include(account => account.Customer)
            .OrderByDescending(account => account.OpenedAtUtc)
            .ToListAsync(cancellationToken);

        return accounts.Select(MapAccount).ToArray();
    }

    public async Task<BankAccountDetailsModel> GetAccountAsync(long accountId, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.BankAccounts
            .AsNoTracking()
            .Include(entity => entity.Customer)
            .FirstOrDefaultAsync(entity => entity.Id == accountId, cancellationToken)
            ?? throw new BankException("Bank account was not found.", 404);

        return MapAccountDetails(account);
    }

    public async Task<BankAccountDetailsModel> CreateAccountAsync(CreateBankAccountRequest request, CancellationToken cancellationToken = default)
    {
        var userId = userService.GetRequiredLoggedInUserId();

        if (request.OpeningBalance < 0)
        {
            throw new BankException("Opening balance cannot be negative.");
        }

        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(entity => entity.Id == request.CustomerId, cancellationToken)
            ?? throw new BankException("Customer was not found.", 404);

        var generatedIban = await GenerateUniqueIbanAsync(cancellationToken);

        var account = new BankAccount
        {
            IBAN = generatedIban,
            Balance = decimal.Round(request.OpeningBalance, 2, MidpointRounding.AwayFromZero),
            Status = BankAccountStatus.Active,
            CustomerId = customer.Id,
            OpenedAtUtc = DateTime.UtcNow,
        };

        dbContext.BankAccounts.Add(account);
        await dbContext.SaveChangesAsync(userId, cancellationToken);

        return await GetAccountAsync(account.Id, cancellationToken);
    }

    public async Task<BankAccountDetailsModel> CloseAccountAsync(long accountId, CancellationToken cancellationToken = default)
    {
        var userId = userService.GetRequiredLoggedInUserId();

        var account = await dbContext.BankAccounts
            .Include(entity => entity.Customer)
            .FirstOrDefaultAsync(entity => entity.Id == accountId, cancellationToken)
            ?? throw new BankException("Bank account was not found.", 404);

        if (account.Status == BankAccountStatus.Closed)
        {
            throw new BankException("Bank account is already closed.");
        }

        if (account.Balance != 0m)
        {
            throw new BankException("Bank account with non-zero balance cannot be closed.");
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
                .AnyAsync(entity => entity.IBAN == normalizedIban, cancellationToken);

            if (!ibanExists)
            {
                return normalizedIban;
            }
        }

        throw new BankException("Could not generate a unique IBAN. Please try again.");
    }
}
