using Bank.DB.Configurations.Base;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class MoneyTransactionConfiguration : BaseConfiguration<MoneyTransaction>
{
    public override void Configure(EntityTypeBuilder<MoneyTransaction> builder)
    {
        base.Configure(builder);

        // Идемпотентност на ниво база за теглене / вноска по кредит / одобрен депозит.
        builder.HasIndex(t => t.IdempotencyKey).IsUnique();

        // Историята по сметка се чете в обратен хронологичен ред.
        builder.HasIndex(t => new { t.BankAccountId, t.DateCreated });

        // Всички FK са Restrict — регистърът е неизменим архив и не бива да се трие каскадно
        // (а и SQL Server не допуска множество каскадни пътища към една таблица).
        builder.HasOne(t => t.BankAccount)
            .WithMany()
            .HasForeignKey(t => t.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Credit)
            .WithMany()
            .HasForeignKey(t => t.CreditId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.CreditInstallment)
            .WithMany()
            .HasForeignKey(t => t.CreditPaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.DepositRequest)
            .WithMany()
            .HasForeignKey(t => t.DepositRequestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
