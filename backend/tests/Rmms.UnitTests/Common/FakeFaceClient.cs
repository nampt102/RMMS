using Rmms.Application.Common.Abstractions;

namespace Rmms.UnitTests.Common;

/// <summary>Configurable <see cref="IFaceClient"/> for tests (match / no-match / throwing).</summary>
internal sealed class FakeFaceClient : IFaceClient
{
    private readonly bool _match;
    private readonly bool _throw;

    public FakeFaceClient(bool match = true, bool throwOnCall = false)
    {
        _match = match;
        _throw = throwOnCall;
    }

    public bool IsLive => true;
    public int EnrollCalls { get; private set; }
    public string? LastDeletedSubject { get; private set; }

    public Task EnrollAsync(string subjectId, IReadOnlyList<PhotoUpload> photos, CancellationToken ct = default)
    {
        if (_throw) throw new InvalidOperationException("face engine down");
        EnrollCalls++;
        return Task.CompletedTask;
    }

    public Task<FaceMatchResult> VerifyAsync(string subjectId, PhotoUpload selfie, CancellationToken ct = default)
    {
        if (_throw) throw new InvalidOperationException("face engine down");
        return Task.FromResult(new FaceMatchResult(_match, _match ? 0.95m : 0.10m));
    }

    public Task DeleteAsync(string subjectId, CancellationToken ct = default)
    {
        LastDeletedSubject = subjectId;
        return Task.CompletedTask;
    }
}
