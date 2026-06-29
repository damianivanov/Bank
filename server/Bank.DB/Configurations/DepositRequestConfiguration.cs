using Bank.DB.Configurations.Base;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class DepositRequestConfiguration : BaseConfiguration<DepositRequest>
{
    public override void Configure(EntityTypeBuilder<DepositRequest> builder)
    {
        base.Configure(builder);

        // Идемпотентност на ниво база: повторното изпращане на същата заявка не създава дубликат.
        builder.HasIndex(d => d.IdempotencyKey).IsUnique();

        // Опашката за одобрение филтрира по статус (по подразбиране Pending), подредена по дата.
        builder.HasIndex(d => new { d.Status, d.DateCreated });

        builder.HasOne(d => d.BankAccount)
            .WithMany()
            .HasForeignKey(d => d.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
