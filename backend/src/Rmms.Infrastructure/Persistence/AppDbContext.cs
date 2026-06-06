using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Approvals;
using Rmms.Domain.Audit;
using Rmms.Domain.Auth;
using Rmms.Domain.Devices;
using Rmms.Domain.Attendance;
using Rmms.Domain.Organization;
using Rmms.Domain.Users;
using Rmms.Domain.Scheduling;

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

    // ----- M03 Organization & Assignment -----
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<UserLeaderAssignment> UserLeaderAssignments => Set<UserLeaderAssignment>();
    public DbSet<UserStoreAssignment> UserStoreAssignments => Set<UserStoreAssignment>();
    public DbSet<UserCategoryAssignment> UserCategoryAssignments => Set<UserCategoryAssignment>();

    // ----- M07 Work Schedule (shifts are an owned collection of WorkSchedule) -----
    public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();

    // ----- M09 Approval Workflow -----
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<ApprovalEmailToken> ApprovalEmailTokens => Set<ApprovalEmailToken>();

    // ----- M05 Attendance -----
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    // ----- Cross-cutting -----
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> from this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
