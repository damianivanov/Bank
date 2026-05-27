using Bank.DB.Configurations.Base;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class CreditPricingChangeConfiguration : BaseConfiguration<CreditPricingChange>
{
    public override void Configure(EntityTypeBuilder<CreditPricingChange> builder)
    {
        base.Configure(builder);

        builder.Property(pricingChange => pricingChange.EffectiveFromPaymentNumber)
            .HasColumnName("EffectiveFromInstallmentNumber");

        builder.HasOne(pricingChange => pricingChange.Credit)
            .WithMany(credit => credit.PricingChanges)
            .HasForeignKey(pricingChange => pricingChange.CreditId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
