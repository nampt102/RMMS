using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rmms.Api.Authentication;
using Rmms.Api.Common;
using Rmms.Api.Dtos.Face;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.FaceRecognition;

namespace Rmms.Api.Controllers;

/// <summary>
/// Self-service Face Verification for PG/Leader (M06, ADR-011 CompreFace): enrollment status,
/// enroll (3 angles), and a standalone verify. Check-in/out verification is automatic in M05.
/// </summary>
[ApiController]
[Route("api/v1/face")]
[Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
public sealed class FaceController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public FaceController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>The caller's face enrollment status.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var result = await _mediator.Send(new GetFaceStatusQuery(userId), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Enroll the caller's face (multipart, 1..5 photos).</summary>
    [HttpPost("enroll")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Enroll([FromForm] EnrollFaceForm form, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();

        var photos = new List<PhotoUpload>();
        foreach (var file in form.Photos)
        {
            var upload = await ToUploadAsync(file, ct);
            if (upload is not null) photos.Add(upload);
        }

        var result = await _mediator.Send(new EnrollFaceCommand(userId, photos), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    /// <summary>Verify the caller's live selfie against their enrolled face (multipart).</summary>
    [HttpPost("verify")]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> Verify([FromForm] VerifyFaceForm form, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId) return Unauthorized();
        var selfie = await ToUploadAsync(form.Selfie, ct);
        if (selfie is null) return BadRequest();

        var result = await _mediator.Send(new VerifyFaceCommand(userId, selfie), ct);
        return result.IsSuccess ? ResultMapping.Ok(result.Value) : ResultMapping.Failure(result.Error, HttpContext.TraceIdentifier);
    }

    private static async Task<PhotoUpload?> ToUploadAsync(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return null;
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        return new PhotoUpload(file.FileName, file.ContentType, ms.ToArray());
    }
}
