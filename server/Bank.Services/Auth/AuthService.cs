using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Auth;
using Bank.Core.Settings;
using Bank.DB;
using Bank.DB.Constants;
using Bank.DB.Entities;
using Bank.Services.Common;
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
    private readonly RoleManager<Role> roleManager;
    private readonly IUserService userService;
    private readonly ApplicationSettings applicationSettings;

    public AuthService(
        AppDbContext dbContext,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IUserService userService,
        ApplicationSettings applicationSettings)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
        this.roleManager = roleManager;
        this.userService = userService;
        this.applicationSettings = applicationSettings;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureRoleAsync(RoleNames.User);

        var email = request.Email.Trim();
        await EnsureEmailIsAvailableAsync(email);

        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            IsActive = true,
            DateCreated = DateTime.UtcNow,
        };

        await CreateUserWithRoleAsync(user, request.Password, RoleNames.User);
        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task<AuthResult> RegisterCustomerAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureRoleAsync(RoleNames.User);
        await EnsureRoleAsync(RoleNames.Customer);

        var email = request.Email.Trim();
        await EnsureEmailIsAvailableAsync(email);

        var personalIdentifier = NormalizeIdentifier(request.PersonalIdentifier);
        var companyIdentifier = NormalizeIdentifier(request.CompanyIdentifier);
        var hasPersonalIdentifier = !string.IsNullOrWhiteSpace(personalIdentifier);
        var hasCompanyIdentifier = !string.IsNullOrWhiteSpace(companyIdentifier);

        if (hasPersonalIdentifier == hasCompanyIdentifier)
        {
            throw new BankException("Provide either personal identifier or company identifier.");
        }

        if (hasPersonalIdentifier && !BulgarianIdentifierValidator.IsValidEgn(personalIdentifier))
        {
            throw new BankException("Personal identifier must be a valid EGN.");
        }

        if (hasCompanyIdentifier && !BulgarianIdentifierValidator.IsValidEik(companyIdentifier))
        {
            throw new BankException("Company identifier must be a valid EIK.");
        }

        var customer = hasPersonalIdentifier
            ? await dbContext.Customers.FirstOrDefaultAsync(entity => entity.PersonalIdentifier == personalIdentifier, cancellationToken)
            : await dbContext.Customers.FirstOrDefaultAsync(entity => entity.CompanyIdentifier == companyIdentifier, cancellationToken);

        if (customer == null)
        {
            throw new BankException("Customer was not found.", 404);
        }

        var customerAlreadyHasLogin = await dbContext.Users.AnyAsync(entity => entity.CustomerId == customer.Id, cancellationToken);
        if (customerAlreadyHasLogin)
        {
            throw new BankException("This customer already has a login account.");
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = BuildFirstName(customer),
            LastName = BuildLastName(customer),
            CustomerId = customer.Id,
            IsActive = true,
            DateCreated = DateTime.UtcNow,
        };

        await CreateUserWithRoleAsync(user, request.Password, RoleNames.Customer);
        await AddUserToRoleAsync(user, RoleNames.User);
        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim())
            ?? throw new BankException("Invalid email or password.", 401);

        if (!user.IsActive || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new BankException("Invalid email or password.", 401);
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.DateModified = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task<AuthResult> RefreshAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new BankException("Refresh token is missing.", 401);
        }

        var principal = ValidateRefreshToken(refreshToken);
        var userId = GetUserId(principal) ?? throw new BankException("Refresh token is invalid.", 401);

        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.Value == refreshToken && token.UserId == userId, cancellationToken)
            ?? throw new BankException("Refresh token is invalid.", 401);

        if (storedToken.IsRevoked || storedToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new BankException("Refresh token is expired.", 401);
        }

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new BankException("User was not found.", 404);

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
            throw new BankException($"Refresh token is invalid. {ex.Message}", 401);
        }
        catch (ArgumentException ex)
        {
            throw new BankException($"Refresh token is invalid. {ex.Message}", 401);
        }
    }

    private static IEnumerable<Claim> BuildClaims(User user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };

        if (user.CustomerId.HasValue)
        {
            claims.Add(new Claim("customer_id", user.CustomerId.Value.ToString()));
        }

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return claims;
    }

    private async Task EnsureRoleAsync(string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new Role { Name = roleName, NormalizedName = roleName.ToUpperInvariant() });
        }
    }

    private static long? GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return long.TryParse(value, out var userId) ? userId : null;
    }

    private async Task EnsureEmailIsAvailableAsync(string email)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            throw new BankException("A user with this email already exists.");
        }
    }

    private async Task CreateUserWithRoleAsync(User user, string password, string roleName)
    {
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new BankException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }

        await AddUserToRoleAsync(user, roleName);
    }

    private async Task AddUserToRoleAsync(User user, string roleName)
    {
        var result = await userManager.AddToRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            throw new BankException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }

    private static string? NormalizeIdentifier(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string BuildFirstName(Customer customer)
    {
        return customer.FirstName
            ?? customer.CompanyName
            ?? "Customer";
    }

    private static string BuildLastName(Customer customer)
    {
        return customer.LastName
            ?? customer.RepresentativeName
            ?? "User";
    }
}
