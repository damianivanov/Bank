using Bank.DB.Configurations.Base;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class TokenConfiguration : BaseConfiguration<Token>
{
    public override void Configure(EntityTypeBuilder<Token> builder)
    {
        base.Configure(builder);

        builder.HasIndex(token => token.Value).IsUnique();
        builder.HasOne(token => token.User)
                .WithMany(user => user.Tokens)
                .HasForeignKey(token => token.UserId);
    }
}
