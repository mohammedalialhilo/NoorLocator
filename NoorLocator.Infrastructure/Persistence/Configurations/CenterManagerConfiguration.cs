using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Persistence.Configurations;

public class CenterManagerConfiguration : IEntityTypeConfiguration<CenterManager>
{
    public void Configure(EntityTypeBuilder<CenterManager> builder)
    {
        builder.ToTable("CenterManagers");
        builder.HasKey(centerManager => centerManager.Id);

        builder.HasIndex(centerManager => new { centerManager.UserId, centerManager.CenterId })
            .IsUnique();

        builder.HasOne(centerManager => centerManager.User)
            .WithMany(user => user.ManagedCenters)
            .HasForeignKey(centerManager => centerManager.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(centerManager => centerManager.Center)
            .WithMany(center => center.CenterManagers)
            .HasForeignKey(centerManager => centerManager.CenterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
