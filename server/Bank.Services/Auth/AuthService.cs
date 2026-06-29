using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Auth;
using Bank.Core.Settings;
using Bank.DB;
using Bank.DB.Constants;
using Bank.DB.Entities;
using Bank.Services.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Bank.Services.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext dbContext;
    private readonly UserManager<User> userManager;
    private readonly IUserService userService;
    private readonly ApplicationSettings applicationSettings;

    public AuthService(
        AppDbContext dbContext,
        UserManager<User> userManager,
        IUserService userService,
        ApplicationSettings applicationSettings)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
        this.userService = userService;
        this.applicationSettings = applicationSettings;
    }

    public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();

        // Имейлът трябва да е свободен.
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            throw new BankException("Вече съществува потребител с този имейл.");
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            IsActive = true,
            DateCreated = DateTime.UtcNow,
        };

        // Регистрацията само създава акаунта, потребителите трябва да се логнат след това.
        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            throw new BankException(string.Join(" ", createResult.Errors.Select(error => error.Description)));
        }

        var roleResult = await userManager.AddToRoleAsync(user, RoleNames.User);
        if (!roleResult.Succeeded)
        {
            throw new BankException(string.Join(" ", roleResult.Errors.Select(error => error.Description)));
        }
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim())
            ?? throw new BankException("Невалиден имейл или парола.", 401);

        if (!user.IsActive || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new BankException("Невалиден имейл или парола.", 401);
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.DateModified = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task<AuthResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var userId = userService.GetRequiredLoggedInUserId();
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new BankException("Потребителят не е намерен.", 404);

        var changeResult = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!changeResult.Succeeded)
        {
            throw new BankException(string.Join(" ", changeResult.Errors.Select(error => error.Description)));
        }

        if (user.MustChangePassword)
        {
            user.MustChangePassword = false;
            user.DateModified = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
        }

        // ChangePasswordAsync ротира SecurityStamp-а -> текущият токен пада на следващата заявка.
        // Преиздаваме нова сесия, за да не изхвърлим потребителя веднага след смяната.
        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task<AuthResult> RefreshAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new BankException("Липсва токен за обновяване.", 401);
        }

        var principal = ValidateRefreshToken(refreshToken);
        var userId = GetUserId(principal) ?? throw new BankException("Невалиден токен за обновяване.", 401);

        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.Value == refreshToken && token.UserId == userId, cancellationToken)
            ?? throw new BankException("Невалиден токен за обновяване.", 401);

        if (storedToken.IsRevoked || storedToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new BankException("Токенът за обновяване е изтекъл.", 401);
        }

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new BankException("Потребителят не е намерен.", 404);

        if (!user.IsActive)
        {
            throw new BankException("Този акаунт е деактивиран.", 401);
        }

        storedToken.RevokedAtUtc = DateTime.UtcNow;
        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.Value == refreshToken, cancellationToken);

        if (storedToken == null || storedToken.IsRevoked)
        {
            return;
        }

        storedToken.RevokedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<UserModel?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        return userService.GetCurrentUserAsync(cancellationToken);
    }

    public Task<UserModel> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        return userService.UpdateProfileAsync(request, cancellationToken);
    }

    private async Task<AuthResult> CreateSessionAsync(User user, CancellationToken cancellationToken)
    {
        // Ако SecurityStamp е празен, го обновяваме, за да се гарантира, че токените са валидни.
        if (string.IsNullOrEmpty(user.SecurityStamp))
        {
            await userManager.UpdateSecurityStampAsync(user);
        }

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = GenerateJwtToken(user, roles, out var tokenExpiresAtUtc);
        var refreshToken = GenerateRefreshToken(user, roles, out var refreshTokenExpiresAtUtc);

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Value = refreshToken,
            ExpiresAtUtc = refreshTokenExpiresAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResult(
            new AuthResponse
            {
                User = await userService.MapUserAsync(user, cancellationToken),
                TokenExpiresAtUtc = tokenExpiresAtUtc,
                RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc,
            },
            accessToken,
            refreshToken);
    }

    private string GenerateJwtToken(User user, IEnumerable<string> roles, out DateTime expiresAtUtc)
    {
        expiresAtUtc = DateTime.UtcNow.AddMinutes(applicationSettings.JwtExpirationMinutes);
        var claims = BuildClaims(user, roles);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(applicationSettings.JwtSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: applicationSettings.JwtIssuer,
            audience: applicationSettings.JwtAudience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken(User user, IEnumerable<string> roles, out DateTime expiresAtUtc)
    {
        expiresAtUtc = DateTime.UtcNow.AddDays(applicationSettings.RefreshTokenExpirationDays);
        var claims = BuildClaims(user, roles);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(applicationSettings.RefreshTokenSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: applicationSettings.RefreshTokenIssuer,
            audience: applicationSettings.RefreshTokenAudience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal ValidateRefreshToken(string refreshToken)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = applicationSettings.RefreshTokenIssuer,
            ValidAudience = applicationSettings.RefreshTokenAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(applicationSettings.RefreshTokenSigningKey)),
            ClockSkew = TimeSpan.Zero,
        };

        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(refreshToken, validationParameters, out _);
        }
        catch (SecurityTokenException ex)
        {
            throw new BankException($"Невалиден токен за обновяване. {ex.Message}", 401);
        }
        catch (ArgumentException ex)
        {
            throw new BankException($"Невалиден токен за обновяване. {ex.Message}", 401);
        }
    }

    private static IEnumerable<Claim> BuildClaims(User user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };

        if (user.PersonId.HasValue)
        {
            claims.Add(new Claim(ClaimNames.PersonId, user.PersonId.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(user.SecurityStamp))
        {
            claims.Add(new Claim(ClaimNames.SecurityStamp, user.SecurityStamp));
        }

        if (user.MustChangePassword)
        {
            claims.Add(new Claim(ClaimNames.MustChangePassword, "true"));
        }

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return claims;
    }

    private static long? GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return long.TryParse(value, out var userId) ? userId : null;
    }

}
