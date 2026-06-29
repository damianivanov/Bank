using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Auth;
using Bank.Core.JsonModels.Common;
using Bank.DB;
using Bank.DB.Constants;
using Bank.DB.Entities;
using Bank.Services.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserRoleModel = Bank.Core.JsonModels.Auth.UserRole;

namespace Bank.Services.Users.Administration;

public class UserAdministrationService : IUserAdministrationService
{
    private const int MaxPageSize = 100;

    private readonly AppDbContext dbContext;
    private readonly UserManager<User> userManager;
    private readonly IUserService userService;

    public UserAdministrationService(
        AppDbContext dbContext,
        UserManager<User> userManager,
        IUserService userService)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
        this.userService = userService;
    }

    public async Task<UserAccessPageModel> GetUsersForAdministrationAsync(PagedRequest request, IReadOnlyCollection<UserRoleModel> roles, bool? isActive, CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var baseQuery = userManager.Users
            .AsNoTracking()
            .Include(u => u.Person)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

        var totalUsers = await baseQuery.CountAsync(cancellationToken);
        var admins = await baseQuery.CountAsync(u => u.UserRoles.Any(ur => ur.Role.Name == RoleNames.Admin), cancellationToken);
        var staff = await baseQuery.CountAsync(u => u.UserRoles.Any(ur => ur.Role.Name == RoleNames.Staff), cancellationToken);
        var customers = await baseQuery.CountAsync(u => u.PersonId != null || u.UserRoles.Any(ur => ur.Role.Name == RoleNames.Customer), cancellationToken);
        var active = await baseQuery.CountAsync(u => u.IsActive, cancellationToken);

        var summary = new UserAccessSummaryModel
        {
            TotalUsers = totalUsers,
            Admins = admins,
            Staff = staff,
            Customers = customers,
            Active = active,
            Inactive = totalUsers - active,
        };

        var filtered = baseQuery.AsQueryable();

        var wantAdmin = roles.Contains(UserRoleModel.Admin);
        var wantStaff = roles.Contains(UserRoleModel.Staff);
        var wantCustomer = roles.Contains(UserRoleModel.Customer);
        if (wantAdmin || wantStaff || wantCustomer)
        {
            filtered = filtered.Where(u =>
                (wantAdmin && u.UserRoles.Any(ur => ur.Role.Name == RoleNames.Admin))
                || (wantStaff && u.UserRoles.Any(ur => ur.Role.Name == RoleNames.Staff))
                || (wantCustomer && (u.PersonId != null || u.UserRoles.Any(ur => ur.Role.Name == RoleNames.Customer))));
        }

        var search = request.Search?.Trim().ToLower();
        if (!string.IsNullOrEmpty(search))
        {
            // Търсене по имейл, име/фамилия на акаунта или име на свързаното лице. ToLower се превежда
            // до SQL LOWER и работи еднакво и при InMemory тестовете; цялата заявка остава в базата.
            filtered = filtered.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(search))
                || (u.FirstName != null && u.FirstName.ToLower().Contains(search))
                || (u.LastName != null && u.LastName.ToLower().Contains(search))
                || (u.Person != null && (u.Person.FirstName + " " + u.Person.LastName).ToLower().Contains(search)));
        }

        if (isActive.HasValue)
        {
            filtered = filtered.Where(u => u.IsActive == isActive.Value);
        }

        var totalCount = await filtered.CountAsync(cancellationToken);

        var maxPage = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        if (page > maxPage)
        {
            page = maxPage;
        }

        var users = await filtered
            .OrderByDescending(u => u.DateCreated)
            .ThenByDescending(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new UserAccessPageModel
        {
            Items = users.Select(u =>
            {
                var (firstName, lastName) = UserNameResolver.Resolve(u);
                return new UserAccessModel
                {
                    Id = u.Id,
                    PersonId = u.PersonId,
                    Email = u.Email ?? string.Empty,
                    FirstName = firstName,
                    LastName = lastName,
                    PersonDisplayName = u.Person == null ? null : CustomerDisplayNameFormatter.BuildPersonName(u.Person),
                    IsActive = u.IsActive,
                    Roles = UserRoleMapper.MapRoles(u.UserRoles.Select(ur => ur.Role.Name)),
                };
            }).ToArray(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Summary = summary,
        };
    }

    public async Task<StaffUserPageModel> GetRegularUsersAsync(PagedRequest request, bool? linked, bool? isActive, CancellationToken cancellationToken = default)
    {
        // Показваме само обикновените потребители (без администратори и служители).
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var regularBase = userManager.Users
            .AsNoTracking()
            .Include(u => u.Person)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => !u.UserRoles.Any(ur => ur.Role.Name == RoleNames.Admin || ur.Role.Name == RoleNames.Staff));

        var total = await regularBase.CountAsync(cancellationToken);
        var linkedCount = await regularBase.CountAsync(u => u.PersonId != null, cancellationToken);
        var activeCount = await regularBase.CountAsync(u => u.IsActive, cancellationToken);

        var summary = new StaffUserSummaryModel
        {
            Total = total,
            Linked = linkedCount,
            MissingCustomer = total - linkedCount,
            Active = activeCount,
            Inactive = total - activeCount,
        };

        var filtered = regularBase.AsQueryable();

        var search = request.Search?.Trim().ToLower();
        if (!string.IsNullOrEmpty(search))
        {
            // Търсене по имейл, име/фамилия на акаунта или име на свързаното лице.
            filtered = filtered.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(search))
                || (u.FirstName != null && u.FirstName.ToLower().Contains(search))
                || (u.LastName != null && u.LastName.ToLower().Contains(search))
                || (u.Person != null && (u.Person.FirstName + " " + u.Person.LastName).ToLower().Contains(search)));
        }

        if (linked.HasValue)
        {
            filtered = filtered.Where(u => (u.PersonId != null) == linked.Value);
        }

        if (isActive.HasValue)
        {
            filtered = filtered.Where(u => u.IsActive == isActive.Value);
        }

        var totalCount = await filtered.CountAsync(cancellationToken);

        var maxPage = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        if (page > maxPage)
        {
            page = maxPage;
        }

        var users = await filtered
            .OrderByDescending(u => u.DateCreated)
            .ThenByDescending(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new StaffUserPageModel
        {
            // Ролите се четат от вече включената навигация UserRoles (без асинхронна заявка на ред).
            Items = users.Select(u =>
            {
                var (firstName, lastName) = UserNameResolver.Resolve(u);
                return new StaffUserGridModel
                {
                    Id = u.Id,
                    PersonId = u.PersonId,
                    Email = u.Email ?? string.Empty,
                    FirstName = firstName,
                    LastName = lastName,
                    PersonDisplayName = u.Person == null ? null : CustomerDisplayNameFormatter.BuildPersonName(u.Person),
                    IsActive = u.IsActive,
                    Roles = UserRoleMapper.MapRoles(u.UserRoles.Select(ur => ur.Role.Name)),
                };
            }).ToArray(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Summary = summary,
        };
    }
    
    public async Task<UserAccessModel> GetUserForAdministrationAsync(long userId, CancellationToken cancellationToken = default)
    {
        // Взимаме потребител с включено свързано лице (ако има такова) и ролите му. Ако няма такова, Person ще е null.
        var user = await userManager.Users
            .AsNoTracking()
            .Include(user => user.Person)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new BankException("Потребителят не е намерен.", 404);

        return await MapUserAccessAsync(user, cancellationToken);
    }

    public async Task<UserAccessModel> UpdateUserAccessAsync(long userId, UpdateUserAccessRequest request, CancellationToken cancellationToken = default)
    {
        var loggedInUserId = userService.GetRequiredLoggedInUserId();

        var user = await userManager.Users
            .Include(user => user.Person)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new BankException("Потребителят не е намерен.", 404);

        if (loggedInUserId == userId)
        {
            if (!request.IsAdmin)
            {
                throw new BankException("Не можете да премахнете собствените си администраторски права.");
            }

            if (!request.IsActive)
            {
                throw new BankException("Не можете да деактивирате собствения си акаунт.");
            }
        }

        var roles = await userManager.GetRolesAsync(user);
        var hasAdminRole = roles.Any(role => string.Equals(role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase));
        var removingAdminAccess = hasAdminRole && !request.IsAdmin;
        var deactivatingAdmin = hasAdminRole && !request.IsActive && user.IsActive;

        if ((removingAdminAccess || deactivatingAdmin) && !await HasAnotherActiveAdminAsync(user.Id, cancellationToken))
        {
            throw new BankException("Изисква се поне един активен администраторски акаунт.");
        }

        var roleSet = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase);
        var isDeactivating = user.IsActive && !request.IsActive;

        // Предварително: ще има ли изобщо промяна? Така не обезсилваме сесии при "save" без промяна.
        var willChange =
            !roleSet.Contains(RoleNames.User)
            || roleSet.Contains(RoleNames.Staff) != request.IsStaff
            || roleSet.Contains(RoleNames.Admin) != request.IsAdmin
            || user.IsActive != request.IsActive;

        if (willChange)
        {
            // Fail-closed: бутаме security stamp-а ПЪРВО. Ролевите операции и UpdateAsync са отделни
            // SaveChanges-и; ако някоя следваща стъпка се провали по средата, искаме да остане
            // "сесиите обезсилени" (re-auth с актуалните права от базата), а НЕ "сменена роля в базата +
            // още валиден стар токен със старите права" (fail-open). Бутнатият stamp валидира всички
            // издадени access токени при следващата заявка (виж JwtSecurityStampValidator).
            EnsureIdentityResult(await userManager.UpdateSecurityStampAsync(user));
        }

        await ApplyRoleAsync(user, roleSet, RoleNames.User, shouldHaveRole: true);
        await ApplyRoleAsync(user, roleSet, RoleNames.Staff, request.IsStaff);
        await ApplyRoleAsync(user, roleSet, RoleNames.Admin, request.IsAdmin);

        if (user.IsActive != request.IsActive)
        {
            user.IsActive = request.IsActive;
            user.DateModified = DateTime.UtcNow;
            EnsureIdentityResult(await userManager.UpdateAsync(user));
        }

        if (willChange)
        {
            user = await userManager.Users
                .AsNoTracking()
                .Include(user => user.Person)
                .FirstOrDefaultAsync(existingUser => existingUser.Id == user.Id, cancellationToken)
                ?? throw new BankException("Потребителят не е намерен.", 404);
        }

        if (isDeactivating)
        {
            // Деактивираният не бива да може да си извади нов токен -> отнемаме и refresh токените (hard logout).
            await RevokeRefreshTokensAsync(user.Id, cancellationToken);
        }

        return await MapUserAccessAsync(user, cancellationToken);
    }

    // Създава логин акаунт за клиент на гише. Без свързване с лице тук — то става отделно
    // (LinkUserToPersonAsync) след като party записите са създадени. Паролата (= ЕГН) и
    // mustChangePassword идват от извикващия.
    public async Task<long> CreateCounterUserAsync(string email, string password, bool mustChangePassword, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim();

        var existing = await userManager.FindByEmailAsync(normalizedEmail);
        if (existing != null)
        {
            throw new BankException("Вече съществува потребител с този имейл.");
        }

        var user = new User
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            IsActive = true,
            MustChangePassword = mustChangePassword,
            DateCreated = DateTime.UtcNow,
        };

        EnsureIdentityResult(await userManager.CreateAsync(user, password));
        EnsureIdentityResult(await userManager.AddToRoleAsync(user, RoleNames.User));

        return user.Id;
    }

    public async Task<UserAccessModel> LinkUserToPersonAsync(long userId, long personId, CancellationToken cancellationToken = default)
    {
        _ = userService.GetRequiredLoggedInUserId();

        var user = await userManager.Users
            .Include(user => user.Person)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new BankException("Потребителят не е намерен.", 404);

        // Администратор/служител не може да бъде свързан с клиентско лице — служебните и клиентските роли се изключват.
        var existingRoles = await userManager.GetRolesAsync(user);
        if (existingRoles.Any(role =>
                string.Equals(role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, RoleNames.Staff, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BankException("Администратор или служител не може да бъде свързан с клиентски акаунт.");
        }

        var person = await dbContext.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(person => person.Id == personId, cancellationToken)
            ?? throw new BankException("Лицето не е намерено.", 404);

        if (user.PersonId.HasValue && user.PersonId.Value != personId)
        {
            throw new BankException("Потребителят вече е свързан с друго лице.");
        }

        var personAlreadyConnectedToAnotherUser = await userManager.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id != userId && user.PersonId == personId, cancellationToken);

        if (personAlreadyConnectedToAnotherUser)
        {
            throw new BankException("Лицето вече е свързано с друг потребител.");
        }

        var accessChanged = false;

        if (!user.PersonId.HasValue)
        {
            user.PersonId = person.Id;
            // Името вече идва от лицето (единствен източник) — изчистваме копието на акаунта, за да не
            // остане втори източник на истина, който да се разминава след промяна на данните на лицето.
            user.FirstName = null;
            user.LastName = null;
            user.DateModified = DateTime.UtcNow;
            EnsureIdentityResult(await userManager.UpdateAsync(user));
            accessChanged = true;
        }

        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Any(role => string.Equals(role, RoleNames.Customer, StringComparison.OrdinalIgnoreCase)))
        {
            EnsureIdentityResult(await userManager.AddToRoleAsync(user, RoleNames.Customer));
            accessChanged = true;
        }

        if (accessChanged)
        {
            // Новата Customer роля и person_id claim трябва да влязат в сила веднага -> бутаме stamp-а,
            // за да форсираме refresh с обновен токен (иначе клиентът остава без правата до изтичане).
            EnsureIdentityResult(await userManager.UpdateSecurityStampAsync(user));
        }

        var updatedUser = await userManager.Users
            .AsNoTracking()
            .Include(user => user.Person)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new BankException("Потребителят не е намерен.", 404);

        return await MapUserAccessAsync(updatedUser, cancellationToken);
    }

    private async Task<UserAccessModel> MapUserAccessAsync(User user, CancellationToken cancellationToken = default)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (firstName, lastName) = UserNameResolver.Resolve(user);
        return new UserAccessModel
        {
            Id = user.Id,
            PersonId = user.PersonId,
            Email = user.Email ?? string.Empty,
            FirstName = firstName,
            LastName = lastName,
            PersonDisplayName = user.Person == null ? null : CustomerDisplayNameFormatter.BuildPersonName(user.Person),
            IsActive = user.IsActive,
            Roles = UserRoleMapper.MapRoles(roles),
        };
    }

    private async Task<bool> ApplyRoleAsync(User user, HashSet<string> roleSet, string roleName, bool shouldHaveRole)
    {
        var hasRole = roleSet.Contains(roleName);
        if (shouldHaveRole && !hasRole)
        {
            EnsureIdentityResult(await userManager.AddToRoleAsync(user, roleName));
            roleSet.Add(roleName);
            return true;
        }

        if (!shouldHaveRole && hasRole)
        {
            EnsureIdentityResult(await userManager.RemoveFromRoleAsync(user, roleName));
            roleSet.Remove(roleName);
            return true;
        }

        return false;
    }

    private async Task<bool> HasAnotherActiveAdminAsync(long excludedUserId, CancellationToken cancellationToken)
    {
        return await userManager.Users
            .Where(user => user.Id != excludedUserId && user.IsActive)
            .AnyAsync(user => user.UserRoles.Any(userRole => userRole.Role.Name == RoleNames.Admin), cancellationToken);
    }

    private async Task RevokeRefreshTokensAsync(long userId, CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        if (activeTokens.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureIdentityResult(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new BankException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }
}
