using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Persistence.Configurations;

public class MajlisLanguageConfiguration : IEntityTypeConfiguration<MajlisLanguage>
{
    public void Configure(EntityTypeBuilder<MajlisLanguage> builder)
    {
        builder.ToTable("MajlisLanguages");
        builder.HasKey(majlisLanguage => majlisLanguage.Id);

        builder.HasIndex(majlisLanguage => new { majlisLanguage.MajlisId, majlisLanguage.LanguageId })
            .IsUnique();

        builder.HasOne(majlisLanguage => majlisLanguage.Majlis)
            .WithMany(majlis => majlis.MajlisLanguages)
            .HasForeignKey(majlisLanguage => majlisLanguage.MajlisId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(majlisLanguage => majlisLanguage.Language)
            .WithMany(language => language.MajlisLanguages)
            .HasForeignKey(majlisLanguage => majlisLanguage.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
