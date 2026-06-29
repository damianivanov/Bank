using Bank.DB;
using Bank.DB.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bank.Web.Infrastructure.Authorization;

// Сверява security_stamp claim-а от вече валидирания (подпис + срок) access токен с текущата стойност в
// базата при ВСЯКА заявка. При смяна на роля/деактивиране UserService "бутва" stamp-а -> старите токени
// падат моментално (context.Fail() -> 401), без да чакаме изтичането им. Това е цената за реална
// инвалидация на иначе stateless JWT — един read по PK индекс на автентикирана заявка.
public static class JwtSecurityStampValidator
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (principal == null)
        {
            context.Fail("Token principal is missing.");
            return;
        }

        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (!long.TryParse(userIdValue, out var userId))
        {
            context.Fail("Token is missing a valid user identifier.");
            return;
        }

        var tokenStamp = principal.FindFirstValue(ClaimNames.SecurityStamp);

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var account = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.SecurityStamp, u.IsActive })
            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

        if (account == null || !account.IsActive)
        {
            context.Fail("Account is no longer active.");
            return;
        }

        // Fail-closed: липсващ stamp в базата НЕ бива да минава като съвпадение с празен claim в токена.
        // Нормално не се случва (UserManager.CreateAsync винаги генерира stamp + CreateSessionAsync го
        // подсигурява), но един security gate трябва да отказва при невъзможност за сверка, не да пуска.
        if (string.IsNullOrEmpty(account.SecurityStamp)
            || !string.Equals(account.SecurityStamp, tokenStamp, StringComparison.Ordinal))
        {
            context.Fail("Session has been invalidated.");
        }
    }
}
