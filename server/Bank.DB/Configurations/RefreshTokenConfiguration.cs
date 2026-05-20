using Bank.DB.Configurations.Base;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class RefreshTokenConfiguration : BaseConfiguration<RefreshToken>
{
    public override void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        base.Configure(builder);

        builder.Property(token => token.Value).HasMaxLength(2048).IsRequired();
        builder.HasIndex(token => token.Value).IsUnique();
        builder.Ignore(token => token.IsRevoked);
        builder.HasOne(token => token.User).WithMany(user => user.RefreshTokens).HasForeignKey(token => token.UserId);
    }
}
