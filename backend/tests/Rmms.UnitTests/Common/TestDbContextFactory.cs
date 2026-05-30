using Microsoft.EntityFrameworkCore;
using Rmms.Infrastructure.Persistence;

namespace Rmms.UnitTests.Common;

/// <summary>
/// Builds a fresh <see cref="AppDbContext"/> on the EF Core InMemory provider for each test.
///
/// Limitations vs real Postgres:
///   - No real unique-constraint enforcement (tests must verify uniqueness logic explicitly).
///   - No ILIKE / case-insensitive collation — tests for search filter may need lowercase fixtures.
///   - Transactions are no-op.
/// These limits are acceptable for handler unit tests; integration tests (Day 9) use Testcontainers.
/// </summary>
internal static class TestDbContextFactory
{
    public static AppDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            // EF InMemory does not understand global filters with `IsDeleted` predicates that
            // are translated to SQL — they still apply in memory, so soft-delete tests work.
            .EnableSensitiveDataLogging()
            .Options;

        return new AppDbContext(options);
    }
}
