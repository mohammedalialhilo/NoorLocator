using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Persistence.Configurations;

public class CenterRequestConfiguration : IEntityTypeConfiguration<CenterRequest>
{
    public void Configure(EntityTypeBuilder<CenterRequest> builder)
    {
        builder.ToTable("CenterRequests");
        builder.HasKey(centerRequest => centerRequest.Id);

        builder.Property(centerRequest => centerRequest.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(centerRequest => centerRequest.Address)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(centerRequest => centerRequest.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(centerRequest => centerRequest.Country)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(centerRequest => centerRequest.Latitude)
            .HasPrecision(9, 6);

        builder.Property(centerRequest => centerRequest.Longitude)
            .HasPrecision(9, 6);

        builder.Property(centerRequest => centerRequest.Description)
            .HasMaxLength(2000);

        builder.Property(centerRequest => centerRequest.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(centerRequest => centerRequest.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        builder.HasOne(centerRequest => centerRequest.RequestedByUser)
            .WithMany(user => user.CenterRequests)
            .HasForeignKey(centerRequest => centerRequest.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
