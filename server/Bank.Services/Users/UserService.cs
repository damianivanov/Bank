using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Auth;
using Bank.DB;
using Bank.DB.Constants;
using Bank.DB.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bank.Services.Users;

public class UserService : IUserService
{
    private readonly IHttpContextAccessor contextAccessor;
    private readonly AppDbContext dbContext;
    private readonly UserManager<User> userManager;

    private bool userIdRetrieved;
    private long? cachedUserId;

    private bool personIdRetrieved;
    private long? cachedPersonId;

    private bool userIsAdminRetrieved;
    private bool cachedUserIsAdmin;

    private bool userRetrieved;
    private User? cachedUser;

    public UserService(
        IHttpContextAccessor contextAccessor,
        AppDbContext dbContext,
        UserManager<User> userManager)
    {
        this.contextAccessor = contextAccessor;
        this.dbContext = dbContext;
        this.userManager = userManager;
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

    public long? LoggedInPersonId
    {
        get
        {
            if (personIdRetrieved)
            {
                return cachedPersonId;
            }

            personIdRetrieved = true;
            var personIdClaim = contextAccessor.HttpContext?.User.FindFirstValue(ClaimNames.PersonId);

            if (long.TryParse(personIdClaim, out var parsedPersonId))
            {
                cachedPersonId = parsedPersonId;
            }

            return cachedPersonId;
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
            cachedUserIsAdmin = contextAccessor.HttpContext?.User.IsInRole(RoleNames.Admin) ?? false;
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

    public long GetRequiredLoggedInUserId()
    {
        return LoggedInUserId ?? throw new BankException("Потребителят не е удостоверен.", 401);
    }

    public long GetRequiredLoggedInPersonId()
    {
        return LoggedInPersonId ?? throw new BankException("Липсва клиентски контекст.", 401);
    }

    public void InvalidateLoggedInUserCache()
    {
        userIdRetrieved = false;
        cachedUserId = null;
        personIdRetrieved = false;
        cachedPersonId = null;
        userIsAdminRetrieved = false;
        cachedUserIsAdmin = false;
        userRetrieved = false;
        cachedUser = null;
    }

    public async Task<UserModel> MapUserAsync(User user, CancellationToken cancellationToken = default)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (firstName, lastName) = await ResolveUserNameAsync(user, cancellationToken);

        return new UserModel
        {
            Id = user.Id,
            PersonId = user.PersonId,
            Email = user.Email ?? string.Empty,
            FirstName = firstName,
            LastName = lastName,
            MustChangePassword = user.MustChangePassword,
            Roles = UserRoleMapper.MapRoles(roles),
        };
    }

    // Свързан акаунт черпи името от лицето (единствен източник). Person навигацията обикновено не е
    // заредена тук (потребителят идва от FindBy.../LoggedInUser), затова я дозареждаме при нужда.
    private async Task<(string? FirstName, string? LastName)> ResolveUserNameAsync(User user, CancellationToken cancellationToken)
    {
        if (!user.PersonId.HasValue)
        {
            return (user.FirstName, user.LastName);
        }

        var person = user.Person ?? await dbContext.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == user.PersonId.Value, cancellationToken);

        return person != null
            ? (person.FirstName, person.LastName)
            : (user.FirstName, user.LastName);
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
            ?? throw new BankException("Потребителят не е намерен.", 404);

        // Името на свързан клиент е част от данните на лицето (единствен източник, KYC) и се управлява
        // от банката — не се променя през профила. Затова пипаме името само за несвързани акаунти
        // (служители и още нерегистрирани като клиент), при които то живее върху самия акаунт.
        if (!loggedInUser.PersonId.HasValue)
        {
            loggedInUser.FirstName = request.FirstName.Trim();
            loggedInUser.LastName = request.LastName.Trim();
            loggedInUser.DateModified = DateTime.UtcNow;

            var result = await userManager.UpdateAsync(loggedInUser);
            if (!result.Succeeded)
            {
                throw new BankException(string.Join(" ", result.Errors.Select(error => error.Description)));
            }

            InvalidateLoggedInUserCache();
        }

        return await MapUserAsync(loggedInUser, cancellationToken);
    }
}
