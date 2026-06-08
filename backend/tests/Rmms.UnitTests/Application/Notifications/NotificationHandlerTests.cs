using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Notifications;
using Rmms.Domain.Devices;
using Rmms.Domain.Enums;
using Rmms.Domain.Notifications;
using Rmms.Infrastructure.Notifications;
using Rmms.Infrastructure.Persistence;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Notifications;

public sealed class NotificationHandlerTests
{
    private static NotificationSpec Spec(bool push = true, bool email = false) => new(
        NotificationType.ApprovalNeeded,
        TitleVi: "Cần duyệt", TitleEn: "Approval needed",
        BodyVi: "Bạn có yêu cầu chờ duyệt.", BodyEn: "You have a pending request.",
        Data: new Dictionary<string, string> { ["deepLink"] = "rmms://approvals/x" },
        Push: push, Email: email);

    private static NotificationService Service(AppDbContext db, TestClock clock, FakePushSender push, CapturingEmailSender email) =>
        new(db, push, email, clock, NullLogger<NotificationService>.Instance);

    // ---------- NotificationService (fan-out) ----------

    [Fact]
    public async Task Notify_PersistsInAppRow_AndPushes_WhenActiveDeviceHasToken()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var push = new FakePushSender();
        var user = UserFactory.CreateActivePg();
        db.Users.Add(user);
        db.UserDevices.Add(UserDevice.RegisterFirstActive(
            user.Id, "dev-1", "Pixel", "android", "14", "1.0.0", "fcm-token-abc", clock.UtcNow));
        await db.SaveChangesAsync();

        await Service(db, clock, push, new CapturingEmailSender()).NotifyAsync(user.Id, Spec(push: true));
        await db.SaveChangesAsync();

        var row = db.Notifications.Single(n => n.UserId == user.Id);
        row.Type.Should().Be(NotificationType.ApprovalNeeded);
        row.IsRead.Should().BeFalse();
        row.ChannelsSent.Should().Contain(new[] { "in_app", "push" });
        push.Sent.Should().ContainSingle().Which.DeviceToken.Should().Be("fcm-token-abc");
    }

    [Fact]
    public async Task Notify_InAppOnly_WhenNoActiveDeviceToken()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var push = new FakePushSender();
        var user = UserFactory.CreateActivePg();
        db.Users.Add(user); // no device registered
        await db.SaveChangesAsync();

        await Service(db, clock, push, new CapturingEmailSender()).NotifyAsync(user.Id, Spec(push: true));
        await db.SaveChangesAsync();

        db.Notifications.Single(n => n.UserId == user.Id).ChannelsSent.Should().BeEquivalentTo(new[] { "in_app" });
        push.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Notify_UsesEnglishTitle_WhenUserPrefersEn()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var user = UserFactory.CreateActivePg();
        user.UpdateProfile(preferredLanguage: "en");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await Service(db, clock, new FakePushSender(), new CapturingEmailSender()).NotifyAsync(user.Id, Spec(push: false));
        await db.SaveChangesAsync();

        db.Notifications.Single(n => n.UserId == user.Id).Title.Should().Be("Approval needed");
    }

    // ---------- Queries ----------

    [Fact]
    public async Task GetMyNotifications_ReturnsItems_AndUnreadCount()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var uid = Guid.NewGuid();
        db.Notifications.Add(Notification.Create(uid, NotificationType.RequestApproved, "a", "b", null, new[] { "in_app" }, clock.UtcNow));
        var read = Notification.Create(uid, NotificationType.RequestRejected, "c", "d", null, new[] { "in_app" }, clock.UtcNow);
        read.MarkRead(clock.UtcNow);
        db.Notifications.Add(read);
        db.Notifications.Add(Notification.Create(Guid.NewGuid(), NotificationType.General, "x", "y", null, new[] { "in_app" }, clock.UtcNow)); // other user
        await db.SaveChangesAsync();

        var result = await new GetMyNotificationsQueryHandler(db).Handle(new GetMyNotificationsQuery(uid), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(2);
        result.Value.UnreadCount.Should().Be(1);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task MarkRead_ByOwner_Succeeds()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var uid = Guid.NewGuid();
        var n = Notification.Create(uid, NotificationType.General, "a", "b", null, new[] { "in_app" }, clock.UtcNow);
        db.Notifications.Add(n);
        await db.SaveChangesAsync();

        var result = await new MarkNotificationReadCommandHandler(db, clock).Handle(new MarkNotificationReadCommand(n.Id, uid), default);

        result.IsSuccess.Should().BeTrue();
        db.Notifications.Single(x => x.Id == n.Id).IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkRead_ByStranger_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var n = Notification.Create(Guid.NewGuid(), NotificationType.General, "a", "b", null, new[] { "in_app" }, clock.UtcNow);
        db.Notifications.Add(n);
        await db.SaveChangesAsync();

        var result = await new MarkNotificationReadCommandHandler(db, clock).Handle(new MarkNotificationReadCommand(n.Id, Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.NotFound);
    }

    // ---------- FCM token registration ----------

    [Fact]
    public async Task RegisterFcmToken_UpdatesActiveDevice()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var uid = Guid.NewGuid();
        db.UserDevices.Add(UserDevice.RegisterFirstActive(uid, "dev-1", "Pixel", "android", "14", "1.0.0", null, clock.UtcNow));
        await db.SaveChangesAsync();

        var result = await new RegisterFcmTokenCommandHandler(db).Handle(new RegisterFcmTokenCommand(uid, "new-token"), default);

        result.IsSuccess.Should().BeTrue();
        db.UserDevices.Single(d => d.UserId == uid).FcmToken.Should().Be("new-token");
    }

    [Fact]
    public async Task RegisterFcmToken_NoActiveDevice_NotFound()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await new RegisterFcmTokenCommandHandler(db).Handle(new RegisterFcmTokenCommand(Guid.NewGuid(), "tok"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.NotFound);
    }
}
