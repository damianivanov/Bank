using Bank.Core.JsonModels.Auth;
using System.Security.Claims;

namespace Bank.Services.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult> RefreshAsync(string? refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);
    Task<UserModel?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<UserModel> UpdateProfileAsync(ClaimsPrincipal principal, UpdateProfileRequest request, CancellationToken cancellationToken = default);
}
