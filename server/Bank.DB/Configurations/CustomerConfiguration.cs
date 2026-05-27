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

        builder.HasIndex(customer => customer.PersonalIdentifier)
            .IsUnique()
            .HasFilter("[PersonalIdentifier] IS NOT NULL");

        builder.HasIndex(customer => customer.CompanyIdentifier)
            .IsUnique()
            .HasFilter("[CompanyIdentifier] IS NOT NULL");
    }
}
