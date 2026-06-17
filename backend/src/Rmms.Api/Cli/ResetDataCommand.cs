using Microsoft.EntityFrameworkCore;
using Rmms.Infrastructure.Persistence;

namespace Rmms.Api.Cli;

/// <summary>
/// DESTRUCTIVE one-shot: wipe ALL data from every table, keeping only the admin/super-admin
/// accounts in <c>users</c> (everything else — PGs, Leaders, BUH, stores, products, schedules,
/// attendance, forms, submissions, notifications, audit, …). Used to get a clean slate before
/// re-seeding master data for a demo / pilot.
///
/// Usage:
///   dotnet Rmms.Api.dll reset-data --confirm
///
/// Keeps: users WHERE role = 'admin' (i.e. the super-admin + the system admin). Deletes every
/// other user and TRUNCATEs all other tables (RESTART IDENTITY CASCADE).
///
/// NOTE: <c>audit_log</c> is append-only — its UPDATE/DELETE is REVOKEd for the non-superuser app
/// role in prod, so truncating it there fails (tolerated + reported). In Dev/CI the app runs as a
/// superuser, so it is wiped too. To force-clear audit in prod, run the TRUNCATE as the DB owner.
/// </summary>
public static class ResetDataCommand
{
    public static async Task<int> RunAsync(IReadOnlyList<string> args, IServiceProvider services)
    {
        if (!args.Skip(1).Any(a => a.Equals("--confirm", StringComparison.OrdinalIgnoreCase)))
        {
            Console.Error.WriteLine("[reset-data] Refusing to wipe without confirmation.");
            Console.Error.WriteLine("             This DELETES all data except admin/super-admin users.");
            Console.Error.WriteLine("             Re-run with:  dotnet Rmms.Api.dll reset-data --confirm");
            return 1;
        }

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Discover every table dynamically (robust to schema changes); keep users + migration history.
        var tables = await db.Database
            .SqlQueryRaw<string>(
                "SELECT tablename AS \"Value\" FROM pg_tables WHERE schemaname = 'public' " +
                "AND tablename NOT IN ('users', '__EFMigrationsHistory') ORDER BY tablename")
            .ToListAsync();

        var wiped = new List<string>();
        var failed = new List<(string Table, string Error)>();

        foreach (var table in tables)
        {
            try
            {
                // Table names come from pg_tables (not user input). Build a plain (non-interpolated)
                // string so EF treats it as raw SQL rather than a parameterised template (EF1002).
                var sql = "TRUNCATE TABLE \"" + table + "\" RESTART IDENTITY CASCADE";
                await db.Database.ExecuteSqlRawAsync(sql);
                wiped.Add(table);
            }
            catch (Exception ex)
            {
                failed.Add((table, ex.Message.Split('\n')[0]));
            }
        }

        int deletedUsers;
        try
        {
            deletedUsers = await db.Database.ExecuteSqlRawAsync("DELETE FROM users WHERE role <> 'admin'");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[reset-data] FAILED deleting non-admin users: {ex.Message.Split('\n')[0]}");
            return 2;
        }

        var keptAdmins = await db.Users.IgnoreQueryFilters().CountAsync();

        Console.WriteLine("[reset-data] Done.");
        Console.WriteLine($"             tables truncated : {wiped.Count}");
        Console.WriteLine($"             non-admin users deleted : {deletedUsers}");
        Console.WriteLine($"             admin/super users kept  : {keptAdmins}");
        if (failed.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("             NOT cleared (likely revoked permission — run as DB owner if needed):");
            foreach (var f in failed)
            {
                Console.WriteLine($"               - {f.Table}: {f.Error}");
            }
        }
        return 0;
    }
}
