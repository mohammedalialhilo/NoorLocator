using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Persistence.Configurations;

public class UserCenterSubscriptionConfiguration : IEntityTypeConfiguration<UserCenterSubscription>
{
    public void Configure(EntityTypeBuilder<UserCenterSubscription> builder)
    {
        builder.ToTable("UserCenterSubscriptions");
        builder.HasKey(subscription => subscription.Id);

        builder.HasIndex(subscription => new { subscription.UserId, subscription.CenterId })
            .IsUnique();

        builder.Property(subscription => subscription.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        builder.HasOne(subscription => subscription.User)
            .WithMany(user => user.CenterSubscriptions)
            .HasForeignKey(subscription => subscription.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(subscription => subscription.Center)
            .WithMany(center => center.UserSubscriptions)
            .HasForeignKey(subscription => subscription.CenterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
