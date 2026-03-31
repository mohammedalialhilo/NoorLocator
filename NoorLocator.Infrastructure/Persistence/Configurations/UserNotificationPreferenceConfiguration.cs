using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Persistence.Configurations;

public class UserNotificationPreferenceConfiguration : IEntityTypeConfiguration<UserNotificationPreference>
{
    public void Configure(EntityTypeBuilder<UserNotificationPreference> builder)
    {
        builder.ToTable("UserNotificationPreferences");
        builder.HasKey(preference => preference.UserId);

        builder.Property(preference => preference.CreatedAtUtc)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        builder.HasOne(preference => preference.User)
            .WithOne(user => user.NotificationPreference)
            .HasForeignKey<UserNotificationPreference>(preference => preference.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
