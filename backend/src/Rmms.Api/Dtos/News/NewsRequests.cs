namespace Rmms.Api.Dtos.News;

/// <summary>Create a bilingual news/announcement (M14, draft until published).</summary>
public sealed record CreateNewsRequest(
    string TitleVi, string TitleEn, string ContentVi, string ContentEn, string? Category, bool IsImportant);

/// <summary>Edit a news item (M14).</summary>
public sealed record UpdateNewsRequest(
    string TitleVi, string TitleEn, string ContentVi, string ContentEn, string? Category, bool IsImportant);

/// <summary>Target a news item to a role or single user (M14).</summary>
public sealed record AssignNewsRequest(string? Role, Guid? UserId);
