using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.Organization.Me;
using Rmms.Shared.Errors;

namespace Rmms.Api.Controllers;

/// <summary>
/// Self-service reads for the mobile app (M03): the caller's assigned stores and Leader.
/// Identity comes from the JWT — no id in the route.
/// </summary>
[ApiController]
[Route("api/v1/users/me")]
[Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public UsersController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Active stores assigned to the current PG/Leader.</summary>
    [HttpGet("stores")]
    public async Task<IActionResult> MyStores(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return ResultMapping.Failure(
                Rmms.Domain.Common.Error.Unauthorized(ErrorCodes.TokenInvalid, "Bạn cần đăng nhập."),
                HttpContext.TraceIdentifier);
        }

        var result = await _mediator.Send(new GetMyStoresQuery(userId), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>The current PG's active managing Leader (null if none / non-PG).</summary>
    [HttpGet("leader")]
    public async Task<IActionResult> MyLeader(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return ResultMapping.Failure(
                Rmms.Domain.Common.Error.Unauthorized(ErrorCodes.TokenInvalid, "Bạn cần đăng nhập."),
                HttpContext.TraceIdentifier);
        }

        var result = await _mediator.Send(new GetMyLeaderQuery(userId), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }
}
