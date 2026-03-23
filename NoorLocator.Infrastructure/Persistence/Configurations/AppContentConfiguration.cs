using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Persistence.Configurations;

public class AppContentConfiguration : IEntityTypeConfiguration<AppContent>
{
    public void Configure(EntityTypeBuilder<AppContent> builder)
    {
        builder.ToTable("AppContents");
        builder.HasKey(appContent => appContent.Id);

        builder.Property(appContent => appContent.Key)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(appContent => appContent.Value)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(appContent => appContent.LanguageCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.HasIndex(appContent => new
        {
            appContent.Key,
            appContent.LanguageCode
        }).IsUnique();
    }
}
