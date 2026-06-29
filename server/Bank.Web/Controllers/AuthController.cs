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

    [HttpPost("register")] // Регистрираме нов потребител. Връща съобщение за успешна регистрация.
    [AllowAnonymous]
    public async Task<ActionResult<CommonJsonModel<string>>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Без SetAuthCookies: регистрацията не отваря сесия, потребителят влиза изрично през login.
            await authService.RegisterAsync(request, cancellationToken);
            return this.ReturnJson("Регистрацията е успешна.");
        }
        catch (BankException exception)
        {
            return this.ReturnError(exception.Message, exception.StatusCode);
        }
    }

    [HttpPost("login")] // Влизаме в системата. Връща информация за потребителя и JWT токени за достъп и обновяване.
    [AllowAnonymous]
    public async Task<ActionResult<CommonJsonModel<AuthResponse>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await authService.LoginAsync(request, cancellationToken);
            SetAuthCookies(result);
            return this.ReturnJson(result.Response);
        }
        catch (BankException exception)
        {
            return this.ReturnError(exception.Message, exception.StatusCode);
        }
    }

    [HttpPost("change-password")] // Променяме паролата на потребителя. Връща информация за потребителя и нови JWT токени за достъп и обновяване.
    [Authorize]
    public async Task<ActionResult<CommonJsonModel<AuthResponse>>> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await authService.ChangePasswordAsync(request, cancellationToken);
            SetAuthCookies(result);
            return this.ReturnJson(result.Response);
        }
        catch (BankException exception)
        {
            return this.ReturnError(exception.Message, exception.StatusCode);
        }
    }

    [HttpPost("refresh")] // Обновяваме JWT токените за достъп и обновяване. Връща информация за потребителя и нови JWT токени за достъп и обновяване.
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

    [HttpGet("current-user")] // Взимаме информация за текущия потребител.
    [AllowAnonymous]
    public async Task<ActionResult<CommonJsonModel<UserModel>>> CurrentUser(CancellationToken cancellationToken)
    {
        var user = await authService.GetCurrentUserAsync(cancellationToken);
        return this.ReturnJson(user ?? new UserModel());
    }

    [HttpPut("profile")] // Актуализираме профила на текущия потребител. Връща информация за актуализирания потребител.
    [Authorize]
    public async Task<ActionResult<CommonJsonModel<UserModel>>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await authService.UpdateProfileAsync(request, cancellationToken);
        return this.ReturnJson(user);
    }

    [HttpPost("logout")] // Излизаме от системата и деактивираме токените.
    [Authorize]
    public async Task<ActionResult<CommonJsonModel<string>>> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(Request.Cookies["RefreshToken"], cancellationToken);
        ClearAuthCookies();
        return this.ReturnJson("Logged out");
    }

    private void SetAuthCookies(AuthResult result) // Задаваме JWT токените за достъп и обновяване като HttpOnly cookies.
    {
        var accessCookieOptions = BuildCookieOptions(result.Response.TokenExpiresAtUtc);
        var refreshCookieOptions = BuildCookieOptions(result.Response.RefreshTokenExpiresAtUtc);

        Response.Cookies.Append("Token", result.AccessToken, accessCookieOptions);
        Response.Cookies.Append("RefreshToken", result.RefreshToken, refreshCookieOptions);
    }

    private void ClearAuthCookies() // Изтриваме JWT токените за достъп и обновяване от HttpOnly cookies.
    {
        Response.Cookies.Delete("Token");
        Response.Cookies.Delete("RefreshToken");
    }

    private CookieOptions BuildCookieOptions(DateTime expiresAtUtc) // Създаваме CookieOptions за задаване на JWT токените като HttpOnly cookies.
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
