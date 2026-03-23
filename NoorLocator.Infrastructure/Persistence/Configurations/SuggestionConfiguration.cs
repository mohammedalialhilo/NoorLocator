using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Persistence.Configurations;

public class SuggestionConfiguration : IEntityTypeConfiguration<Suggestion>
{
    public void Configure(EntityTypeBuilder<Suggestion> builder)
    {
        builder.ToTable("Suggestions");
        builder.HasKey(suggestion => suggestion.Id);

        builder.Property(suggestion => suggestion.Message)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(suggestion => suggestion.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(suggestion => suggestion.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(suggestion => suggestion.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(suggestion => suggestion.User)
            .WithMany(user => user.Suggestions)
            .HasForeignKey(suggestion => suggestion.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
