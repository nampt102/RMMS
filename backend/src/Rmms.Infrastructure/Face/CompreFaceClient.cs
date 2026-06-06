using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Options;

namespace Rmms.Infrastructure.Face;

/// <summary>
/// Real face client backed by self-hosted CompreFace (M06, ADR-011). Talks to the Recognition
/// service: enroll = add example images under <c>subject</c>; verify = <c>recognize</c> and match
/// when the top subject equals the expected subject and similarity ≥ threshold; delete = remove
/// the subject. The <c>x-api-key</c> header + base address are set on the typed <see cref="HttpClient"/>.
/// </summary>
internal sealed class CompreFaceClient : IFaceClient
{
    private readonly HttpClient _http;
    private readonly CompreFaceOptions _options;

    public CompreFaceClient(HttpClient http, IOptions<CompreFaceOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public bool IsLive => true;

    public async Task EnrollAsync(string subjectId, IReadOnlyList<PhotoUpload> photos, CancellationToken ct = default)
    {
        foreach (var photo in photos)
        {
            using var content = BuildFileContent(photo);
            var url = $"api/v1/recognition/faces?subject={Uri.EscapeDataString(subjectId)}&det_prob_threshold=0.8";
            using var resp = await _http.PostAsync(url, content, ct);
            resp.EnsureSuccessStatusCode();
        }
    }

    public async Task<FaceMatchResult> VerifyAsync(string subjectId, PhotoUpload selfie, CancellationToken ct = default)
    {
        using var content = BuildFileContent(selfie);
        using var resp = await _http.PostAsync("api/v1/recognition/recognize?limit=1&prediction_count=1", content, ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        // Shape: { "result": [ { "subjects": [ { "subject": "...", "similarity": 0.97 }, ... ] } ] }
        if (!doc.RootElement.TryGetProperty("result", out var result) ||
            result.ValueKind != JsonValueKind.Array || result.GetArrayLength() == 0)
        {
            return new FaceMatchResult(false, 0m); // no face detected
        }

        var first = result[0];
        if (!first.TryGetProperty("subjects", out var subjects) ||
            subjects.ValueKind != JsonValueKind.Array || subjects.GetArrayLength() == 0)
        {
            return new FaceMatchResult(false, 0m);
        }

        var top = subjects[0]; // CompreFace returns subjects sorted by similarity desc
        var matchedSubject = top.GetProperty("subject").GetString();
        var similarity = top.GetProperty("similarity").GetDecimal();

        var isMatch = string.Equals(matchedSubject, subjectId, StringComparison.Ordinal)
                      && similarity >= _options.ConfidenceThreshold;
        return new FaceMatchResult(isMatch, similarity);
    }

    public async Task DeleteAsync(string subjectId, CancellationToken ct = default)
    {
        using var resp = await _http.DeleteAsync(
            $"api/v1/recognition/subjects/{Uri.EscapeDataString(subjectId)}", ct);
        // 404 = nothing to delete → treat as success (idempotent).
        if (resp.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            resp.EnsureSuccessStatusCode();
        }
    }

    private static MultipartFormDataContent BuildFileContent(PhotoUpload photo)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(photo.Content);
        file.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(photo.ContentType) ? "image/jpeg" : photo.ContentType);
        content.Add(file, "file", string.IsNullOrWhiteSpace(photo.FileName) ? "face.jpg" : photo.FileName);
        return content;
    }
}
