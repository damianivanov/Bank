using Bank.DB.Configurations.Base;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class ErrorConfiguration : BaseConfiguration<Error>
{
    public override void Configure(EntityTypeBuilder<Error> builder)
    {
        base.Configure(builder);

        builder.Property(error => error.Message).HasMaxLength(1000).IsRequired();
        builder.Property(error => error.Details).HasMaxLength(8000);
        builder.Property(error => error.Path).HasMaxLength(500);
        builder.Property(error => error.UserName).HasMaxLength(256);
    }
}
