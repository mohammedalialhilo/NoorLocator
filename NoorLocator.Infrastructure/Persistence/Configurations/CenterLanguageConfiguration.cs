using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Persistence.Configurations;

public class CenterLanguageConfiguration : IEntityTypeConfiguration<CenterLanguage>
{
    public void Configure(EntityTypeBuilder<CenterLanguage> builder)
    {
        builder.ToTable("CenterLanguages");
        builder.HasKey(centerLanguage => centerLanguage.Id);

        builder.HasIndex(centerLanguage => new { centerLanguage.CenterId, centerLanguage.LanguageId })
            .IsUnique();

        builder.HasOne(centerLanguage => centerLanguage.Center)
            .WithMany(center => center.CenterLanguages)
            .HasForeignKey(centerLanguage => centerLanguage.CenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(centerLanguage => centerLanguage.Language)
            .WithMany(language => language.CenterLanguages)
            .HasForeignKey(centerLanguage => centerLanguage.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
