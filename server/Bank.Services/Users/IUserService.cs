using Bank.Core.JsonModels.Auth;
using Bank.DB.Entities;
using System.Security.Claims;

namespace Bank.Services.Users;

public interface IUserService
{
    Task<UserModel> MapUserAsync(User user, CancellationToken cancellationToken = default);
    Task<UserModel?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<UserModel> UpdateProfileAsync(ClaimsPrincipal principal, UpdateProfileRequest request, CancellationToken cancellationToken = default);
}
