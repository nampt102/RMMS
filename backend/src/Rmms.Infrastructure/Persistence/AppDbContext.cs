using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Audit;
using Rmms.Domain.Auth;
using Rmms.Domain.Devices;
using Rmms.Domain.Users;

namespace Rmms.Infrastructure.Persistence;

/// <summary>
/// Root EF Core DbContext for RMMS.
/// Entities are added per-module as we implement M01..M16.
/// Naming: snake_case tables/columns via EFCore.NamingConventions (configured in DI).
/// </summary>
public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ----- M01 Identity & Access -----
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LoginHistory> LoginHistory => Set<LoginHistory>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    // ----- M02 Device Management -----
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();

    // ----- Cross-cutting -----
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> from this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
