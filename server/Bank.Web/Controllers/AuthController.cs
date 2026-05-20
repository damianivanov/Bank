using Bank.Core.JsonModels.Auth;
using Bank.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService authService;

    public AuthController(IAuthService authService)
    {
        this.authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        SetAuthCookies(result);
        return Ok(result.Response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        SetAuthCookies(result);
        return Ok(result.Response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken)
    {
        var result = await authService.RefreshAsync(Request.Cookies["RefreshToken"], cancellationToken);
        SetAuthCookies(result);
        return Ok(result.Response);
    }

    [HttpGet("current-user")]
    [Authorize]
    public async Task<ActionResult<UserModel>> CurrentUser(CancellationToken cancellationToken)
    {
        var user = await authService.GetCurrentUserAsync(User, cancellationToken);
        return user == null ? Unauthorized("User is not authenticated.") : Ok(user);
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserModel>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await authService.UpdateProfileAsync(User, request, cancellationToken);
        return Ok(user);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<string>> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(Request.Cookies["RefreshToken"], cancellationToken);
        ClearAuthCookies();
        return Ok("Logged out");
    }

    private void SetAuthCookies(AuthResult result)
    {
        var accessCookieOptions = BuildCookieOptions(result.Response.TokenExpiresAtUtc);
        var refreshCookieOptions = BuildCookieOptions(result.Response.RefreshTokenExpiresAtUtc);

        Response.Cookies.Append("Token", result.AccessToken, accessCookieOptions);
        Response.Cookies.Append("RefreshToken", result.RefreshToken, refreshCookieOptions);
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete("Token");
        Response.Cookies.Delete("RefreshToken");
    }

    private CookieOptions BuildCookieOptions(DateTime expiresAtUtc)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                ? SameSiteMode.Lax
                : SameSiteMode.Strict,
            Secure = !HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
            Expires = DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc),
        };
    }
}
