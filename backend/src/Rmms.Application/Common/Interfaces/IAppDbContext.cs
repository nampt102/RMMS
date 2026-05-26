using Microsoft.EntityFrameworkCore;

namespace Rmms.Application.Common.Interfaces;

/// <summary>
/// Application-facing abstraction over EF Core <c>DbContext</c>.
/// Lets Application layer issue queries without depending on Infrastructure.
/// DbSet&lt;T&gt; properties are added as entities are introduced.
/// </summary>
public interface IAppDbContext
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    DatabaseFacade Database { get; }
}
