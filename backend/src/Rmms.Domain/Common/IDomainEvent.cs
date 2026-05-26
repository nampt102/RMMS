namespace Rmms.Domain.Common;

/// <summary>
/// Marker interface for domain events. Raised inside aggregates,
/// captured by EF Core SaveChangesInterceptor, written to outbox, then dispatched.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}
