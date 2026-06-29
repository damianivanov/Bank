using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Bank.Customers;
using Bank.Core.JsonModels.Common;
using Bank.DB;
using Bank.DB.Entities;
using Bank.Services.Common;
using Bank.Services.Credits;
using Bank.Services.Users;
using Bank.Services.Users.Administration;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Bank.Services.Customers;

public class CustomerService : ICustomerService
{
    private const int MaxPageSize = 100;

    private static readonly Regex PersonalIdentifierRegex = new(@"^\d{10}$", RegexOptions.Compiled);
    private static readonly Regex CompanyIdentifierRegex = new(@"^(\d{9}|\d{13})$", RegexOptions.Compiled);

    private readonly AppDbContext dbContext;
    private readonly IUserService userService;
    private readonly IUserAdministrationService userAdministrationService;
    private readonly ICreditRepricingService creditRepricingService;

    public CustomerService(
        AppDbContext dbContext,
        IUserService userService,
        IUserAdministrationService userAdministrationService,
        ICreditRepricingService creditRepricingService)
    {
        this.dbContext = dbContext;
        this.userService = userService;
        this.userAdministrationService = userAdministrationService;
        this.creditRepricingService = creditRepricingService;
    }

    public async Task<PagedResponse<CustomerModel>> GetCustomersAsync(PagedRequest request, CustomerType? customerType = null, CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = dbContext.Customers
            .AsNoTracking()
            .Include(customer => customer.Person)
            .Include(customer => customer.Company)
            .AsSplitQuery()
            .AsQueryable();

        if (customerType.HasValue)
        {
            query = query.Where(customer => customer.CustomerType == customerType.Value);
        }

        var search = request.Search?.Trim().ToLower();
        if (!string.IsNullOrEmpty(search))
        {
            // Търсене по име на физическо лице, ЕГН, име на фирма или ЕИК.
            query = query.Where(customer =>
                (customer.Person != null
                    && ((customer.Person.FirstName + " " + customer.Person.LastName).ToLower().Contains(search)
                        || customer.Person.Egn.ToLower().Contains(search)))
                || (customer.Company != null
                    && (customer.Company.Name.ToLower().Contains(search)
                        || customer.Company.Eik.ToLower().Contains(search))));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Ограничаваме страницата до наличния диапазон, за да не препълни int32 изчислението на отместването
        // (Skip) при огромна стойност за Page и да не се стигне до отрицателен OFFSET в SQL.
        var maxPage = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        if (page > maxPage)
        {
            page = maxPage;
        }

        var customers = await query
            // Вторичен ключ по Id, за да е страницирането детерминирано при еднакво DateCreated
            // (иначе един и същ запис може да се появи на две страници или да бъде пропуснат).
            .OrderByDescending(customer => customer.DateCreated)
            .ThenByDescending(customer => customer.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<CustomerModel>
        {
            Items = customers.Select(MapCustomer).ToArray(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<IReadOnlyCollection<CustomerLookupModel>> GetCustomerLookupAsync(string? search, CancellationToken cancellationToken = default)
    {
        const int CustomerLookupResultCap = 50;

        var query = dbContext.Customers
            .AsNoTracking()
            .Include(customer => customer.Person)
            .Include(customer => customer.Company)
            .AsQueryable();

        var term = search?.Trim().ToLower();
        if (!string.IsNullOrEmpty(term))
        {
            // търсене по име на физическо лице, ЕГН, име на фирма или ЕИК.
            query = query.Where(customer =>
                (customer.Person != null
                    && ((customer.Person.FirstName + " " + customer.Person.LastName).ToLower().Contains(term)
                        || customer.Person.Egn.ToLower().Contains(term)))
                || (customer.Company != null
                    && (customer.Company.Name.ToLower().Contains(term)
                        || customer.Company.Eik.ToLower().Contains(term))));
        }

        // Падащото меню не е пълен списък. Подреден по последно създадени.
        var customers = await query
            .OrderByDescending(customer => customer.DateCreated)
            .Take(CustomerLookupResultCap)
            .ToListAsync(cancellationToken);

        return customers
            .OrderBy(customer => customer.CustomerType)
            .ThenBy(CustomerDisplayNameFormatter.BuildDisplayName)
            .Select(customer => new CustomerLookupModel
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
            .Include(customer => customer.Person)
            .Include(customer => customer.Company)
                .ThenInclude(company => company!.Representatives)
                .ThenInclude(representative => representative.Person)
            .Include(customer => customer.Accounts)
            .Include(customer => customer.Credits)
                .ThenInclude(credit => credit.CreditTypeCondition)
            .AsSplitQuery()
            .FirstOrDefaultAsync(customer => customer.Id == customerId, cancellationToken)
            ?? throw new BankException("Клиентът не е намерен.", 404);

        return MapCustomerDetails(customer);
    }

    public async Task<CustomerEditModel> GetCustomerForEditAsync(long customerId, CancellationToken cancellationToken = default)
    {
        // По-лека заявка за формата за редакция — без сметки и кредити, защото формата не ги ползва.
        var customer = await dbContext.Customers
            .AsNoTracking()
            .Include(customer => customer.Person)
            .Include(customer => customer.Company)
                .ThenInclude(company => company!.Representatives)
                .ThenInclude(representative => representative.Person)
            .AsSplitQuery()
            .FirstOrDefaultAsync(customer => customer.Id == customerId, cancellationToken)
            ?? throw new BankException("Клиентът не е намерен.", 404);

        return MapCustomerForEdit(customer);
    }

    public async Task<IReadOnlyCollection<long>> GetAccessibleCustomerIdsAsync(long personId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await dbContext.Customers
            .AsNoTracking()
            .Where(customer => customer.PersonId == personId
                || (customer.CompanyId != null && customer.Company!.Representatives.Any(representative =>
                    representative.PersonId == personId
                    && (representative.ValidFrom == null || representative.ValidFrom <= today)
                    && (representative.ValidTo == null || representative.ValidTo >= today))))
            .OrderByDescending(customer => customer.PersonId == personId)
            .ThenBy(customer => customer.Id)
            .Select(customer => customer.Id)
            .ToListAsync(cancellationToken);
    }

    // Леки резюмета на всички клиенти, до които лицето има достъп (собствено физическо лице + фирми,
    // които представлява активно) — за превключвателя в /my-banking. Подредбата е като при
    // GetAccessibleCustomerIdsAsync: собственото физическо лице първо (подразбиращ се избор).
    public async Task<IReadOnlyCollection<CustomerLookupModel>> GetAccessibleCustomersAsync(long personId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var customers = await dbContext.Customers
            .AsNoTracking()
            .Include(c => c.Person)
            .Include(c => c.Company)
            .Where(c => c.PersonId == personId
                || (c.CompanyId != null && c.Company!.Representatives.Any(r =>
                    r.PersonId == personId
                    && (r.ValidFrom == null || r.ValidFrom <= today)
                    && (r.ValidTo == null || r.ValidTo >= today))))
            .OrderByDescending(c => c.PersonId == personId)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

        return customers
            .Select(c => new CustomerLookupModel
            {
                Id = c.Id,
                CustomerType = c.CustomerType,
                IsVip = c.IsVip,
                DisplayName = CustomerDisplayNameFormatter.BuildDisplayName(c),
            })
            .ToArray();
    }

    public async Task<CustomerModel> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var userId = userService.GetRequiredLoggedInUserId();
        var customer = await CreateCustomerEntityAsync(request, userId, cancellationToken);
        return MapCustomer(customer);
    }

    public async Task<CustomerModel> CreateCustomerForUserAsync(long userId, CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var loggedInUserId = userService.GetRequiredLoggedInUserId();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var customer = await CreateCustomerEntityAsync(request, loggedInUserId, cancellationToken);
        var personId = await ResolvePersonIdForLoginAsync(customer, cancellationToken);
        await userAdministrationService.LinkUserToPersonAsync(userId, personId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return MapCustomer(customer);
    }

    public async Task<CustomerModel> RegisterCounterCustomerAsync(RegisterCounterCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var loggedInUserId = userService.GetRequiredLoggedInUserId();

        // Валидираме идентификаторите ПРЕДИ да създадем логин акаунта. Иначе невалиден ЕГН (но валидна
        // като парола стойност) би създал потребител, който после се "отвързва" само през rollback на
        // транзакцията (а InMemory няма транзакции). Ранното спиране дава и смислено съобщение.
        ValidateCounterIdentifiers(request);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // 1. Логин акаунтът се създава пръв — при зает имейл (честа грешка на касата) спираме рано,
        // преди да пипаме party данните. Парола = ЕГН, със задължителна смяна при първо влизане.
        var userId = await userAdministrationService.CreateCounterUserAsync(request.Email, request.Egn, mustChangePassword: true, cancellationToken);

        // 2. Преизползваме съществуващата party + customer логика (валидира ЕГН/ЕИК, намира-или-създава Person/Company).
        var customer = await CreateCustomerEntityAsync(BuildCounterCustomerRequest(request), loggedInUserId, cancellationToken);
        var personId = await ResolvePersonIdForLoginAsync(customer, cancellationToken);
        await userAdministrationService.LinkUserToPersonAsync(userId, personId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return MapCustomer(customer);
    }

    // Огледало на проверките в ApplyCustomerPartyAsync, но изпълнени преди създаването на логина,
    // за да не остане orphan акаунт при невалиден идентификатор.
    private static void ValidateCounterIdentifiers(RegisterCounterCustomerRequest request)
    {
        var egn = request.Egn?.Trim() ?? string.Empty;
        if (!PersonalIdentifierRegex.IsMatch(egn))
        {
            throw new BankException("ЕГН трябва да съдържа точно 10 цифри.");
        }

        if (!BulgarianIdentifierValidator.IsValidEgn(egn))
        {
            throw new BankException("ЕГН трябва да е валиден ЕГН.");
        }

        if (request.CustomerType != CustomerType.Company)
        {
            return;
        }

        var eik = request.CompanyIdentifier?.Trim() ?? string.Empty;
        if (!CompanyIdentifierRegex.IsMatch(eik))
        {
            throw new BankException("Идентификаторът на фирмата трябва да съдържа точно 9 или 13 цифри.");
        }

        if (!BulgarianIdentifierValidator.IsValidEik(eik))
        {
            throw new BankException("Идентификаторът на фирмата трябва да е валиден ЕИК.");
        }
    }

    // Превръща гише-заявката в стандартна CreateCustomerRequest. За юридическо лице носещият логина
    // става ЕДИНСТВЕНИЯТ представител (по дизайн: на гише се добавя само първият потребител).
    private static CreateCustomerRequest BuildCounterCustomerRequest(RegisterCounterCustomerRequest request)
    {
        if (request.CustomerType == CustomerType.Individual)
        {
            return new CreateCustomerRequest
            {
                CustomerType = CustomerType.Individual,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PersonalIdentifier = request.Egn,
            };
        }

        return new CreateCustomerRequest
        {
            CustomerType = CustomerType.Company,
            CompanyName = request.CompanyName,
            CompanyIdentifier = request.CompanyIdentifier,
            Representatives =
            [
                new CustomerRepresentativeRequest
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Egn = request.Egn,
                    Role = request.RepresentativeRole ?? RepresentativeRole.Manager,
                    ValidFrom = request.ValidFrom,
                    ValidTo = request.ValidTo,
                },
            ],
        };
    }

    private async Task<Customer> CreateCustomerEntityAsync(CreateCustomerRequest request, long userId, CancellationToken cancellationToken)
    {
        var customer = new Customer();
        await ApplyCustomerPartyAsync(
            customer,
            request.CustomerType,
            request.FirstName,
            request.LastName,
            request.PersonalIdentifier,
            request.CompanyName,
            request.CompanyIdentifier,
            request.Representatives,
            excludedCustomerId: null,
            cancellationToken);

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(userId, cancellationToken);

        return customer;
    }

    public async Task<CustomerModel> UpdateCustomerAsync(long customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var userId = userService.GetRequiredLoggedInUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken)
            ?? throw new BankException("Клиентът не е намерен.", 404);

        // Заснемаме кои лица този клиент прави достъпни ПРЕДИ редакцията, за да разпознаем кои "изпадат".
        var previouslyExposedPersonIds = await GetPersonIdsExposedByCustomerAsync(customerId, today, cancellationToken);

        await ApplyCustomerPartyAsync(
            customer,
            request.CustomerType,
            request.FirstName,
            request.LastName,
            request.PersonalIdentifier,
            request.CompanyName,
            request.CompanyIdentifier,
            request.Representatives,
            excludedCustomerId: customerId,
            cancellationToken);

        await EnsureLinkedLoginsKeepAccessAsync(customer, customerId, previouslyExposedPersonIds, today, cancellationToken);

        await dbContext.SaveChangesAsync(userId, cancellationToken);
        return MapCustomer(customer);
    }

    private static bool IsRepresentativeActiveOn(CompanyRepresentative representative, DateOnly today)
        => (representative.ValidFrom == null || representative.ValidFrom <= today)
            && (representative.ValidTo == null || representative.ValidTo >= today);

    // Лицата, които даден клиент прави достъпни днес: при физическо лице — самото лице; при
    // юридическо — лицата на представителите с активен мандат (заснето от базата, преди редакция).
    private async Task<HashSet<long>> GetPersonIdsExposedByCustomerAsync(long customerId, DateOnly today, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers
            .AsNoTracking()
            .Include(c => c.Company)
                .ThenInclude(c => c!.Representatives)
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

        var exposed = new HashSet<long>();
        if (customer == null)
        {
            return exposed;
        }

        if (customer.PersonId.HasValue)
        {
            exposed.Add(customer.PersonId.Value);
        }

        if (customer.Company != null)
        {
            foreach (var representative in customer.Company.Representatives.Where(r => IsRepresentativeActiveOn(r, today)))
            {
                exposed.Add(representative.PersonId);
            }
        }

        return exposed;
    }

    // Същото изчисление, но върху редактираната (но още незаписана) инстанция в паметта.
    private static HashSet<long> GetPersonIdsExposedByCustomerInMemory(Customer customer, DateOnly today)
    {
        var exposed = new HashSet<long>();

        if (customer.CustomerType == CustomerType.Individual)
        {
            var personId = customer.Person?.Id ?? customer.PersonId;
            if (personId is > 0)
            {
                exposed.Add(personId.Value);
            }
        }
        else if (customer.Company != null)
        {
            foreach (var representative in customer.Company.Representatives.Where(r => IsRepresentativeActiveOn(r, today)))
            {
                var personId = representative.Person?.Id ?? representative.PersonId;
                if (personId > 0)
                {
                    exposed.Add(personId);
                }
            }
        }

        return exposed;
    }

    // Fail-closed: редакция на клиент не бива да остави логин акаунт, свързан с лице, което вече няма
    // достъп до нито един клиент. Ако такова лице "изпадне" от този клиент, не е достъпно през друг и
    // към него има закачен акаунт — блокираме (админът първо трябва да премахне връзката на акаунта).
    private async Task EnsureLinkedLoginsKeepAccessAsync(
        Customer customer,
        long customerId,
        IReadOnlyCollection<long> previouslyExposedPersonIds,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (previouslyExposedPersonIds.Count == 0)
        {
            return;
        }

        var stillExposedPersonIds = GetPersonIdsExposedByCustomerInMemory(customer, today);
        var releasedPersonIds = previouslyExposedPersonIds.Where(personId => !stillExposedPersonIds.Contains(personId)).ToList();
        if (releasedPersonIds.Count == 0)
        {
            return;
        }

        var releasedPersonIdsWithLogin = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.PersonId != null && releasedPersonIds.Contains(u.PersonId.Value))
            .Select(u => u.PersonId!.Value)
            .ToListAsync(cancellationToken);

        foreach (var personId in releasedPersonIdsWithLogin)
        {
            var accessibleViaAnotherCustomer = await IsPersonExposedByOtherCustomerAsync(personId, customerId, today, cancellationToken);
            if (!accessibleViaAnotherCustomer)
            {
                throw new BankException(
                    "Това лице е свързано с потребителски акаунт. Първо премахнете връзката на акаунта, преди да го отделите от клиента.");
            }
        }
    }

    // Дали лицето остава достъпно през ДРУГ клиент (различен от редактирания) — като физическо лице
    // или като активен представител. Огледало на GetAccessibleCustomerIdsAsync, без текущия клиент.
    private Task<bool> IsPersonExposedByOtherCustomerAsync(long personId, long excludedCustomerId, DateOnly today, CancellationToken cancellationToken)
    {
        return dbContext.Customers
            .AsNoTracking()
            .AnyAsync(c => c.Id != excludedCustomerId
                && (c.PersonId == personId
                    || (c.CompanyId != null && c.Company!.Representatives.Any(r =>
                        r.PersonId == personId
                        && (r.ValidFrom == null || r.ValidFrom <= today)
                        && (r.ValidTo == null || r.ValidTo >= today)))),
                cancellationToken);
    }

    public async Task<CustomerDetailsModel> UpdateVipAsync(long customerId, UpdateCustomerVipRequest request, CancellationToken cancellationToken = default)
    {
        var userId = userService.GetRequiredLoggedInUserId();

        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(customer => customer.Id == customerId, cancellationToken)
            ?? throw new BankException("Клиентът не е намерен.", 404);

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

    private async Task ApplyCustomerPartyAsync(
        Customer customer,
        CustomerType customerType,
        string? firstName,
        string? lastName,
        string? personalIdentifier,
        string? companyName,
        string? companyIdentifier,
        IReadOnlyCollection<CustomerRepresentativeRequest>? representatives,
        long? excludedCustomerId,
        CancellationToken cancellationToken)
    {
        var normalizedFirstName = NormalizeNullable(firstName);
        var normalizedLastName = NormalizeNullable(lastName);
        var normalizedPersonalIdentifier = NormalizeNullable(personalIdentifier);
        var normalizedCompanyName = NormalizeNullable(companyName);
        var normalizedCompanyIdentifier = NormalizeNullable(companyIdentifier);

        customer.CustomerType = customerType;

        if (customerType == CustomerType.Individual)
        {
            if (string.IsNullOrWhiteSpace(normalizedFirstName) || string.IsNullOrWhiteSpace(normalizedLastName) || string.IsNullOrWhiteSpace(normalizedPersonalIdentifier))
            {
                throw new BankException("За физическо лице са необходими име, фамилия и личен идентификатор.");
            }

            if (!string.IsNullOrWhiteSpace(normalizedCompanyName)
                || !string.IsNullOrWhiteSpace(normalizedCompanyIdentifier)
                || (representatives != null && representatives.Count > 0))
            {
                throw new BankException("Полетата за фирма трябва да са празни за физическо лице.");
            }

            if (!PersonalIdentifierRegex.IsMatch(normalizedPersonalIdentifier))
            {
                throw new BankException("Личният идентификатор трябва да съдържа точно 10 цифри.");
            }

            if (!BulgarianIdentifierValidator.IsValidEgn(normalizedPersonalIdentifier))
            {
                throw new BankException("Личният идентификатор трябва да е валиден ЕГН.");
            }

            var person = await GetOrCreatePersonAsync(normalizedFirstName, normalizedLastName, normalizedPersonalIdentifier, cancellationToken);
            await EnsurePersonNotAlreadyIndividualCustomerAsync(person, excludedCustomerId, cancellationToken);

            customer.Person = person;
            customer.PersonId = person.Id == 0 ? null : person.Id;
            customer.Company = null;
            customer.CompanyId = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(normalizedCompanyName) || string.IsNullOrWhiteSpace(normalizedCompanyIdentifier))
        {
            throw new BankException("За юридическо лице са необходими име на фирма и идентификатор на фирма.");
        }

        if (representatives == null || representatives.Count == 0)
        {
            throw new BankException("За юридическо лице е необходим поне един представител.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedFirstName)
            || !string.IsNullOrWhiteSpace(normalizedLastName)
            || !string.IsNullOrWhiteSpace(normalizedPersonalIdentifier))
        {
            throw new BankException("Личните полета трябва да са празни за юридическо лице.");
        }

        if (!CompanyIdentifierRegex.IsMatch(normalizedCompanyIdentifier))
        {
            throw new BankException("Идентификаторът на фирмата трябва да съдържа точно 9 или 13 цифри.");
        }

        if (!BulgarianIdentifierValidator.IsValidEik(normalizedCompanyIdentifier))
        {
            throw new BankException("Идентификаторът на фирмата трябва да е валиден ЕИК.");
        }

        var company = await GetOrCreateCompanyAsync(normalizedCompanyName, normalizedCompanyIdentifier, cancellationToken);
        await EnsureCompanyNotAlreadyCustomerAsync(company, excludedCustomerId, cancellationToken);

        customer.Company = company;
        customer.CompanyId = company.Id == 0 ? null : company.Id;
        customer.Person = null;
        customer.PersonId = null;

        await ApplyRepresentativesAsync(company, representatives, cancellationToken);
    }

    private async Task<Person> GetOrCreatePersonAsync(string firstName, string lastName, string egn, CancellationToken cancellationToken)
    {
        var person = dbContext.Persons.Local.FirstOrDefault(existingPerson => existingPerson.Egn == egn)
            ?? await dbContext.Persons.FirstOrDefaultAsync(existingPerson => existingPerson.Egn == egn, cancellationToken);
        if (person != null)
        {
            person.FirstName = firstName;
            person.LastName = lastName;
            return person;
        }

        person = new Person { FirstName = firstName, LastName = lastName, Egn = egn };
        dbContext.Persons.Add(person);
        return person;
    }

    private async Task<Company> GetOrCreateCompanyAsync(string name, string eik, CancellationToken cancellationToken)
    {
        var company = dbContext.Companies.Local.FirstOrDefault(existingCompany => existingCompany.Eik == eik)
            ?? await dbContext.Companies
                .Include(existingCompany => existingCompany.Representatives)
                .FirstOrDefaultAsync(existingCompany => existingCompany.Eik == eik, cancellationToken);

        if (company != null)
        {
            company.Name = name;
            return company;
        }

        company = new Company { Name = name, Eik = eik };
        dbContext.Companies.Add(company);
        return company;
    }

    private async Task EnsurePersonNotAlreadyIndividualCustomerAsync(Person person, long? excludedCustomerId, CancellationToken cancellationToken)
    {
        if (person.Id == 0)
        {
            return;
        }

        var alreadyCustomer = await dbContext.Customers.AnyAsync(customer =>
            customer.PersonId == person.Id
            && (!excludedCustomerId.HasValue || customer.Id != excludedCustomerId.Value), cancellationToken);

        if (alreadyCustomer)
        {
            throw new BankException("Вече съществува клиент с този личен идентификатор.");
        }
    }

    private async Task EnsureCompanyNotAlreadyCustomerAsync(Company company, long? excludedCustomerId, CancellationToken cancellationToken)
    {
        if (company.Id == 0)
        {
            return;
        }

        var alreadyCustomer = await dbContext.Customers.AnyAsync(customer =>
            customer.CompanyId == company.Id
            && (!excludedCustomerId.HasValue || customer.Id != excludedCustomerId.Value), cancellationToken);

        if (alreadyCustomer)
        {
            throw new BankException("Вече съществува клиент с този идентификатор на фирма.");
        }
    }

    private async Task ApplyRepresentativesAsync(Company company, IReadOnlyCollection<CustomerRepresentativeRequest> representatives, CancellationToken cancellationToken)
    {
        var resolved = new List<(Person Person, CustomerRepresentativeRequest Request)>();
        var seenKeys = new HashSet<(string Egn, RepresentativeRole Role)>();

        foreach (var representative in representatives)
        {
            var firstName = NormalizeNullable(representative.FirstName);
            var lastName = NormalizeNullable(representative.LastName);
            var egn = NormalizeNullable(representative.Egn);

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(egn))
            {
                throw new BankException("За всеки представител са необходими име, фамилия и ЕГН.");
            }

            if (!PersonalIdentifierRegex.IsMatch(egn))
            {
                throw new BankException("ЕГН на представителя трябва да съдържа точно 10 цифри.");
            }

            if (!BulgarianIdentifierValidator.IsValidEgn(egn))
            {
                throw new BankException("ЕГН на представителя трябва да е валиден ЕГН.");
            }

            if (!seenKeys.Add((egn, representative.Role)))
            {
                throw new BankException("Един представител не може да бъде посочен два пъти с една и съща роля.");
            }

            var person = await GetOrCreatePersonAsync(firstName, lastName, egn, cancellationToken);
            resolved.Add((person, representative));
        }

        var existing = company.Representatives.ToList();

        foreach (var existingRepresentative in existing)
        {
            var stillRequested = resolved.Any(resolvedRepresentative =>
                resolvedRepresentative.Person.Id != 0
                && resolvedRepresentative.Person.Id == existingRepresentative.PersonId
                && resolvedRepresentative.Request.Role == existingRepresentative.Role);

            if (!stillRequested)
            {
                dbContext.CompanyRepresentatives.Remove(existingRepresentative);
                company.Representatives.Remove(existingRepresentative);
            }
        }

        foreach (var (person, request) in resolved)
        {
            var match = existing.FirstOrDefault(representative =>
                person.Id != 0 && representative.PersonId == person.Id && representative.Role == request.Role);

            if (match != null)
            {
                match.ValidFrom = request.ValidFrom;
                match.ValidTo = request.ValidTo;
                continue;
            }

            company.Representatives.Add(new CompanyRepresentative
            {
                Person = person,
                Role = request.Role,
                ValidFrom = request.ValidFrom,
                ValidTo = request.ValidTo,
            });
        }
    }

    private async Task<long> ResolvePersonIdForLoginAsync(Customer customer, CancellationToken cancellationToken)
    {
        if (customer.CustomerType == CustomerType.Individual)
        {
            return customer.PersonId ?? throw new BankException("Клиентът няма свързано лице.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeRepresentativePersonIds = await dbContext.CompanyRepresentatives
            .Where(r => r.CompanyId == customer.CompanyId
                && (r.ValidFrom == null || r.ValidFrom <= today)
                && (r.ValidTo == null || r.ValidTo >= today))
            .Select(r => r.PersonId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (activeRepresentativePersonIds.Count == 0)
        {
            throw new BankException("Юридическото лице няма активен представител, към когото да се свърже акаунтът.");
        }

        if (activeRepresentativePersonIds.Count > 1)
        {
            throw new BankException("Юридическото лице има няколко активни представители; посочете към кого да се свърже акаунтът.");
        }

        return activeRepresentativePersonIds[0];
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
            FirstName = customer.Person?.FirstName,
            LastName = customer.Person?.LastName,
            PersonalIdentifier = customer.Person?.Egn,
            CompanyName = customer.Company?.Name,
            CompanyIdentifier = customer.Company?.Eik,
            Representatives = MapRepresentatives(customer),
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

    private static CustomerEditModel MapCustomerForEdit(Customer customer)
    {
        return new CustomerEditModel
        {
            Id = customer.Id,
            CustomerType = customer.CustomerType,
            FirstName = customer.Person?.FirstName,
            LastName = customer.Person?.LastName,
            PersonalIdentifier = customer.Person?.Egn,
            CompanyName = customer.Company?.Name,
            CompanyIdentifier = customer.Company?.Eik,
            Representatives = MapRepresentatives(customer),
        };
    }

    private static CompanyRepresentativeModel[] MapRepresentatives(Customer customer)
    {
        return customer.Company == null
            ? []
            : customer.Company.Representatives
                .OrderBy(representative => representative.Person.LastName)
                .ThenBy(representative => representative.Person.FirstName)
                .Select(representative => new CompanyRepresentativeModel
                {
                    PersonId = representative.PersonId,
                    FirstName = representative.Person.FirstName,
                    LastName = representative.Person.LastName,
                    Egn = representative.Person.Egn,
                    Role = representative.Role,
                    ValidFrom = representative.ValidFrom,
                    ValidTo = representative.ValidTo,
                })
                .ToArray();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
