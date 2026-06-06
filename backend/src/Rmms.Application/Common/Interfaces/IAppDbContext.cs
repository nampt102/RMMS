using Microsoft.EntityFrameworkCore;
using Rmms.Domain.Audit;
using Rmms.Domain.Auth;
using Rmms.Domain.Devices;
using Rmms.Domain.Attendance;
using Rmms.Domain.Organization;
using Rmms.Domain.Users;
using Rmms.Domain.Scheduling;

namespace Rmms.Application.Common.Interfaces;

/// <summary>
/// Application-facing abstraction over EF Core <c>DbContext</c>.
/// Lets Application layer issue queries without depending on Infrastructure.
/// DbSet&lt;T&gt; properties are added as entities are introduced.
/// </summary>
public interface IAppDbContext
{
    // ----- M01 Identity & Access -----
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<LoginHistory> LoginHistory { get; }
    DbSet<EmailVerificationToken> EmailVerificationTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }

    // ----- M02 Device Management -----
    DbSet<UserDevice> UserDevices { get; }

    // ----- M03 Organization & Assignment -----
    DbSet<Area> Areas { get; }
    DbSet<Store> Stores { get; }
    DbSet<Category> Categories { get; }
    DbSet<UserLeaderAssignment> UserLeaderAssignments { get; }
    DbSet<UserStoreAssignment> UserStoreAssignments { get; }
    DbSet<UserCategoryAssignment> UserCategoryAssignments { get; }

    // ----- M07 Work Schedule (shifts are an owned collection of WorkSchedule) -----
    DbSet<WorkSchedule> WorkSchedules { get; }

    // ----- M05 Attendance -----
    DbSet<AttendanceRecord> AttendanceRecords { get; }

    // ----- Cross-cutting -----
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
