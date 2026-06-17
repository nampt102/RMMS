using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Users;
using Rmms.Infrastructure.Persistence;

namespace Rmms.Api.Cli;

/// <summary>
/// One-shot bootstrap: create (or promote) the hidden super-admin account.
///
/// Usage:
///   dotnet Rmms.Api.dll seed-superadmin --email=root@example.com [--full-name="Super Admin"] [--language=vi] [--password=...]
///
/// A super-admin has full Admin rights but is invisible to every other user (only another
/// super-admin can list/manage it). If <c>--password</c> is omitted a strong random one is
/// generated and printed ONCE. If the email already exists the account is promoted in place
/// (password left unchanged).
/// </summary>
public static class SeedSuperAdminCommand
{
    public static async Task<int> RunAsync(IReadOnlyList<string> args, IServiceProvider services)
    {
        var parsed = ParseArgs(args);
        if (parsed is null)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet Rmms.Api.dll seed-superadmin --email=root@example.com \\");
            Console.WriteLine("    [--full-name=\"Super Admin\"] [--language=vi] [--password=Strong1!]");
            return 1;
        }

        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();

        var email = parsed.Email.Trim().ToLowerInvariant();

        var existing = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email);
        if (existing is not null)
        {
            if (existing.IsSuperAdmin)
            {
                Console.WriteLine($"[seed-superadmin] '{email}' is already a super-admin — nothing to do.");
                return 0;
            }
            existing.PromoteToSuperAdmin();
            await db.SaveChangesAsync();
            Console.WriteLine($"[seed-superadmin] OK — existing account '{email}' promoted to super-admin (password unchanged).");
            Console.WriteLine($"                  id = {existing.Id}");
            return 0;
        }

        var password = parsed.Password ?? GeneratePassword();
        var user = User.CreateSuperAdmin(email, hasher.Hash(password), parsed.FullName, parsed.Language);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        Console.WriteLine("[seed-superadmin] OK — super-admin created (hidden from all other users).");
        Console.WriteLine($"                  id       = {user.Id}");
        Console.WriteLine($"                  email    = {user.Email}");
        Console.WriteLine($"                  password = {password}");
        Console.WriteLine();
        Console.WriteLine("Store the password in a secret manager and rotate it after first login.");
        return 0;
    }

    private sealed record ParsedArgs(string Email, string? Password, string FullName, string Language);

    private static ParsedArgs? ParseArgs(IReadOnlyList<string> args)
    {
        string? email = null, password = null, fullName = "Super Admin", language = "vi";

        foreach (var raw in args.Skip(1)) // args[0] == "seed-superadmin"
        {
            var eq = raw.IndexOf('=', StringComparison.Ordinal);
            var key = eq > 0 ? raw[..eq] : raw;
            var value = eq > 0 ? raw[(eq + 1)..] : null;
            switch (key)
            {
                case "--email": email = value; break;
                case "--password": password = value; break;
                case "--full-name": fullName = value; break;
                case "--language": language = value?.ToLowerInvariant() ?? "vi"; break;
                default:
                    Console.Error.WriteLine($"[seed-superadmin] Unknown flag: {raw}");
                    return null;
            }
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            Console.Error.WriteLine("[seed-superadmin] --email is required.");
            return null;
        }
        if (password is not null && (password.Length < 8 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit)))
        {
            Console.Error.WriteLine("[seed-superadmin] --password must be ≥8 chars with at least 1 letter + 1 digit.");
            return null;
        }
        if (language != "vi" && language != "en")
        {
            Console.Error.WriteLine("[seed-superadmin] --language must be 'vi' or 'en'.");
            return null;
        }

        return new ParsedArgs(email!, password, string.IsNullOrWhiteSpace(fullName) ? "Super Admin" : fullName!, language!);
    }

    /// <summary>16-char URL-safe random password guaranteed to contain a letter and a digit.</summary>
    private static string GeneratePassword()
    {
        const string letters = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string all = letters + digits + "!@#$%^&*-_";

        char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];

        var chars = new char[16];
        chars[0] = Pick(letters);
        chars[1] = Pick(digits);
        for (var i = 2; i < chars.Length; i++) chars[i] = Pick(all);

        // Shuffle so the guaranteed letter/digit aren't always first (Fisher–Yates).
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }
}
