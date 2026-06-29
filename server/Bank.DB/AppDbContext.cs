using Bank.DB.Entities;
using Bank.DB.Entities.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bank.DB;

public class AppDbContext
    : IdentityDbContext<User, Role, long, IdentityUserClaim<long>, UserRole, IdentityUserLogin<long>, IdentityRoleClaim<long>, IdentityUserToken<long>>
{
    public DbSet<Error> Errors => Set<Error>();
    public DbSet<Token> Tokens => Set<Token>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyRepresentative> CompanyRepresentatives => Set<CompanyRepresentative>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<CreditTypeCondition> CreditTypeConditions => Set<CreditTypeCondition>();
    public DbSet<Credit> Credits => Set<Credit>();
    public DbSet<CreditInstallment> CreditInstallments => Set<CreditInstallment>();
    public DbSet<CreditPricingChange> CreditPricingChanges => Set<CreditPricingChange>();
    public DbSet<CreditTerms> CreditTerms => Set<CreditTerms>();
    public DbSet<CreditTermsFee> CreditTermsFees => Set<CreditTermsFee>();
    public DbSet<SavedCalculation> SavedCalculations => Set<SavedCalculation>();
    public DbSet<DepositRequest> DepositRequests => Set<DepositRequest>();
    public DbSet<MoneyTransaction> MoneyTransactions => Set<MoneyTransaction>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public int SaveChanges(long? userId)
    {
        return SaveChanges(userId, acceptAllChangesOnSuccess: true);
    }

    public override int SaveChanges()
    {
        return SaveChanges(acceptAllChangesOnSuccess: true);
    }

    public int SaveChanges(long? userId, bool acceptAllChangesOnSuccess)
    {
        TrackUser(userId);
        return SaveChanges(acceptAllChangesOnSuccess);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AddTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public Task<int> SaveChangesAsync(long? userId, CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(userId, acceptAllChangesOnSuccess: true, cancellationToken);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);
    }

    public Task<int> SaveChangesAsync(long? userId, bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        TrackUser(userId);
        return SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AddTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    private void AddTimestamps()
    {
        var now = DateTime.UtcNow;
        var entries = ChangeTracker.Entries<IBaseEntity>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added && entry.Entity.DateCreated == default)
            {
                entry.Entity.DateCreated = now;
            }

            entry.Entity.DateModified = now;
        }
    }

    private void TrackUser(long? userId)
    {
        if (!userId.HasValue)
        {
            return;
        }

        var entries = ChangeTracker.Entries<IBaseTrackUserEntity>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedById = userId.Value;
            }

            entry.Entity.ModifiedById = userId.Value;
        }
    }
}
