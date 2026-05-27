using Bank.Core.Common;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Auth;
using Bank.Services.Auth;
using Bank.Web.Controllers.Base;
using Bank.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Web.Controllers;

[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly IAuthService authService;

    public AuthController(IAuthService authService)
    {
        this.authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<CommonJsonModel<AuthResponse>>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        SetAuthCookies(result);
        return this.ReturnJson(result.Response);
    }

    [HttpPost("register-customer")]
    [AllowAnonymous]
    public async Task<ActionResult<CommonJsonModel<AuthResponse>>> RegisterCustomer(RegisterCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterCustomerAsync(request, cancellationToken);
        SetAuthCookies(result);
        return this.ReturnJson(result.Response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<CommonJsonModel<AuthResponse>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        SetAuthCookies(result);
        return this.ReturnJson(result.Response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<CommonJsonModel<AuthResponse>>> Refresh(CancellationToken cancellationToken)
    {
        try
        {
            var refreshToken = Request.Cookies["RefreshToken"];
            var result = await authService.RefreshAsync(refreshToken, cancellationToken);
            SetAuthCookies(result);
            return this.ReturnJson(result.Response);
        }
        catch (BankException exception)
        {
            return this.ReturnError(exception.Message, exception.StatusCode);
        }
    }

    [HttpGet("current-user")]
    [AllowAnonymous]
    public async Task<ActionResult<CommonJsonModel<UserModel>>> CurrentUser(CancellationToken cancellationToken)
    {
        var user = await authService.GetCurrentUserAsync(cancellationToken);
        return this.ReturnJson(user ?? new UserModel());
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<CommonJsonModel<UserModel>>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await authService.UpdateProfileAsync(request, cancellationToken);
        return this.ReturnJson(user);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<CommonJsonModel<string>>> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(Request.Cookies["RefreshToken"], cancellationToken);
        ClearAuthCookies();
        return this.ReturnJson("Logged out");
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
