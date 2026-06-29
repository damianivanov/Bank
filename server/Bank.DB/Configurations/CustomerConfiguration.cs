using Bank.DB.Configurations.Base;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class CustomerConfiguration : BaseConfiguration<Customer>
{
    public override void Configure(EntityTypeBuilder<Customer> builder)
    {
        base.Configure(builder);

        builder.HasOne(c => c.Person)
            .WithMany(p => p.Customers)
            .HasForeignKey(c => c.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Company)
            .WithMany(c => c.Customers)
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.PersonId)
            .IsUnique()
            .HasFilter("[PersonId] IS NOT NULL");

        builder.HasIndex(c => c.CompanyId)
            .IsUnique()
            .HasFilter("[CompanyId] IS NOT NULL");

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Customers_PartyXor",
            "([PersonId] IS NOT NULL AND [CompanyId] IS NULL) OR ([PersonId] IS NULL AND [CompanyId] IS NOT NULL)"));
    }
}
