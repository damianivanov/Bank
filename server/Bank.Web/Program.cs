using Bank.Core.Common;
using Bank.Core.JsonModels.Auth;
using Bank.Core.Settings;
using Bank.DB;
using Bank.DB.Constants;
using Bank.DB.Entities;
using Bank.Services.Accounts.BankAccounts;
using Bank.Services.Accounts.Iban;
using Bank.Services.Auth;
using Bank.Services.Calculators;
using Bank.Services.CreditConditions;
using Bank.Services.Credits;
using Bank.Services.Customers;
using Bank.Services.Diagnostics;
using Bank.Services.MoneyOperations;
using Bank.Services.Users;
using Bank.Services.Users.Administration;
using Bank.Web.Attributes;
using Bank.Web.Infrastructure;
using Bank.Web.Infrastructure.Authorization;
using Bank.Web.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

var configuredOrigins = builder.Configuration
    .GetSection("Application:AllowedOrigins")
    .Get<string[]>() ?? [];

var allowedOrigins = configuredOrigins
    .Concat([
        builder.Configuration["Application:ClientUrl"],
        "http://localhost:3001",
        "https://localhost:3001",
    ])
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin!.Trim().TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddScoped<LogApiErrorAttribute>();
builder.Services.AddControllers(options => options.Filters.AddService<LogApiErrorAttribute>());

builder.Services.Configure<ApiBehaviorOptions>(o =>
{
    o.InvalidModelStateResponseFactory = context =>
    {
        var message = string.Join(" ", context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid request." : e.ErrorMessage));

        if (string.IsNullOrWhiteSpace(message))
        {
            message = "Invalid request.";
        }

        return new BadRequestObjectResult(CommonJsonModel<string>.ErrorResult(message));
    };
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services
    .AddIdentity<User, Role>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
    })
    .AddErrorDescriber<BulgarianIdentityErrorDescriber>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtSigningKey))
{
    throw new InvalidOperationException("JWT signing key is not configured.");
}

var jwtKey = Encoding.UTF8.GetBytes(jwtSigningKey);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
            ClockSkew = TimeSpan.Zero,
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Headers.Authorization.FirstOrDefault()?.Split(' ').Last();

                if (string.IsNullOrEmpty(token))
                {
                    token = context.Request.Cookies["Token"];
                }

                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = JwtSecurityStampValidator.ValidateAsync,
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Явно изброени роли: RequireStaff приема Staff и Admin, RequireAdmin приема само Admin.
    options.AddPolicy(Policies.RequireStaff, policy => policy.RequireRole(RoleNames.Staff, RoleNames.Admin));
    options.AddPolicy(Policies.RequireAdmin, policy => policy.RequireRole(RoleNames.Admin));
    options.AddPolicy(Policies.RequireCustomer, policy => policy.RequireRole(RoleNames.Customer));
    // Fail closed: всеки endpoint без явни authorization метаданни пак изисква автентикиран потребител.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.ForwardLimit = 1;
    o.KnownProxies.Clear();
    o.KnownNetworks.Clear();

    var knownProxies = builder.Configuration.GetSection("Application:KnownProxies").Get<string[]>() ?? [];
    foreach (var proxy in knownProxies)
    {
        if (IPAddress.TryParse(proxy, out var ip))
        {
            o.KnownProxies.Add(ip);
        }
    }
});

builder.Services.AddBankRateLimiting();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ApplicationSettings>();
builder.Services.AddSingleton(new DemoOptions
{
    AllowPayingFutureInstallments = builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Application:AllowPayingFutureInstallments"),
});
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserAdministrationService, UserAdministrationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IIbanGenerator, IbanGenerator>();
builder.Services.AddScoped<IBankAccountService, BankAccountService>();
builder.Services.AddScoped<ICreditConditionService, CreditConditionService>();
builder.Services.AddScoped<ICreditService, CreditService>();
builder.Services.AddScoped<ICreditRepricingService, CreditRepricingService>();
builder.Services.AddScoped<IRepaymentPlanCalculator, RepaymentPlanCalculator>();
builder.Services.AddScoped<IVipPricingPolicy, VipPricingPolicy>();
builder.Services.AddScoped<IAccountLedger, AccountLedger>();
builder.Services.AddScoped<IMoneyOperationService, MoneyOperationService>();
builder.Services.AddScoped<IDepositApprovalService, DepositApprovalService>();
builder.Services.AddScoped<IErrorService, ErrorService>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ICreditCalculatorService, CreditCalculatorService>();
builder.Services.AddSingleton<ILeasingCalculatorService, LeasingCalculatorService>();
builder.Services.AddSingleton<IRefinancingCalculatorService, RefinancingCalculatorService>();
builder.Services.AddScoped<ISavedCalculationService, SavedCalculationService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<MustChangePasswordMiddleware>();

app.UseMiddleware<AnonFingerprintMiddleware>();
app.UseRateLimiter();

app.MigrateDatabase();
await app.SeedDatabase();

app.MapControllers();
app.Run();
