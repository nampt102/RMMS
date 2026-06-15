using FluentAssertions;
using Rmms.Application.Reports;
using Rmms.Domain.Enums;
using Rmms.Domain.LeaveOt;
using Rmms.Infrastructure.Reports;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Reports;

public sealed class RequestReportHandlerTests
{
    [Fact]
    public async Task ExportLeaveRequests_ProducesXlsx_WithRows()
    {
        await using var db = TestDbContextFactory.Create();
        var user = UserFactory.CreateActivePg("leave@example.com");
        db.Users.Add(user);
        db.LeaveRequests.Add(LeaveRequest.Create(
            user.Id, LeaveType.Regular, new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 11), null, null, "Nghỉ phép"));
        await db.SaveChangesAsync();

        var res = await new ExportLeaveRequestsCommandHandler(db, new ClosedXmlReportExporter())
            .Handle(new ExportLeaveRequestsCommand(Status: null), default);

        res.IsSuccess.Should().BeTrue();
        res.Value.Content.Length.Should().BeGreaterThan(0);
        res.Value.FileName.Should().Be("LeaveRequests.xlsx");
        res.Value.ContentType.Should().Contain("spreadsheetml");
    }

    [Fact]
    public async Task ExportOtRequests_FiltersByStatus()
    {
        await using var db = TestDbContextFactory.Create();
        var user = UserFactory.CreateActivePg("ot@example.com");
        db.Users.Add(user);
        db.OtRequests.Add(OtRequest.Create(
            user.Id, new DateOnly(2026, 6, 10), new TimeOnly(18, 0), new TimeOnly(20, 0), "Tăng ca"));
        await db.SaveChangesAsync();

        // No 'approved' rows yet → still produces a valid (empty-body) workbook.
        var res = await new ExportOtRequestsCommandHandler(db, new ClosedXmlReportExporter())
            .Handle(new ExportOtRequestsCommand(Status: "approved"), default);

        res.IsSuccess.Should().BeTrue();
        res.Value.Content.Length.Should().BeGreaterThan(0);
    }
}
