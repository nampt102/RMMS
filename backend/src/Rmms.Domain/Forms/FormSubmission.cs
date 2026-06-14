using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Domain.Forms;

/// <summary>
/// A filled-in form (M10 + <c>04-data-model.md</c> form_submissions). Snapshots
/// <see cref="FormVersionId"/> so the answers stay anchored to the schema they were entered
/// against (BR-505/AC-21). <see cref="ClientIdempotencyKey"/> dedups offline retries (AC-23).
/// </summary>
public sealed class FormSubmission : AuditableEntity, IAggregateRoot
{
    public Guid FormId { get; private set; }
    public Guid FormVersionId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? StoreId { get; private set; }

    /// <summary>Field id → value (jsonb).</summary>
    public string Answers { get; private set; } = "{}";

    /// <summary>Field id → attachment object keys (jsonb). Null when none.</summary>
    public string? Attachments { get; private set; }

    public decimal? Score { get; private set; }
    public int TimeSpentSeconds { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public DateTimeOffset? EditedAt { get; private set; }
    public FormSubmissionStatus Status { get; private set; }
    public string ClientIdempotencyKey { get; private set; } = string.Empty;

    private FormSubmission() { } // EF Core

    public static FormSubmission Create(
        Guid formId, Guid formVersionId, Guid userId, Guid? storeId,
        string answers, string? attachments, decimal? score, int timeSpentSeconds,
        string clientIdempotencyKey, DateTimeOffset now)
    {
        if (formId == Guid.Empty) throw new ArgumentException("Form id is required.", nameof(formId));
        if (formVersionId == Guid.Empty) throw new ArgumentException("Form version id is required.", nameof(formVersionId));
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(clientIdempotencyKey);

        return new FormSubmission
        {
            FormId = formId,
            FormVersionId = formVersionId,
            UserId = userId,
            StoreId = storeId,
            Answers = string.IsNullOrWhiteSpace(answers) ? "{}" : answers,
            Attachments = string.IsNullOrWhiteSpace(attachments) ? null : attachments,
            Score = score,
            TimeSpentSeconds = timeSpentSeconds < 0 ? 0 : timeSpentSeconds,
            SubmittedAt = now,
            Status = FormSubmissionStatus.Submitted,
            ClientIdempotencyKey = clientIdempotencyKey.Trim(),
            CreatedAt = now,
        };
    }
}
