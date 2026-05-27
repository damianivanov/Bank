using Bank.DB.Configurations.Base;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class BankAccountConfiguration : BaseConfiguration<BankAccount>
{
    public override void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        base.Configure(builder);

        builder.HasIndex(account => account.IBAN).IsUnique();

        builder.HasOne(account => account.Customer)
            .WithMany(customer => customer.Accounts)
            .HasForeignKey(account => account.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
