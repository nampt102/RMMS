using Microsoft.AspNetCore.Http;

namespace Rmms.Api.Dtos.Face;

/// <summary>POST /face/enroll — multipart, 1..5 face photos (front/left/right).</summary>
public sealed class EnrollFaceForm
{
    public List<IFormFile> Photos { get; set; } = new();
}

/// <summary>POST /face/verify — multipart, a single selfie.</summary>
public sealed class VerifyFaceForm
{
    public IFormFile? Selfie { get; set; }
}
