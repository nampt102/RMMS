using Rmms.Application.Common;
using Rmms.Domain.Documents;

namespace Rmms.Application.Documents;

/// <summary>Document projected for list/detail (M13). The file key is never exposed — downloads go
/// through a signed-URL endpoint.</summary>
public sealed record DocumentDto(
    Guid Id,
    string Name,
    string? Description,
    string FolderType,
    long FileSizeBytes,
    string MimeType,
    DateTimeOffset CreatedAt);

internal static class DocumentMapper
{
    public static DocumentDto ToDto(Document d) => new(
        d.Id, d.Name, d.Description, d.FolderType.ToSnakeCase(), d.FileSizeBytes, d.MimeType, d.CreatedAt);
}
