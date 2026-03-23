using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Persistence.Configurations;

public class CenterLanguageSuggestionConfiguration : IEntityTypeConfiguration<CenterLanguageSuggestion>
{
    public void Configure(EntityTypeBuilder<CenterLanguageSuggestion> builder)
    {
        builder.ToTable("CenterLanguageSuggestions");
        builder.HasKey(centerLanguageSuggestion => centerLanguageSuggestion.Id);

        builder.Property(centerLanguageSuggestion => centerLanguageSuggestion.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasOne(centerLanguageSuggestion => centerLanguageSuggestion.Center)
            .WithMany(center => center.CenterLanguageSuggestions)
            .HasForeignKey(centerLanguageSuggestion => centerLanguageSuggestion.CenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(centerLanguageSuggestion => centerLanguageSuggestion.Language)
            .WithMany(language => language.CenterLanguageSuggestions)
            .HasForeignKey(centerLanguageSuggestion => centerLanguageSuggestion.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(centerLanguageSuggestion => centerLanguageSuggestion.SuggestedByUser)
            .WithMany(user => user.CenterLanguageSuggestions)
            .HasForeignKey(centerLanguageSuggestion => centerLanguageSuggestion.SuggestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
