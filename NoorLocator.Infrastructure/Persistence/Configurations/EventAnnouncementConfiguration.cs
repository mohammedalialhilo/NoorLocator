using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Persistence.Configurations;

public class EventAnnouncementConfiguration : IEntityTypeConfiguration<EventAnnouncement>
{
    public void Configure(EntityTypeBuilder<EventAnnouncement> builder)
    {
        builder.ToTable("EventAnnouncements");
        builder.HasKey(eventAnnouncement => eventAnnouncement.Id);

        builder.Property(eventAnnouncement => eventAnnouncement.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(eventAnnouncement => eventAnnouncement.Description)
            .HasMaxLength(4000);

        builder.Property(eventAnnouncement => eventAnnouncement.ImageUrl)
            .HasMaxLength(512);

        builder.Property(eventAnnouncement => eventAnnouncement.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(eventAnnouncement => eventAnnouncement.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        builder.HasIndex(eventAnnouncement => new
        {
            eventAnnouncement.CenterId,
            eventAnnouncement.Status,
            eventAnnouncement.CreatedAt
        });

        builder.HasOne(eventAnnouncement => eventAnnouncement.Center)
            .WithMany(center => center.EventAnnouncements)
            .HasForeignKey(eventAnnouncement => eventAnnouncement.CenterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(eventAnnouncement => eventAnnouncement.CreatedByManager)
            .WithMany(user => user.EventAnnouncements)
            .HasForeignKey(eventAnnouncement => eventAnnouncement.CreatedByManagerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
