using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Persistence.Configurations;

public class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("Languages");
        builder.HasKey(language => language.Id);

        builder.Property(language => language.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(language => language.Code)
            .HasMaxLength(10)
            .IsRequired();

        builder.HasIndex(language => language.Code)
            .IsUnique();
    }
}
