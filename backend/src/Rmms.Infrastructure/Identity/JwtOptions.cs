// JwtOptions moved to Rmms.Application.Common.Options.JwtOptions
// (Application layer owns the contract; Infrastructure binds + validates at startup.)
//
// This shim is intentionally left as an empty namespace file so existing `using`
// directives in Infrastructure files do not break. Remove after Sprint 02 cleanup.
namespace Rmms.Infrastructure.Identity;
