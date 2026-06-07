using FluentAssertions;
using Rmms.Application.Audit;
using Rmms.Domain.Audit;
using Rmms.Domain.Enums;
using Rmms.Infrastructure.Persistence;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Audit;

public sealed class GetAuditLogsQueryTests
{
    private static AuditLog Row(string action, DateTimeOffset at) =>
        AuditLog.Record(action, "user", Guid.NewGuid(), Guid.NewGuid(), null, "ua", "{}", at);

    [Fact]
    public async Task FiltersByAction_AndOrdersNewestFirst_AndPaginates()
    {
        await using var db = TestDbContextFactory.Create();
        var t0 = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        db.AuditLogs.Add(Row(AuditAction.UserRegistered, t0));
        db.AuditLogs.Add(Row(AuditAction.ApprovalApproved, t0.AddMinutes(5)));
        db.AuditLogs.Add(Row(AuditAction.ApprovalApproved, t0.AddMinutes(10)));
        await db.SaveChangesAsync();

        var all = await new GetAuditLogsQueryHandler(db)
            .Handle(new GetAuditLogsQuery(null, null, null, null, null, 1, 20), default);
        all.Value.Meta.Total.Should().Be(3);
        all.Value.Data[0].CreatedAt.Should().Be(t0.AddMinutes(10)); // newest first

        var filtered = await new GetAuditLogsQueryHandler(db)
            .Handle(new GetAuditLogsQuery(AuditAction.ApprovalApproved, null, null, null, null, 1, 20), default);
        filtered.Value.Meta.Total.Should().Be(2);
        filtered.Value.Data.Should().OnlyContain(x => x.Action == AuditAction.ApprovalApproved);
    }
}
