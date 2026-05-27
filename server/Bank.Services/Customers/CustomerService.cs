using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Bank.Customers;
using Bank.DB;
using Bank.DB.Entities;
using Bank.Services.Common;
using Bank.Services.Credits;
using Bank.Services.Users;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Bank.Services.Customers;

public class CustomerService : ICustomerService
{
    private static readonly Regex PersonalIdentifierRegex = new(@"^\d{10}$", RegexOptions.Compiled);
    private static readonly Regex CompanyIdentifierRegex = new(@"^(\d{9}|\d{13})$", RegexOptions.Compiled);

    private readonly AppDbContext dbContext;
    private readonly IUserService userService;
    private readonly ICreditRepricingService creditRepricingService;

    public CustomerService(
        AppDbContext dbContext,
        IUserService userService,
        ICreditRepricingService creditRepricingService)
    {
        this.dbContext = dbContext;
        this.userService = userService;
        this.creditRepricingService = creditRepricingService;
    }

    public async Task<IReadOnlyCollection<CustomerModel>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        var customers = await dbContext.Customers
            .AsNoTracking()
            .OrderByDescending(customer => customer.DateCreated)
            .ToListAsync(cancellationToken);

        return customers.Select(MapCustomer).ToArray();
    }

    public async Task<IReadOnlyCollection<CustomerLookupModel>> GetCustomerLookupAsync(CancellationToken cancellationToken = default)
    {
        var customers = await dbContext.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.CustomerType)
            .ThenBy(customer => customer.CompanyName)
            .ThenBy(customer => customer.LastName)
            .ThenBy(customer => customer.FirstName)
            .ToListAsync(cancellationToken);

        return customers.Select(customer => new CustomerLookupModel
        {
            Id = customer.Id,
            CustomerType = customer.CustomerType,
            IsVip = customer.IsVip,
            DisplayName = CustomerDisplayNameFormatter.BuildDisplayName(customer),
        }).ToArray();
    }

    public async Task<CustomerDetailsModel> GetCustomerAsync(long customerId, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers
            .AsNoTracking()
            .Include(entity => entity.Accounts)
            .Include(entity => entity.Credits)
            .ThenInclude(credit => credit.CreditTypeCondition)
            .FirstOrDefaultAsync(entity => entity.Id == customerId, cancellationToken)
            ?? throw new BankException("Customer was not found.", 404);

        return MapCustomerDetails(customer);
    }

    public async Task<CustomerDetailsModel> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var userId = userService.GetRequiredLoggedInUserId();
        var customer = await CreateCustomerEntityAsync(request, userId, cancellationToken);
        return await GetCustomerAsync(customer.Id, cancellationToken);
    }

    public async Task<CustomerDetailsModel> CreateCustomerForUserAsync(long userId, CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var loggedInUserId = userService.GetRequiredLoggedInUserId();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var customer = await CreateCustomerEntityAsync(request, loggedInUserId, cancellationToken);

        await userService.LinkUserToCustomerAsync(userId, customer.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetCustomerAsync(customer.Id, cancellationToken);
    }

    private async Task<Customer> CreateCustomerEntityAsync(CreateCustomerRequest request, long userId, CancellationToken cancellationToken)
    {
        var customer = new Customer();
        ApplyCustomerRequest(customer, request.CustomerType, request.FirstName, request.LastName, request.PersonalIdentifier, request.CompanyName, request.CompanyIdentifier, request.RepresentativeName);
        await ValidateIdentifiersAsync(customer, null, cancellationToken);

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(userId, cancellationToken);

        return customer;
    }

    public async Task<CustomerDetailsModel> UpdateCustomerAsync(long customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var userId = userService.GetRequiredLoggedInUserId();

        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(entity => entity.Id == customerId, cancellationToken)
            ?? throw new BankException("Customer was not found.", 404);

        ApplyCustomerRequest(customer, request.CustomerType, request.FirstName, request.LastName, request.PersonalIdentifier, request.CompanyName, request.CompanyIdentifier, request.RepresentativeName);
        await ValidateIdentifiersAsync(customer, customerId, cancellationToken);

        await dbContext.SaveChangesAsync(userId, cancellationToken);
        return await GetCustomerAsync(customer.Id, cancellationToken);
    }

    public async Task<CustomerDetailsModel> UpdateVipAsync(long customerId, UpdateCustomerVipRequest request, CancellationToken cancellationToken = default)
    {
        var userId = userService.GetRequiredLoggedInUserId();

        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(entity => entity.Id == customerId, cancellationToken)
            ?? throw new BankException("Customer was not found.", 404);

        if (customer.IsVip == request.IsVip)
        {
            return await GetCustomerAsync(customerId, cancellationToken);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        customer.IsVip = request.IsVip;
        await dbContext.SaveChangesAsync(userId, cancellationToken);
        await creditRepricingService.RepriceActiveCreditsForCustomerAsync(customer.Id, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return await GetCustomerAsync(customer.Id, cancellationToken);
    }

    private static void ApplyCustomerRequest(
        Customer customer,
        CustomerType customerType,
        string? firstName,
        string? lastName,
        string? personalIdentifier,
        string? companyName,
        string? companyIdentifier,
        string? representativeName)
    {
        var normalizedFirstName = NormalizeNullable(firstName);
        var normalizedLastName = NormalizeNullable(lastName);
        var normalizedPersonalIdentifier = NormalizeNullable(personalIdentifier);
        var normalizedCompanyName = NormalizeNullable(companyName);
        var normalizedCompanyIdentifier = NormalizeNullable(companyIdentifier);
        var normalizedRepresentativeName = NormalizeNullable(representativeName);

        customer.CustomerType = customerType;

        if (customerType == CustomerType.Individual)
        {
            if (string.IsNullOrWhiteSpace(normalizedFirstName) || string.IsNullOrWhiteSpace(normalizedLastName) || string.IsNullOrWhiteSpace(normalizedPersonalIdentifier))
            {
                throw new BankException("Individual customer requires first name, last name, and personal identifier.");
            }

            if (!string.IsNullOrWhiteSpace(normalizedCompanyName)
                || !string.IsNullOrWhiteSpace(normalizedCompanyIdentifier)
                || !string.IsNullOrWhiteSpace(normalizedRepresentativeName))
            {
                throw new BankException("Company fields must be empty for individual customer.");
            }

            if (!PersonalIdentifierRegex.IsMatch(normalizedPersonalIdentifier))
            {
                throw new BankException("Personal identifier must contain exactly 10 digits.");
            }

            if (!BulgarianIdentifierValidator.IsValidEgn(normalizedPersonalIdentifier))
            {
                throw new BankException("Personal identifier must be a valid EGN.");
            }

            customer.FirstName = normalizedFirstName;
            customer.LastName = normalizedLastName;
            customer.PersonalIdentifier = normalizedPersonalIdentifier;
            customer.CompanyName = null;
            customer.CompanyIdentifier = null;
            customer.RepresentativeName = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(normalizedCompanyName)
            || string.IsNullOrWhiteSpace(normalizedCompanyIdentifier)
            || string.IsNullOrWhiteSpace(normalizedRepresentativeName))
        {
            throw new BankException("Company customer requires company name, company identifier, and representative.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedFirstName)
            || !string.IsNullOrWhiteSpace(normalizedLastName)
            || !string.IsNullOrWhiteSpace(normalizedPersonalIdentifier))
        {
            throw new BankException("Personal fields must be empty for company customer.");
        }

        if (!CompanyIdentifierRegex.IsMatch(normalizedCompanyIdentifier))
        {
            throw new BankException("Company identifier must contain exactly 9 or 13 digits.");
        }

        if (!BulgarianIdentifierValidator.IsValidEik(normalizedCompanyIdentifier))
        {
            throw new BankException("Company identifier must be a valid EIK.");
        }

        customer.FirstName = null;
        customer.LastName = null;
        customer.PersonalIdentifier = null;
        customer.CompanyName = normalizedCompanyName;
        customer.CompanyIdentifier = normalizedCompanyIdentifier;
        customer.RepresentativeName = normalizedRepresentativeName;
    }

    private async Task ValidateIdentifiersAsync(Customer customer, long? excludedCustomerId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(customer.PersonalIdentifier))
        {
            var duplicatePersonalIdentifier = await dbContext.Customers
                .AnyAsync(entity =>
                    entity.PersonalIdentifier == customer.PersonalIdentifier
                    && (!excludedCustomerId.HasValue || entity.Id != excludedCustomerId.Value), cancellationToken);

            if (duplicatePersonalIdentifier)
            {
                throw new BankException("Personal identifier already exists.");
            }
        }

        if (!string.IsNullOrWhiteSpace(customer.CompanyIdentifier))
        {
            var duplicateCompanyIdentifier = await dbContext.Customers
                .AnyAsync(entity =>
                    entity.CompanyIdentifier == customer.CompanyIdentifier
                    && (!excludedCustomerId.HasValue || entity.Id != excludedCustomerId.Value), cancellationToken);

            if (duplicateCompanyIdentifier)
            {
                throw new BankException("Company identifier already exists.");
            }
        }
    }

    private static CustomerModel MapCustomer(Customer customer)
    {
        return new CustomerModel
        {
            Id = customer.Id,
            CustomerType = customer.CustomerType,
            IsVip = customer.IsVip,
            DisplayName = CustomerDisplayNameFormatter.BuildDisplayName(customer),
            Identifier = CustomerDisplayNameFormatter.BuildIdentifier(customer),
        };
    }

    private static CustomerDetailsModel MapCustomerDetails(Customer customer)
    {
        return new CustomerDetailsModel
        {
            Id = customer.Id,
            CustomerType = customer.CustomerType,
            IsVip = customer.IsVip,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            PersonalIdentifier = customer.PersonalIdentifier,
            CompanyName = customer.CompanyName,
            CompanyIdentifier = customer.CompanyIdentifier,
            RepresentativeName = customer.RepresentativeName,
            Accounts = customer.Accounts
                .OrderByDescending(account => account.OpenedAtUtc)
                .Select(account => new CustomerAccountSummaryModel
                {
                    Id = account.Id,
                    Iban = account.IBAN,
                    Balance = account.Balance,
                    Status = account.Status,
                    OpenedAtUtc = account.OpenedAtUtc,
                    ClosedAtUtc = account.ClosedAtUtc,
                })
                .ToArray(),
            Credits = customer.Credits
                .OrderByDescending(credit => credit.GrantedAtUtc)
                .Select(credit => new CustomerCreditSummaryModel
                {
                    Id = credit.Id,
                    CreditType = credit.CreditTypeCondition.CreditType,
                    GrantedAmount = credit.GrantedAmount,
                    TermMonths = credit.TermMonths,
                    AppliedAnnualInterestRate = credit.AppliedAnnualInterestRate,
                    PlannedMonthlyPaymentAmount = credit.PlannedMonthlyPaymentAmount,
                    Status = credit.Status,
                    GrantedAtUtc = credit.GrantedAtUtc,
                    RepaidAtUtc = credit.RepaidAtUtc,
                })
                .ToArray(),
        };
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
