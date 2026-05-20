using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Auth;
using Bank.DB.Constants;
using Bank.DB.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using UserRoleModel = Bank.Core.JsonModels.Auth.UserRole;

namespace Bank.Services.Users;

public class UserService : IUserService
{
    private readonly UserManager<User> userManager;

    public UserService(UserManager<User> userManager)
    {
        this.userManager = userManager;
    }

    public async Task<UserModel> MapUserAsync(User user, CancellationToken cancellationToken = default)
    {
        var roles = await userManager.GetRolesAsync(user);

        return new UserModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles.Select(MapRole).ToArray(),
        };
    }

    public async Task<UserModel?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(principal);
        if (!userId.HasValue)
        {
            return null;
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());
        return user == null || !user.IsActive ? null : await MapUserAsync(user, cancellationToken);
    }

    public async Task<UserModel> UpdateProfileAsync(ClaimsPrincipal principal, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(principal) ?? throw new BankException("User is not authenticated.", 401);
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new BankException("User was not found.", 404);

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.DateModified = DateTime.UtcNow;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new BankException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }

        return await MapUserAsync(user, cancellationToken);
    }

    private static long? GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        return long.TryParse(value, out var userId) ? userId : null;
    }

    private static UserRoleModel MapRole(string role)
    {
        return string.Equals(role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase)
            ? UserRoleModel.Admin
            : UserRoleModel.User;
    }
}
