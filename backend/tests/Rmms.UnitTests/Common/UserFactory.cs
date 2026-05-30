using Rmms.Domain.Enums;
using Rmms.Domain.Users;

namespace Rmms.UnitTests.Common;

internal static class UserFactory
{
    public static User CreatePgPendingVerify(string email = "pg1@example.com", string passwordHash = "plain:Test1234")
    {
        return User.Register(
            email: email,
            passwordHash: passwordHash,
            fullName: "PG Test",
            phone: "0901234567",
            preferredLanguage: "vi");
    }

    public static User CreateActivePg(string email = "pg1@example.com", string passwordHash = "plain:Test1234")
    {
        var u = CreatePgPendingVerify(email, passwordHash);
        u.VerifyEmail(DateTimeOffset.UtcNow);
        return u;
    }

    public static User CreateAdmin(string email = "admin@example.com")
    {
        return User.CreateByAdmin(
            email: email,
            passwordHash: "plain:AdminPwd1",
            fullName: "System Admin",
            role: UserRole.Admin,
            phone: null,
            preferredLanguage: "vi");
    }

    public static User CreateInactivePg(string email = "inactive@example.com")
    {
        var u = CreateActivePg(email);
        u.Deactivate();
        return u;
    }
}
