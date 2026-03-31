using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Persistence.Configurations;

public class UserCenterVisitConfiguration : IEntityTypeConfiguration<UserCenterVisit>
{
    public void Configure(EntityTypeBuilder<UserCenterVisit> builder)
    {
        builder.ToTable("UserCenterVisits");
        builder.HasKey(visit => visit.Id);

        builder.Property(visit => visit.Source)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(visit => visit.VisitedAtUtc)
            .IsRequired();

        builder.HasIndex(visit => new { visit.UserId, visit.CenterId })
            .IsUnique();

        builder.Property(visit => visit.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        builder.HasOne(visit => visit.User)
            .WithMany(user => user.CenterVisits)
            .HasForeignKey(visit => visit.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(visit => visit.Center)
            .WithMany(center => center.UserVisits)
            .HasForeignKey(visit => visit.CenterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
