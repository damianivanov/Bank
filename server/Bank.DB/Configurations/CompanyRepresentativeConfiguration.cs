using Bank.DB.Configurations.Base;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class CompanyRepresentativeConfiguration : BaseConfiguration<CompanyRepresentative>
{
    public override void Configure(EntityTypeBuilder<CompanyRepresentative> builder)
    {
        base.Configure(builder);

        builder.HasOne(r => r.Person)
            .WithMany(p => p.Representations)
            .HasForeignKey(r => r.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Company)
            .WithMany(c => c.Representatives)
            .HasForeignKey(r => r.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.CompanyId, r.PersonId, r.Role })
            .IsUnique();
    }
}
