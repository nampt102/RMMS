using System.Text.Json;
using Rmms.Application.Common;
using Rmms.Domain.Notifications;

namespace Rmms.Application.Notifications;

/// <summary>A single in-app notification as returned to the mobile client.</summary>
public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    IReadOnlyDictionary<string, string>? Data,
    bool IsRead,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt);

/// <summary>A page of notifications + the unread badge count.</summary>
public sealed record NotificationListDto(
    IReadOnlyList<NotificationDto> Items,
    int UnreadCount,
    int Page,
    int PageSize,
    int Total);

internal static class NotificationMapper
{
    public static NotificationDto ToDto(Notification n) => new(
        n.Id,
        n.Type.ToSnakeCase(),
        n.Title,
        n.Body,
        ParseData(n.Data),
        n.IsRead,
        n.ReadAt,
        n.CreatedAt);

    private static Dictionary<string, string>? ParseData(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
