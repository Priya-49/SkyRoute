using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkyRoute.Domain.Entities;

namespace SkyRoute.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Property(user => user.Email).IsRequired().HasMaxLength(320);
        builder.Property(user => user.PasswordHash).IsRequired().HasMaxLength(500);
        builder.Property(user => user.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(user => user.LastName).IsRequired().HasMaxLength(100);
        builder.Property(user => user.CreatedAt).IsRequired();

        builder.HasIndex(user => user.Email).IsUnique();
    }
}
