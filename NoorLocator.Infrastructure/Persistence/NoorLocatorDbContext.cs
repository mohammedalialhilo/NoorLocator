using Microsoft.EntityFrameworkCore;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Persistence;

public class NoorLocatorDbContext(DbContextOptions<NoorLocatorDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Center> Centers => Set<Center>();

    public DbSet<CenterRequest> CenterRequests => Set<CenterRequest>();

    public DbSet<CenterManager> CenterManagers => Set<CenterManager>();

    public DbSet<Majlis> Majalis => Set<Majlis>();

    public DbSet<Language> Languages => Set<Language>();

    public DbSet<MajlisLanguage> MajlisLanguages => Set<MajlisLanguage>();

    public DbSet<CenterLanguage> CenterLanguages => Set<CenterLanguage>();

    public DbSet<CenterLanguageSuggestion> CenterLanguageSuggestions => Set<CenterLanguageSuggestion>();

    public DbSet<Suggestion> Suggestions => Set<Suggestion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NoorLocatorDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
