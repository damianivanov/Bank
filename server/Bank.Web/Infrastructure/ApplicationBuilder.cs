using Bank.Core.Enums;
using Bank.DB;
using Bank.DB.Constants;
using Bank.DB.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bank.Web.Infrastructure;

public static class ApplicationBuilder
{
    public static void MigrateDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
    }

    public static async Task SeedDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        foreach (var roleName in new[] { RoleNames.User, RoleNames.Customer, RoleNames.Staff, RoleNames.Admin })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new Role { Name = roleName, NormalizedName = roleName.ToUpperInvariant() });
            }
        }

        await SeedCreditTypeConditionsAsync(dbContext);

        await SeedAdminUserAsync(configuration, userManager);
    }

    private static async Task SeedCreditTypeConditionsAsync(AppDbContext dbContext)
    {
        var seedConditions = new[]
        {
            new CreditTypeCondition
            {
                CreditType = CreditType.Consumer,
                Name = "Потребителски кредит",
                StandardAnnualInterestRate = 7.20m,
                VipAnnualInterestRate = 5.90m,
                MaximumAmount = 80000m,
                MaximumTermMonths = 120,
                StandardGrantingFee = 120m,
                VipGrantingFee = 60m,
                DefaultPaymentType = PaymentType.Annuity,
                PromoPeriodMonths = 3,
                StandardPromoRate = 4.90m,
                VipPromoRate = 3.90m,
                GracePeriodMonths = 0,
                StandardMonthlyManagementFee = 4m,
                VipMonthlyManagementFee = 2m,
                StandardAnnualManagementFee = 0m,
                VipAnnualManagementFee = 0m,
                IsActive = true,
            },
            new CreditTypeCondition
            {
                CreditType = CreditType.Mortgage,
                Name = "Ипотечен кредит",
                StandardAnnualInterestRate = 3.20m,
                VipAnnualInterestRate = 2.80m,
                MaximumAmount = 500000m,
                MaximumTermMonths = 360,
                StandardGrantingFee = 300m,
                VipGrantingFee = 150m,
                DefaultPaymentType = PaymentType.Annuity,
                PromoPeriodMonths = 3,
                StandardPromoRate = 2.50m,
                VipPromoRate = 2.10m,
                GracePeriodMonths = 3,
                StandardMonthlyManagementFee = 0m,
                VipMonthlyManagementFee = 0m,
                StandardAnnualManagementFee = 60m,
                VipAnnualManagementFee = 30m,
                IsActive = true,
            },
        };

        var existingConditions = await dbContext.CreditTypeConditions
            .ToListAsync();

        // Seed-ът е само първоначални данни: добавя липсващите условия, но НЕ пренаписва съществуващите.
        // Тарифата вече се управлява от админ през приложението, затова презаписване при всяко стартиране
        // би изтрило неговите редакции.
        var newConditions = seedConditions
            .Where(seed => existingConditions.All(existing => existing.CreditType != seed.CreditType))
            .ToList();

        if (newConditions.Count == 0)
        {
            return;
        }

        dbContext.CreditTypeConditions.AddRange(newConditions);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedAdminUserAsync(IConfiguration configuration, UserManager<User> userManager)
    {
        var email = configuration["AdminUser:Email"];
        var password = configuration["AdminUser:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new User
            {
                UserName = email,
                Email = email,
                FirstName = "Bank",
                LastName = "Admin",
                IsActive = true,
                DateCreated = DateTime.UtcNow,
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            await userManager.AddToRoleAsync(user, RoleNames.Admin);
        }

        if (!await userManager.IsInRoleAsync(user, RoleNames.User))
        {
            await userManager.AddToRoleAsync(user, RoleNames.User);
        }
    }

}
