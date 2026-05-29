using Microsoft.EntityFrameworkCore;
using Rmms.Domain.Audit;
using Rmms.Domain.Auth;
using Rmms.Domain.Devices;
using Rmms.Domain.Users;

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

    // ----- Cross-cutting -----
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
