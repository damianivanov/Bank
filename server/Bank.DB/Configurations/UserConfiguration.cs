using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.DB.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(user => user.FirstName).HasMaxLength(100);
        builder.Property(user => user.LastName).HasMaxLength(100);
        builder.Property(user => user.AvatarUrl).HasMaxLength(500);
        builder.Property(user => user.DateCreated).IsRequired();

        builder
            .HasMany(user => user.UserRoles)
            .WithOne(userRole => userRole.User)
            .HasForeignKey(userRole => userRole.UserId)
            .IsRequired();
    }
}
