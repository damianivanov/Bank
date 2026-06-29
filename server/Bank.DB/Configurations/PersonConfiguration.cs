using Bank.DB.Configurations.Base;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class PersonConfiguration : BaseConfiguration<Person>
{
    public override void Configure(EntityTypeBuilder<Person> builder)
    {
        base.Configure(builder);

        builder.Property(p => p.Egn).IsRequired().HasMaxLength(20);
        builder.HasIndex(p => p.Egn).IsUnique();
    }
}
