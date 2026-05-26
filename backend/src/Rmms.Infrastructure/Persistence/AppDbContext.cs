using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Rmms.Application.Common.Interfaces;

namespace Rmms.Infrastructure.Persistence;

/// <summary>
/// Root EF Core DbContext for RMMS.
/// Entities are added per-module as we implement M01..M16.
/// Naming: snake_case tables/columns via EFCore.NamingConventions (configured in DI).
/// </summary>
public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Add DbSet<T> here as entities are introduced.
    // Example: public DbSet<User> Users => Set<User>();

    DatabaseFacade IAppDbContext.Database => Database;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> from this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
