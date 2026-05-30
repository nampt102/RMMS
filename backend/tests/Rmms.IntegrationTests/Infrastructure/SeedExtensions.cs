using Microsoft.Extensions.DependencyInjection;
using Rmms.Application.Common.Abstractions;
using Rmms.Domain.Enums;
using Rmms.Domain.Users;
using Rmms.Infrastructure.Persistence;

namespace Rmms.IntegrationTests.Infrastructure;

public static class SeedExtensions
{
    /// <summary>
    /// Seeds an admin-created (pre-verified, Active) user directly in the DB and returns the
    /// plaintext password. Use for Leader / BUH / Admin; PG must go through register().
    /// </summary>
    public static async Task<(Guid Id, string Email, string Password)> SeedUserAsync(
        this RmmsApiFactory factory, UserRole role, string password = "Passw0rd1")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var email = $"seed.{role.ToString().ToLowerInvariant()}.{Guid.NewGuid():N}@example.com";
        var user = User.CreateByAdmin(email, hasher.Hash(password), $"Seed {role}", role);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return (user.Id, email, password);
    }
}
