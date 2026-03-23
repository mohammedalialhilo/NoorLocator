using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Persistence.Configurations;

public class CenterConfiguration : IEntityTypeConfiguration<Center>
{
    public void Configure(EntityTypeBuilder<Center> builder)
    {
        builder.ToTable("Centers");
        builder.HasKey(center => center.Id);

        builder.Property(center => center.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(center => center.Address)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(center => center.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(center => center.Country)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(center => center.Latitude)
            .HasPrecision(9, 6);

        builder.Property(center => center.Longitude)
            .HasPrecision(9, 6);

        builder.Property(center => center.Description)
            .HasMaxLength(2000);

        builder.Property(center => center.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
