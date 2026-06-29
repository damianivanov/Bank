using Bank.DB.Configurations.Base;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class CreditTermsFeeConfiguration : BaseConfiguration<CreditTermsFee>
{
    public override void Configure(EntityTypeBuilder<CreditTermsFee> builder)
    {
        base.Configure(builder);

        builder.HasOne(fee => fee.CreditTerms)
            .WithMany(terms => terms.Fees)
            .HasForeignKey(fee => fee.CreditTermsId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
