using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Enums;
using Rmms.Domain.Users;
using Rmms.Infrastructure.Persistence;

namespace Rmms.Api.Cli;

/// <summary>
/// One-shot bootstrap command: create the initial Admin account.
///
/// Usage:
///   dotnet run --project src/Rmms.Api -- seed-admin --email=admin@example.com --password=AdminPwd1 --full-name="System Admin" [--language=vi]
///
/// Idempotent: if an account with the given email already exists, exits 0 with a notice (no overwrite).
///
/// NOTE: audit log is NOT emitted for this bootstrap action — there's no Admin user yet
/// to attribute it to. The first audit entry comes from the next real Admin action.
/// </summary>
public static class SeedAdminCommand
{
    public static async Task<int> RunAsync(IReadOnlyList<string> args, IServiceProvider services)
    {
        var parsed = ParseArgs(args);
        if (parsed is null)
        {
            PrintUsage();
            return 1;
        }

        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();

        var normalizedEmail = parsed.Email.Trim().ToLowerInvariant();

        var exists = await db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == normalizedEmail);

        if (exists)
        {
            Console.WriteLine($"[seed-admin] User '{normalizedEmail}' already exists — skipping.");
            return 0;
        }

        var hash = hasher.Hash(parsed.Password);

        var user = User.CreateByAdmin(
            email: normalizedEmail,
            passwordHash: hash,
            fullName: parsed.FullName,
            role: UserRole.Admin,
            phone: null,
            preferredLanguage: parsed.Language);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        Console.WriteLine($"[seed-admin] OK — admin created.");
        Console.WriteLine($"             id    = {user.Id}");
        Console.WriteLine($"             email = {user.Email}");
        Console.WriteLine($"             role  = admin");
        Console.WriteLine($"             status= {user.Status}");
        Console.WriteLine();
        Console.WriteLine("Please login via POST /api/v1/auth/login and rotate the seed password ASAP.");
        return 0;
    }

    // ─────────────────────────────────────────────────────────────────────────

    private sealed record ParsedArgs(string Email, string Password, string FullName, string Language);

    private static ParsedArgs? ParseArgs(IReadOnlyList<string> args)
    {
        string? email = null, password = null, fullName = null, language = "vi";

        foreach (var raw in args.Skip(1)) // args[0] == "seed-admin"
        {
            var (key, value) = SplitFlag(raw);
            switch (key)
            {
                case "--email": email = value; break;
                case "--password": password = value; break;
                case "--full-name": fullName = value; break;
                case "--language": language = value?.ToLowerInvariant() ?? "vi"; break;
                default:
                    Console.Error.WriteLine($"[seed-admin] Unknown flag: {raw}");
                    return null;
            }
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            Console.Error.WriteLine("[seed-admin] --email is required.");
            return null;
        }
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 ||
            !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            Console.Error.WriteLine("[seed-admin] --password is required, ≥8 chars, with at least 1 letter + 1 digit.");
            return null;
        }
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = "System Admin"; // sensible default
        }
        if (language != "vi" && language != "en")
        {
            Console.Error.WriteLine("[seed-admin] --language must be 'vi' or 'en'.");
            return null;
        }

        return new ParsedArgs(email!, password!, fullName!, language!);
    }

    /// <summary>Split <c>--key=value</c> or <c>--key value</c> (only key=value supported here for simplicity).</summary>
    private static (string Key, string? Value) SplitFlag(string raw)
    {
        var eqIdx = raw.IndexOf('=', StringComparison.Ordinal);
        return eqIdx > 0
            ? (raw[..eqIdx], raw[(eqIdx + 1)..])
            : (raw, null);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project src/Rmms.Api -- seed-admin \\");
        Console.WriteLine("    --email=admin@example.com \\");
        Console.WriteLine("    --password=AdminPwd1 \\");
        Console.WriteLine("    [--full-name=\"System Admin\"] \\");
        Console.WriteLine("    [--language=vi]");
    }
}
