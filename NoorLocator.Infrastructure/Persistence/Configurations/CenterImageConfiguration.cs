using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Persistence.Configurations;

public class CenterImageConfiguration : IEntityTypeConfiguration<CenterImage>
{
    public void Configure(EntityTypeBuilder<CenterImage> builder)
    {
        builder.ToTable("CenterImages");
        builder.HasKey(centerImage => centerImage.Id);

        builder.Property(centerImage => centerImage.ImageUrl)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(centerImage => centerImage.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        builder.HasIndex(centerImage => new
        {
            centerImage.CenterId,
            centerImage.IsPrimary
        });

        builder.HasIndex(centerImage => new
        {
            centerImage.CenterId,
            centerImage.CreatedAt
        });

        builder.HasOne(centerImage => centerImage.Center)
            .WithMany(center => center.CenterImages)
            .HasForeignKey(centerImage => centerImage.CenterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(centerImage => centerImage.UploadedByManager)
            .WithMany(user => user.UploadedCenterImages)
            .HasForeignKey(centerImage => centerImage.UploadedByManagerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
