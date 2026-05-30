using Mediator;
using Rmms.Domain.Common;

namespace Rmms.Application.Auth.Me;

/// <summary>
/// Returns the authenticated user's profile plus the device the current access token
/// was issued for. <paramref name="UserId"/> / <paramref name="DeviceId"/> come from the
/// JWT claims (resolved by the API layer), never from the request body.
/// </summary>
public sealed record GetMeQuery(Guid UserId, Guid? DeviceId) : IRequest<Result<MeDto>>;
