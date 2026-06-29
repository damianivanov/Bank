using Bank.DB.Configurations.Base;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class SavedCalculationConfiguration : BaseConfiguration<SavedCalculation>
{
    public override void Configure(EntityTypeBuilder<SavedCalculation> builder)
    {
        base.Configure(builder);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Без ограничение -> nvarchar(max). Четем го само цялостно per ред, никога не правим заявки в него.
        builder.Property(c => c.InputsJson)
            .IsRequired();

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.UserId);
    }
}
