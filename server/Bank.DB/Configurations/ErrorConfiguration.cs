using Bank.DB.Configurations.Base;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class ErrorConfiguration : BaseConfiguration<Error>
{
    public override void Configure(EntityTypeBuilder<Error> builder)
    {
        base.Configure(builder);
    }
}
