using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Domain.Users;
using Rmms.Infrastructure.Persistence;

namespace Rmms.Api.Cli;

/// <summary>
/// Seeds a realistic, coherent master-data set for demo / pilot: org areas (hierarchical),
/// retail stores with GPS, product categories + SKUs, a BUH + Leaders + PGs (all login-ready),
/// and the PG↔Leader / user↔store / user↔category assignments that tie them together.
///
/// Usage:
///   dotnet Rmms.Api.dll seed-master [--password=Rmms@2026] [--domain=rmms.local] [--language=vi]
///
/// Idempotent: areas/stores/categories are matched by code, products by SKU, users by email,
/// assignments by active row — existing rows are skipped, so it is safe to re-run. Pair with
/// <c>reset-data --confirm</c> first for a perfectly clean slate.
/// </summary>
public static class SeedMasterCommand
{
    private const string DefaultPassword = "Rmms@2026";
    private const string DefaultDomain = "rmms.local";

    public static async Task<int> RunAsync(IReadOnlyList<string> args, IServiceProvider services)
    {
        var password = DefaultPassword;
        var domain = DefaultDomain;
        var language = "vi";

        foreach (var raw in args.Skip(1))
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
                    Console.Error.WriteLine($"[seed-master] Unknown flag: {raw}");
                    return 1;
            }
        }

        if (password.Length < 8 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            Console.Error.WriteLine("[seed-master] --password must be ≥8 chars with at least 1 letter + 1 digit.");
            return 1;
        }

        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var clock = sp.GetRequiredService<IDateTimeProvider>();
        var hash = hasher.Hash(password);
        var today = clock.UtcToday;

        // ───────── Areas (hierarchical) ─────────
        var areaId = new Dictionary<string, Guid>(StringComparer.Ordinal);
        async Task Area_(string code, string name, string? parentCode)
        {
            var existing = await db.Areas.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Code == code);
            if (existing is not null) { areaId[code] = existing.Id; return; }
            var parent = parentCode is not null && areaId.TryGetValue(parentCode, out var p) ? p : (Guid?)null;
            var area = Area.Create(code, name, parent);
            db.Areas.Add(area);
            areaId[code] = area.Id;
        }
        await Area_("MN", "Miền Nam", null);
        await Area_("MB", "Miền Bắc", null);
        await Area_("HCM", "TP. Hồ Chí Minh", "MN");
        await Area_("HN", "Hà Nội", "MB");
        await Area_("HCM-Q1", "Quận 1", "HCM");
        await Area_("HCM-Q3", "Quận 3", "HCM");
        await Area_("HCM-TD", "TP. Thủ Đức", "HCM");
        await Area_("HN-CG", "Cầu Giấy", "HN");
        await Area_("HN-HK", "Hoàn Kiếm", "HN");
        await db.SaveChangesAsync();

        // ───────── Categories (ngành hàng) ─────────
        var categoryId = new Dictionary<string, Guid>(StringComparer.Ordinal);
        async Task Cat_(string code, string name)
        {
            var existing = await db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Code == code);
            if (existing is not null) { categoryId[code] = existing.Id; return; }
            var cat = Category.Create(code, name);
            db.Categories.Add(cat);
            categoryId[code] = cat.Id;
        }
        await Cat_("BEV", "Nước giải khát");
        await Cat_("DAIRY", "Sữa & sản phẩm từ sữa");
        await Cat_("SNACK", "Bánh kẹo");
        await Cat_("HOME", "Hoá phẩm gia dụng");
        await Cat_("PCARE", "Chăm sóc cá nhân");
        await db.SaveChangesAsync();

        // ───────── Stores (with GPS, attached to an area) ─────────
        var storeId = new Dictionary<string, Guid>(StringComparer.Ordinal);
        async Task Store_(string code, string name, string address, decimal lat, decimal lng, string areaCode)
        {
            var existing = await db.Stores.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Code == code);
            if (existing is not null) { storeId[code] = existing.Id; return; }
            var store = Store.Create(code, name, address, lat, lng, areaId.GetValueOrDefault(areaCode));
            db.Stores.Add(store);
            storeId[code] = store.Id;
        }
        await Store_("ST-001", "Co.opmart Cống Quỳnh", "201 Cống Quỳnh, Q1, TP.HCM", 10.7665m, 106.6890m, "HCM-Q1");
        await Store_("ST-002", "Bách hoá Xanh Nguyễn Trãi", "169 Nguyễn Trãi, Q1, TP.HCM", 10.7626m, 106.6822m, "HCM-Q1");
        await Store_("ST-003", "WinMart Lê Văn Sỹ", "242 Lê Văn Sỹ, Q3, TP.HCM", 10.7900m, 106.6780m, "HCM-Q3");
        await Store_("ST-004", "Co.opXtra Nguyễn Đình Chiểu", "168 Nguyễn Đình Chiểu, Q3, TP.HCM", 10.7838m, 106.6850m, "HCM-Q3");
        await Store_("ST-005", "WinMart+ Võ Văn Ngân", "18 Võ Văn Ngân, TP. Thủ Đức", 10.8494m, 106.7537m, "HCM-TD");
        await Store_("ST-006", "Co.opmart Xa Lộ Hà Nội", "191 Xa Lộ Hà Nội, TP. Thủ Đức", 10.8230m, 106.7710m, "HCM-TD");
        await Store_("ST-007", "WinMart Cầu Giấy", "126 Xuân Thuỷ, Cầu Giấy, Hà Nội", 21.0362m, 105.7827m, "HN-CG");
        await Store_("ST-008", "Intimex Bờ Hồ", "32 Lê Thái Tổ, Hoàn Kiếm, Hà Nội", 21.0285m, 105.8520m, "HN-HK");
        await db.SaveChangesAsync();

        // ───────── Products (SKUs) ─────────
        async Task Product_(string sku, string name, string brand, string categoryCode)
        {
            if (await db.Products.IgnoreQueryFilters().AnyAsync(p => p.Sku == sku)) return;
            db.Products.Add(Product.Create(sku, name, brand, categoryId.GetValueOrDefault(categoryCode), null));
        }
        await Product_("BEV-CC330", "Coca-Cola lon 330ml", "Coca-Cola", "BEV");
        await Product_("BEV-PP330", "Pepsi lon 330ml", "Suntory PepsiCo", "BEV");
        await Product_("BEV-STG330", "Sting Dâu 330ml", "Suntory PepsiCo", "BEV");
        await Product_("DRY-VNM180", "Vinamilk có đường 180ml", "Vinamilk", "DAIRY");
        await Product_("DRY-TH180", "TH true MILK 180ml", "TH true MILK", "DAIRY");
        await Product_("SNK-ORE137", "Oreo 137g", "Mondelez", "SNACK");
        await Product_("SNK-COSY132", "Bánh Cosy 132g", "Kinh Đô", "SNACK");
        await Product_("HOM-OMO720", "OMO Matic 720g", "Unilever", "HOME");
        await Product_("HOM-SUN750", "Nước rửa chén Sunlight Chanh 750g", "Unilever", "HOME");
        await Product_("PCR-CLG200", "Kem đánh răng Colgate 200g", "Colgate", "PCARE");
        await Product_("PCR-DOV340", "Dầu gội Dove 340g", "Unilever", "PCARE");
        await db.SaveChangesAsync();

        // ───────── Users (BUH + Leaders + PGs), all Active / login-ready ─────────
        var userId = new Dictionary<string, Guid>(StringComparer.Ordinal);
        async Task User_(string localPart, string fullName, UserRole role, string phone)
        {
            var email = $"{localPart}@{domain}".ToLowerInvariant();
            var existing = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email);
            if (existing is not null) { userId[localPart] = existing.Id; return; }

            User user;
            if (role == UserRole.Pg)
            {
                user = User.Register(email, hash, fullName, phone, language);
                user.VerifyEmail(clock.UtcNow); // demo PGs are Active immediately
            }
            else
            {
                user = User.CreateByAdmin(email, hash, fullName, role, phone, language);
            }
            db.Users.Add(user);
            userId[localPart] = user.Id;
        }
        await User_("buh", "Trần Quốc Bảo", UserRole.Buh, "0901000001");
        await User_("leader.hcm", "Nguyễn Thị Hồng", UserRole.Leader, "0902000001");
        await User_("leader.hn", "Lê Văn Cường", UserRole.Leader, "0902000002");
        await User_("pg1", "Phạm Thị Mai", UserRole.Pg, "0903000001");
        await User_("pg2", "Võ Minh Tuấn", UserRole.Pg, "0903000002");
        await User_("pg3", "Đặng Thị Lan", UserRole.Pg, "0903000003");
        await User_("pg4", "Bùi Văn Khoa", UserRole.Pg, "0903000004");
        await User_("pg5", "Hoàng Thị Thu", UserRole.Pg, "0903000005");
        await User_("pg6", "Ngô Văn Sơn", UserRole.Pg, "0903000006");
        await User_("pg7", "Lý Thị Hà", UserRole.Pg, "0903000007");
        await User_("pg8", "Trịnh Văn Đạt", UserRole.Pg, "0903000008");
        await db.SaveChangesAsync();

        // ───────── Assignments ─────────
        async Task PgLeader_(string pg, string leader)
        {
            if (!userId.TryGetValue(pg, out var pgId) || !userId.TryGetValue(leader, out var leaderId)) return;
            if (await db.UserLeaderAssignments.AnyAsync(a => a.PgUserId == pgId && a.EffectiveTo == null)) return;
            db.UserLeaderAssignments.Add(UserLeaderAssignment.Create(pgId, leaderId, today));
        }
        async Task UserStore_(string user, string store)
        {
            if (!userId.TryGetValue(user, out var uId) || !storeId.TryGetValue(store, out var sId)) return;
            if (await db.UserStoreAssignments.AnyAsync(a => a.UserId == uId && a.StoreId == sId && a.EffectiveTo == null)) return;
            db.UserStoreAssignments.Add(UserStoreAssignment.Create(uId, sId, today));
        }
        async Task UserCategory_(string user, string category)
        {
            if (!userId.TryGetValue(user, out var uId) || !categoryId.TryGetValue(category, out var cId)) return;
            if (await db.UserCategoryAssignments.AnyAsync(a => a.UserId == uId && a.CategoryId == cId)) return;
            db.UserCategoryAssignments.Add(UserCategoryAssignment.Create(uId, cId));
        }

        foreach (var pg in new[] { "pg1", "pg2", "pg3", "pg4" }) await PgLeader_(pg, "leader.hcm");
        foreach (var pg in new[] { "pg5", "pg6", "pg7", "pg8" }) await PgLeader_(pg, "leader.hn");

        await UserStore_("leader.hcm", "ST-001"); await UserStore_("leader.hcm", "ST-003");
        await UserStore_("leader.hn", "ST-007"); await UserStore_("leader.hn", "ST-008");
        await UserStore_("pg1", "ST-001"); await UserStore_("pg2", "ST-002");
        await UserStore_("pg3", "ST-003"); await UserStore_("pg4", "ST-005");
        await UserStore_("pg5", "ST-007"); await UserStore_("pg6", "ST-008");
        await UserStore_("pg7", "ST-007"); await UserStore_("pg8", "ST-008");

        await UserCategory_("pg1", "BEV"); await UserCategory_("pg2", "BEV");
        await UserCategory_("pg3", "DAIRY"); await UserCategory_("pg4", "DAIRY");
        await UserCategory_("pg5", "SNACK"); await UserCategory_("pg6", "SNACK");
        await UserCategory_("pg7", "HOME"); await UserCategory_("pg8", "PCARE");
        await db.SaveChangesAsync();

        Console.WriteLine("[seed-master] Done — master data ready for demo / pilot.");
        Console.WriteLine($"              areas={areaId.Count}  categories={categoryId.Count}  stores={storeId.Count}  users={userId.Count}");
        Console.WriteLine($"              shared password = {password}  (domain @{domain})");
        Console.WriteLine("              accounts: buh, leader.hcm, leader.hn, pg1..pg8");
        Console.WriteLine("              Rotate the seed password after the demo.");
        return 0;
    }
}
