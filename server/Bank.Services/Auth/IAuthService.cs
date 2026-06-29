using Bank.Core.JsonModels.Auth;

namespace Bank.Services.Auth;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult> RefreshAsync(string? refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);
    Task<UserModel?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    Task<UserModel> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
}
