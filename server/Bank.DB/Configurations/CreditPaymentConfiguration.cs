using Bank.DB.Configurations.Base;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class CreditPaymentConfiguration : BaseConfiguration<CreditPayment>
{
    public override void Configure(EntityTypeBuilder<CreditPayment> builder)
    {
        base.Configure(builder);

        builder.ToTable("CreditInstallments");
        builder.Property(payment => payment.PaymentNumber).HasColumnName("InstallmentNumber");
        builder.Property(payment => payment.PaymentAmount).HasColumnName("InstallmentAmount");

        builder.HasIndex(payment => new { payment.CreditId, payment.PaymentNumber }).IsUnique();

        builder.HasOne(payment => payment.Credit)
            .WithMany(credit => credit.Payments)
            .HasForeignKey(payment => payment.CreditId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
