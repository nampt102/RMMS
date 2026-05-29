namespace Rmms.Domain.Common;

/// <summary>
/// Marker interface for aggregate roots — top-level domain objects whose
/// lifecycle is managed independently (Cross-module access goes through the root).
///
/// Per <c>knowledge-base/08-coding-standards.md</c> — Domain layer rules:
/// only Aggregate Roots get repositories; sub-entities are reached via the root.
/// </summary>
public interface IAggregateRoot
{
}
