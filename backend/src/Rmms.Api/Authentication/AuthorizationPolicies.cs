using Microsoft.AspNetCore.Authorization;

namespace Rmms.Api.Authentication;

/// <summary>
/// Central catalogue of named authorization policies for the RMMS API.
///
/// Role values in the JWT are emitted lowercase ("pg", "leader", "buh", "admin")
/// by <c>JwtTokenService</c>, and <c>RoleClaimType = "role"</c> + <c>MapInboundClaims = false</c>
/// in <c>Program.cs</c> ensure <see cref="AuthorizationPolicyBuilder.RequireRole(string[])"/>
/// matches them exactly. Use these constants with
/// <c>[Authorize(Policy = AuthorizationPolicies.AdminOnly)]</c> instead of hardcoding
/// <c>[Authorize(Roles = "...")]</c> on controllers.
/// </summary>
public static class AuthorizationPolicies
{
    public const string PgOnly = "PgOnly";
    public const string LeaderOnly = "LeaderOnly";
    public const string BuhOnly = "BuhOnly";
    public const string AdminOnly = "AdminOnly";
    public const string PgOrLeader = "PgOrLeader";
    public const string AdminOrLeader = "AdminOrLeader";
    public const string AnyAuthenticated = "AnyAuthenticated";

    // Lowercase role claim values — must match JwtTokenService output.
    private const string RolePg = "pg";
    private const string RoleLeader = "leader";
    private const string RoleBuh = "buh";
    private const string RoleAdmin = "admin";

    /// <summary>
    /// Registers all RMMS authorization policies. Call instead of the bare
    /// <c>AddAuthorization()</c> in <c>Program.cs</c>.
    /// </summary>
    public static IServiceCollection AddRmmsAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PgOnly, p => p.RequireRole(RolePg));
            options.AddPolicy(LeaderOnly, p => p.RequireRole(RoleLeader));
            options.AddPolicy(BuhOnly, p => p.RequireRole(RoleBuh));
            options.AddPolicy(AdminOnly, p => p.RequireRole(RoleAdmin));
            options.AddPolicy(PgOrLeader, p => p.RequireRole(RolePg, RoleLeader));
            options.AddPolicy(AdminOrLeader, p => p.RequireRole(RoleAdmin, RoleLeader));
            options.AddPolicy(AnyAuthenticated, p => p.RequireAuthenticatedUser());
        });

        return services;
    }
}
