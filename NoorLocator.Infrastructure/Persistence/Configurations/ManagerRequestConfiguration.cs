using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Persistence.Configurations;

public class ManagerRequestConfiguration : IEntityTypeConfiguration<ManagerRequest>
{
    public void Configure(EntityTypeBuilder<ManagerRequest> builder)
    {
        builder.ToTable("ManagerRequests");
        builder.HasKey(managerRequest => managerRequest.Id);

        builder.Property(managerRequest => managerRequest.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(managerRequest => managerRequest.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        builder.HasOne(managerRequest => managerRequest.User)
            .WithMany(user => user.ManagerRequests)
            .HasForeignKey(managerRequest => managerRequest.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(managerRequest => managerRequest.Center)
            .WithMany(center => center.ManagerRequests)
            .HasForeignKey(managerRequest => managerRequest.CenterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
