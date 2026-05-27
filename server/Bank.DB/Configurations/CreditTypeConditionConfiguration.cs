using Bank.DB.Configurations.Base;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class CreditTypeConditionConfiguration : BaseConfiguration<CreditTypeCondition>
{
    public override void Configure(EntityTypeBuilder<CreditTypeCondition> builder)
    {
        base.Configure(builder);

        builder.HasIndex(condition => condition.CreditType).IsUnique();
    }
}
