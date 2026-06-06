using Rmms.Application.Common.Abstractions;
using Rmms.Domain.Enums;

namespace Rmms.UnitTests.Common;

/// <summary>Records <see cref="IApprovalService"/> calls for assertions (M09 producer wiring).</summary>
internal sealed class FakeApprovalService : IApprovalService
{
    public sealed record Call(ApprovalEntityType EntityType, Guid EntityId, Guid RequesterId, Guid ApproverId, UserRole ApproverRole);

    public List<Call> Calls { get; } = new();

    public Task<Guid> CreateAsync(
        ApprovalEntityType entityType,
        Guid entityId,
        Guid requesterId,
        Guid approverId,
        UserRole approverRole,
        CancellationToken ct = default)
    {
        Calls.Add(new Call(entityType, entityId, requesterId, approverId, approverRole));
        return Task.FromResult(Guid.NewGuid());
    }
}
