using Bank.DB.Configurations.Base;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class CreditTermsConfiguration : BaseConfiguration<CreditTerms>
{
    public override void Configure(EntityTypeBuilder<CreditTerms> builder)
    {
        base.Configure(builder);

        // Текущата версия на условията се чете често по (CreditId, IsCurrent); индексът я държи бърза.
        builder.HasIndex(terms => new { terms.CreditId, terms.IsCurrent });

        builder.HasOne(terms => terms.Credit)
            .WithMany(credit => credit.Terms)
            .HasForeignKey(terms => terms.CreditId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
