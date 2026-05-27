using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Auth;
using Bank.DB;
using Bank.DB.Constants;
using Bank.DB.Entities;
using Bank.Services.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UserRoleModel = Bank.Core.JsonModels.Auth.UserRole;

namespace Bank.Services.Users;

public class UserService : IUserService
{
    private readonly IHttpContextAccessor contextAccessor;
    private readonly AppDbContext dbContext;
    private readonly UserManager<User> userManager;
    private readonly RoleManager<Role> roleManager;

    private bool userIdRetrieved;
    private long? cachedUserId;

    private bool userIsAdminRetrieved;
    private bool cachedUserIsAdmin;

    private bool userRetrieved;
    private User? cachedUser;

    public UserService(
        IHttpContextAccessor contextAccessor,
        AppDbContext dbContext,
        UserManager<User> userManager,
        RoleManager<Role> roleManager)
    {
        this.contextAccessor = contextAccessor;
        this.dbContext = dbContext;
        this.userManager = userManager;
        this.roleManager = roleManager;
    }

    public long? LoggedInUserId
    {
        get
        {
            if (userIdRetrieved)
            {
                return cachedUserId;
            }

            userIdRetrieved = true;
            var userIdClaim = contextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? contextAccessor.HttpContext?.User.FindFirstValue("sub");

            if (long.TryParse(userIdClaim, out var parsedUserId))
            {
                cachedUserId = parsedUserId;
            }

            return cachedUserId;
        }
    }

    public bool LoggedInUserIsAdmin
    {
        get
        {
            if (userIsAdminRetrieved)
            {
                return cachedUserIsAdmin;
            }

            userIsAdminRetrieved = true;
            cachedUserIsAdmin = contextAccessor.HttpContext?.User.IsInRole(RoleNames.Admin) == true;
            return cachedUserIsAdmin;
        }
    }

    public User? LoggedInUser
    {
        get
        {
            if (userRetrieved)
            {
                return cachedUser;
            }

            userRetrieved = true;
            var userId = LoggedInUserId;

            if (userId.HasValue)
            {
                cachedUser = dbContext.Users
                    .AsNoTracking()
                    .FirstOrDefault(user => user.Id == userId.Value);
            }

            return cachedUser;
        }
    }

    public void InvalidateLoggedInUserCache()
    {
        userIdRetrieved = false;
        cachedUserId = null;
        userIsAdminRetrieved = false;
        cachedUserIsAdmin = false;
        userRetrieved = false;
        cachedUser = null;
    }

    public async Task<UserModel> MapUserAsync(User user, CancellationToken cancellationToken = default)
    {
        var roles = await userManager.GetRolesAsync(user);

        return new UserModel
        {
            Id = user.Id,
            CustomerId = user.CustomerId,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = MapRoles(roles),
        };
    }

    public async Task<UserModel?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var loggedInUser = LoggedInUser;
        if (loggedInUser == null || !loggedInUser.IsActive)
        {
            return null;
        }

        return await MapUserAsync(loggedInUser, cancellationToken);
    }

    public async Task<UserModel> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var loggedInUserId = GetRequiredLoggedInUserId();
        var loggedInUser = await userManager.FindByIdAsync(loggedInUserId.ToString())
            ?? throw new BankException("User was not found.", 404);

        loggedInUser.FirstName = request.FirstName.Trim();
        loggedInUser.LastName = request.LastName.Trim();
        loggedInUser.DateModified = DateTime.UtcNow;

        var result = await userManager.UpdateAsync(loggedInUser);
        if (!result.Succeeded)
        {
            throw new BankException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }

        InvalidateLoggedInUserCache();
        return await MapUserAsync(loggedInUser, cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserAccessModel>> GetUsersForAdministrationAsync(CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users
            .AsNoTracking()
            .Include(user => user.Customer)
            .OrderByDescending(user => user.DateCreated)
            .ToListAsync(cancellationToken);

        var models = new List<UserAccessModel>(users.Count);
        foreach (var user in users)
        {
            models.Add(await MapUserAccessAsync(user, cancellationToken));
        }

        return models;
    }

    public async Task<IReadOnlyCollection<StaffUserGridModel>> GetStaffUsersForManagementAsync(CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users
            .AsNoTracking()
            .Include(user => user.Customer)
            .OrderByDescending(user => user.DateCreated)
            .ToListAsync(cancellationToken);

        var models = new List<StaffUserGridModel>(users.Count);
        foreach (var user in users)
        {
            var roleNames = await userManager.GetRolesAsync(user);
            var roles = MapRoles(roleNames);

            if (!ShouldBeInStaffGrid(user, roles))
            {
                continue;
            }

            models.Add(MapStaffUserGrid(user, roles));
        }

        return models;
    }

    public async Task<UserAccessModel> GetUserForAdministrationAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users
            .AsNoTracking()
            .Include(entity => entity.Customer)
            .FirstOrDefaultAsync(entity => entity.Id == userId, cancellationToken)
            ?? throw new BankException("User was not found.", 404);

        return await MapUserAccessAsync(user, cancellationToken);
    }

    public async Task<UserAccessModel> UpdateUserAccessAsync(long userId, UpdateUserAccessRequest request, CancellationToken cancellationToken = default)
    {
        var loggedInUserId = GetRequiredLoggedInUserId();

        await EnsureRoleAsync(RoleNames.User);
        await EnsureRoleAsync(RoleNames.Staff);
        await EnsureRoleAsync(RoleNames.Admin);

        var user = await userManager.Users
            .Include(entity => entity.Customer)
            .FirstOrDefaultAsync(entity => entity.Id == userId, cancellationToken)
            ?? throw new BankException("User was not found.", 404);

        if (loggedInUserId == userId)
        {
            if (!request.IsAdmin)
            {
                throw new BankException("You cannot remove your own admin access.");
            }

            if (!request.IsActive)
            {
                throw new BankException("You cannot deactivate your own account.");
            }
        }

        var roles = await userManager.GetRolesAsync(user);
        var hasAdminRole = roles.Any(role => string.Equals(role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase));
        var removingAdminAccess = hasAdminRole && !request.IsAdmin;
        var deactivatingAdmin = hasAdminRole && !request.IsActive && user.IsActive;

        if ((removingAdminAccess || deactivatingAdmin) && !await HasAnotherActiveAdminAsync(user.Id, cancellationToken))
        {
            throw new BankException("At least one active admin account is required.");
        }

        var hasChanges = false;
        var roleSet = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase);

        hasChanges = await ApplyRoleAsync(user, roleSet, RoleNames.User, shouldHaveRole: true) || hasChanges;
        hasChanges = await ApplyRoleAsync(user, roleSet, RoleNames.Staff, request.IsStaff) || hasChanges;
        hasChanges = await ApplyRoleAsync(user, roleSet, RoleNames.Admin, request.IsAdmin) || hasChanges;

        if (user.IsActive != request.IsActive)
        {
            user.IsActive = request.IsActive;
            user.DateModified = DateTime.UtcNow;
            EnsureIdentityResult(await userManager.UpdateAsync(user));
            hasChanges = true;
        }

        if (hasChanges)
        {
            user = await userManager.Users
                .AsNoTracking()
                .Include(entity => entity.Customer)
                .FirstOrDefaultAsync(entity => entity.Id == user.Id, cancellationToken)
                ?? throw new BankException("User was not found.", 404);
        }

        return await MapUserAccessAsync(user, cancellationToken);
    }

    public async Task<UserAccessModel> LinkUserToCustomerAsync(long userId, long customerId, CancellationToken cancellationToken = default)
    {
        _ = GetRequiredLoggedInUserId();

        await EnsureRoleAsync(RoleNames.Customer);

        var user = await userManager.Users
            .Include(entity => entity.Customer)
            .FirstOrDefaultAsync(entity => entity.Id == userId, cancellationToken)
            ?? throw new BankException("User was not found.", 404);

        var customer = await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == customerId, cancellationToken)
            ?? throw new BankException("Customer was not found.", 404);

        if (user.CustomerId.HasValue && user.CustomerId.Value != customerId)
        {
            throw new BankException("User is already connected to another customer.");
        }

        var customerAlreadyConnectedToAnotherUser = await userManager.Users
            .AsNoTracking()
            .AnyAsync(entity => entity.Id != userId && entity.CustomerId == customerId, cancellationToken);

        if (customerAlreadyConnectedToAnotherUser)
        {
            throw new BankException("Customer is already connected to another user.");
        }

        if (!user.CustomerId.HasValue)
        {
            user.CustomerId = customer.Id;
            user.DateModified = DateTime.UtcNow;
            EnsureIdentityResult(await userManager.UpdateAsync(user));
        }

        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Any(role => string.Equals(role, RoleNames.Customer, StringComparison.OrdinalIgnoreCase)))
        {
            EnsureIdentityResult(await userManager.AddToRoleAsync(user, RoleNames.Customer));
        }

        var updatedUser = await userManager.Users
            .AsNoTracking()
            .Include(entity => entity.Customer)
            .FirstOrDefaultAsync(entity => entity.Id == userId, cancellationToken)
            ?? throw new BankException("User was not found.", 404);

        return await MapUserAccessAsync(updatedUser, cancellationToken);
    }

    private async Task<UserAccessModel> MapUserAccessAsync(User user, CancellationToken cancellationToken = default)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserAccessModel
        {
            Id = user.Id,
            CustomerId = user.CustomerId,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CustomerDisplayName = user.Customer == null ? null : CustomerDisplayNameFormatter.BuildDisplayName(user.Customer),
            IsActive = user.IsActive,
            Roles = MapRoles(roles),
        };
    }

    private static StaffUserGridModel MapStaffUserGrid(User user, IReadOnlyCollection<UserRoleModel> roles)
    {
        return new StaffUserGridModel
        {
            Id = user.Id,
            CustomerId = user.CustomerId,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CustomerDisplayName = user.Customer == null ? null : CustomerDisplayNameFormatter.BuildDisplayName(user.Customer),
            IsActive = user.IsActive,
            Roles = roles,
        };
    }

    private static bool ShouldBeInStaffGrid(User user, IReadOnlyCollection<UserRoleModel> roles)
    {
        var hasAdminRole = roles.Contains(UserRoleModel.Admin);
        var hasStaffRole = roles.Contains(UserRoleModel.Staff);
        var hasCustomerRole = roles.Contains(UserRoleModel.Customer);
        var hasCustomerLink = user.CustomerId.HasValue;

        return !hasAdminRole && !hasStaffRole && (hasCustomerRole || hasCustomerLink);
    }

    private static IReadOnlyCollection<UserRoleModel> MapRoles(IEnumerable<string> roles)
    {
        return roles
            .Select(MapRole)
            .Distinct()
            .OrderBy(role => (int)role)
            .ToArray();
    }

    private static UserRoleModel MapRole(string role)
    {
        if (string.Equals(role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return UserRoleModel.Admin;
        }

        if (string.Equals(role, RoleNames.Staff, StringComparison.OrdinalIgnoreCase))
        {
            return UserRoleModel.Staff;
        }

        if (string.Equals(role, RoleNames.Customer, StringComparison.OrdinalIgnoreCase))
        {
            return UserRoleModel.Customer;
        }

        return UserRoleModel.User;
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

    public long GetRequiredLoggedInUserId()
    {
        return LoggedInUserId ?? throw new BankException("User is not authenticated.", 401);
    }

    private async Task EnsureRoleAsync(string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        EnsureIdentityResult(await roleManager.CreateAsync(new Role
        {
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
        }));
    }

    private static void EnsureIdentityResult(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new BankException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }
}
