using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Persistence.Configurations;

public class MajlisConfiguration : IEntityTypeConfiguration<Majlis>
{
    public void Configure(EntityTypeBuilder<Majlis> builder)
    {
        builder.ToTable("Majalis");
        builder.HasKey(majlis => majlis.Id);

        builder.Property(majlis => majlis.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(majlis => majlis.Description)
            .HasMaxLength(2000);

        builder.Property(majlis => majlis.Time)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(majlis => majlis.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        builder.HasOne(majlis => majlis.Center)
            .WithMany(center => center.Majalis)
            .HasForeignKey(majlis => majlis.CenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(majlis => majlis.CreatedByManager)
            .WithMany(user => user.CreatedMajalis)
            .HasForeignKey(majlis => majlis.CreatedByManagerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
