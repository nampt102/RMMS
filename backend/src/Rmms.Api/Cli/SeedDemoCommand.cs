using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Domain.Users;
using Rmms.Infrastructure.Persistence;

namespace Rmms.Api.Cli;

/// <summary>
/// One-shot demo seeding: 1 Admin + 2 Leaders + 5 PGs (all Active and login-ready),
/// plus 3 demo stores and assignments (pg1–3 → leader1, pg4–5 → leader2; PGs mapped to stores).
///
/// Usage:
///   dotnet run --project src/Rmms.Api -- seed-demo [--password=Rmms@2026] [--domain=rmms.local] [--language=vi]
///
/// Idempotent: existing emails are skipped (no overwrite). Admin/Leader use
/// <see cref="User.CreateByAdmin"/>; PGs (which must self-register per BR-101) are
/// created via <see cref="User.Register"/> + immediate email verification so they are Active.
/// </summary>
public static class SeedDemoCommand
{
    private const string DefaultPassword = "Rmms@2026";
    private const string DefaultDomain = "rmms.local";

    private sealed record Person(string Email, string FullName, UserRole Role, string Phone);

    public static async Task<int> RunAsync(IReadOnlyList<string> args, IServiceProvider services)
    {
        var password = DefaultPassword;
        var domain = DefaultDomain;
        var language = "vi";

        foreach (var raw in args.Skip(1)) // args[0] == "seed-demo"
        {
            var eq = raw.IndexOf('=', StringComparison.Ordinal);
            var key = eq > 0 ? raw[..eq] : raw;
            var value = eq > 0 ? raw[(eq + 1)..] : null;
            switch (key)
            {
                case "--password": password = value ?? password; break;
                case "--domain": domain = value ?? domain; break;
                case "--language": language = value?.ToLowerInvariant() ?? language; break;
                default:
                    Console.Error.WriteLine($"[seed-demo] Unknown flag: {raw}");
                    return 1;
            }
        }

        if (password.Length < 8 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            Console.Error.WriteLine("[seed-demo] --password must be ≥8 chars with at least 1 letter + 1 digit.");
            return 1;
        }

        var people = new List<Person>
        {
            new($"admin@{domain}", "System Admin", UserRole.Admin, "0900000001"),
            new($"leader1@{domain}", "Team Lead 1", UserRole.Leader, "0900000011"),
            new($"leader2@{domain}", "Team Lead 2", UserRole.Leader, "0900000012"),
        };
        for (var i = 1; i <= 5; i++)
        {
            people.Add(new($"pg{i}@{domain}", $"PG {i:00}", UserRole.Pg, $"09000001{i:00}"));
        }

        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var clock = sp.GetRequiredService<IDateTimeProvider>();
        var hash = hasher.Hash(password);

        var created = new List<(string Email, string Role, string Status, bool Skipped)>();

        foreach (var p in people)
        {
            var email = p.Email.Trim().ToLowerInvariant();
            var exists = await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email);
            if (exists)
            {
                created.Add((email, p.Role.ToString().ToLowerInvariant(), "—", true));
                continue;
            }

            User user;
            if (p.Role == UserRole.Pg)
            {
                // PGs self-register (BR-101); verify immediately so the demo account is Active.
                user = User.Register(email, hash, p.FullName, p.Phone, language);
                user.VerifyEmail(clock.UtcNow);
            }
            else
            {
                user = User.CreateByAdmin(email, hash, p.FullName, p.Role, p.Phone, language);
            }

            db.Users.Add(user);
            created.Add((email, p.Role.ToString().ToLowerInvariant(), user.Status.ToString(), false));
        }

        await db.SaveChangesAsync();

        // ----- Organization: demo stores + assignments (idempotent) -----
        var storeDefs = new (string Code, string Name, decimal Lat, decimal Lng)[]
        {
            ("ST-001", "Cửa hàng Quận 1", 10.7769m, 106.7009m),
            ("ST-002", "Cửa hàng Quận 3", 10.7838m, 106.6850m),
            ("ST-003", "Cửa hàng Thủ Đức", 10.8494m, 106.7537m),
        };
        var storeReport = new List<(string Code, bool Skipped)>();
        foreach (var s in storeDefs)
        {
            if (await db.Stores.IgnoreQueryFilters().AnyAsync(x => x.Code == s.Code))
            {
                storeReport.Add((s.Code, true));
                continue;
            }
            db.Stores.Add(Store.Create(s.Code, s.Name, null, s.Lat, s.Lng, null));
            storeReport.Add((s.Code, false));
        }
        await db.SaveChangesAsync();

        var today = clock.UtcToday;

        async Task<Guid?> UserIdByEmail(string e) => await db.Users.IgnoreQueryFilters()
            .Where(u => u.Email == e).Select(u => (Guid?)u.Id).FirstOrDefaultAsync();
        async Task<Guid?> StoreIdByCode(string code) => await db.Stores.IgnoreQueryFilters()
            .Where(x => x.Code == code).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

        // pg1–3 → leader1, pg4–5 → leader2 ; PGs spread across the demo stores.
        var leaderMap = new[] { ("pg1", "leader1"), ("pg2", "leader1"), ("pg3", "leader1"), ("pg4", "leader2"), ("pg5", "leader2") };
        var storeMap = new[] { ("pg1", "ST-001"), ("pg2", "ST-001"), ("pg3", "ST-002"), ("pg4", "ST-002"), ("pg5", "ST-003") };

        var assignReport = new List<string>();
        foreach (var (pg, leader) in leaderMap)
        {
            var pgId = await UserIdByEmail($"{pg}@{domain}");
            var leaderId = await UserIdByEmail($"{leader}@{domain}");
            if (pgId is null || leaderId is null) continue;
            if (await db.UserLeaderAssignments.AnyAsync(a => a.PgUserId == pgId && a.EffectiveTo == null))
            {
                assignReport.Add($"{pg} → {leader} (skipped: active exists)");
                continue;
            }
            db.UserLeaderAssignments.Add(UserLeaderAssignment.Create(pgId.Value, leaderId.Value, today));
            assignReport.Add($"{pg} → {leader}");
        }
        foreach (var (pg, store) in storeMap)
        {
            var pgId = await UserIdByEmail($"{pg}@{domain}");
            var storeId = await StoreIdByCode(store);
            if (pgId is null || storeId is null) continue;
            if (await db.UserStoreAssignments.AnyAsync(a => a.UserId == pgId && a.StoreId == storeId && a.EffectiveTo == null))
            {
                assignReport.Add($"{pg} @ {store} (skipped)");
                continue;
            }
            db.UserStoreAssignments.Add(UserStoreAssignment.Create(pgId.Value, storeId.Value, today));
            assignReport.Add($"{pg} @ {store}");
        }
        await db.SaveChangesAsync();

        Console.WriteLine("[seed-demo] Done.");
        Console.WriteLine($"            shared password = {password}");
        Console.WriteLine($"            {"email",-28} {"role",-8} {"status",-10} note");
        foreach (var c in created)
        {
            Console.WriteLine($"            {c.Email,-28} {c.Role,-8} {c.Status,-10} {(c.Skipped ? "already existed — skipped" : "created")}");
        }
        Console.WriteLine();
        Console.WriteLine("            stores:");
        foreach (var s in storeReport)
        {
            Console.WriteLine($"              {s.Code} {(s.Skipped ? "(skipped)" : "(created)")}");
        }
        Console.WriteLine("            assignments:");
        foreach (var a in assignReport)
        {
            Console.WriteLine($"              {a}");
        }
        Console.WriteLine();
        Console.WriteLine("Login via POST /api/v1/auth/login and rotate the seed password ASAP.");
        return 0;
    }
}
