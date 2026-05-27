using Bank.DB.Configurations.Base;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class CreditConfiguration : BaseConfiguration<Credit>
{
    public override void Configure(EntityTypeBuilder<Credit> builder)
    {
        base.Configure(builder);

        builder.Property(credit => credit.PlannedMonthlyPaymentAmount)
            .HasColumnName("PlannedMonthlyInstallmentAmount");

        builder.HasOne(credit => credit.Customer)
            .WithMany(customer => customer.Credits)
            .HasForeignKey(credit => credit.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(credit => credit.CreditTypeCondition)
            .WithMany(condition => condition.Credits)
            .HasForeignKey(credit => credit.CreditTypeConditionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
