using Bank.Core.JsonModels.Auth;

namespace Bank.Services.Auth;

public sealed record AuthResult(
    AuthResponse Response,
    string AccessToken,
    string RefreshToken);
