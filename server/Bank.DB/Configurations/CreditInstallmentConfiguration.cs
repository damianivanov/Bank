using Bank.DB.Configurations.Base;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class CreditInstallmentConfiguration : BaseConfiguration<CreditInstallment>
{
    public override void Configure(EntityTypeBuilder<CreditInstallment> builder)
    {
        base.Configure(builder);

        builder.HasIndex(installment => new { installment.CreditId, installment.InstallmentNumber }).IsUnique();

        builder.HasOne(installment => installment.Credit)
            .WithMany(credit => credit.Installments)
            .HasForeignKey(installment => installment.CreditId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
