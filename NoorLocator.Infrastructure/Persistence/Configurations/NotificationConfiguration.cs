using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(notification => notification.Message)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(notification => notification.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(notification => notification.RelatedEntityType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(notification => notification.LinkUrl)
            .HasMaxLength(512);

        builder.Property(notification => notification.DeduplicationKey)
            .HasMaxLength(160)
            .IsRequired();

        builder.HasIndex(notification => new { notification.UserId, notification.IsRead });
        builder.HasIndex(notification => new { notification.UserId, notification.DeduplicationKey })
            .IsUnique();

        builder.Property(notification => notification.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        builder.HasOne(notification => notification.User)
            .WithMany(user => user.Notifications)
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
