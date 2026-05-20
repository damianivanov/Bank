namespace Bank.Core.JsonModels.Auth;

public class AuthResponse
{
    public UserModel User { get; set; } = new();
    public DateTime TokenExpiresAtUtc { get; set; }
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
}
