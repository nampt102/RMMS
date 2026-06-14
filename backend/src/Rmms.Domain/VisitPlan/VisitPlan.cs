using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Domain.VisitPlan;

/// <summary>Input for one planned store visit when creating/editing a plan (store + form to fill).</summary>
public sealed record VisitItemInput(Guid StoreId, Guid FormId);

/// <summary>
/// A Leader's plan to visit one or more stores on a given day (M11, <c>04-data-model.md</c>
/// visit_plans). Holds 1..N <see cref="VisitPlanItem"/> (each = store + post-visit form).
///
/// Lifecycle (re-uses the M09 approval engine, Leader→BUH per BR-406):
///  - Create → <see cref="VisitPlanStatus.Pending"/>; an approval is routed to the BUH.
///  - Editing a still-pending plan edits it in place (<see cref="ReplaceItems"/>).
///  - BUH decision (web/email) flips it to <see cref="VisitPlanStatus.Approved"/> / <see cref="VisitPlanStatus.Rejected"/>.
///  - After the visits, the Leader links each item's post-visit submission (<see cref="ExecuteItem"/>);
///    once every item is executed the plan becomes <see cref="VisitPlanStatus.Executed"/>.
/// </summary>
public sealed class VisitPlan : AuditableEntity, IAggregateRoot
{
    private readonly List<VisitPlanItem> _items = new();

    public Guid LeaderUserId { get; private set; }
    public DateOnly VisitDate { get; private set; }
    public string? Notes { get; private set; }
    public VisitPlanStatus Status { get; private set; }
    public Guid? ApprovalId { get; private set; }

    public IReadOnlyList<VisitPlanItem> Items => _items;

    private VisitPlan() { } // EF Core

    /// <summary>Create a new draft plan (status = Pending) with at least one store item.</summary>
    public static VisitPlan Create(Guid leaderUserId, DateOnly visitDate, string? notes, IReadOnlyList<VisitItemInput> items)
    {
        if (leaderUserId == Guid.Empty) throw new ArgumentException("Leader user id is required.", nameof(leaderUserId));

        var plan = new VisitPlan
        {
            LeaderUserId = leaderUserId,
            VisitDate = visitDate,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Status = VisitPlanStatus.Pending,
        };
        plan.SetItems(items);
        return plan;
    }

    public bool IsPending => Status == VisitPlanStatus.Pending;

    public void LinkApproval(Guid approvalId) => ApprovalId = approvalId;

    /// <summary>
    /// Replace the items of a still-pending plan. Reconciles in place (mutate / add / trim) so an
    /// unchanged-count edit issues UPDATEs rather than delete-and-recreate.
    /// </summary>
    public void ReplaceItems(string? notes, IReadOnlyList<VisitItemInput> items)
    {
        if (Status != VisitPlanStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending visit plan can be edited.");
        }

        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        var ordered = ValidateAndOrder(items);
        for (var i = 0; i < ordered.Count; i++)
        {
            if (i < _items.Count)
            {
                _items[i].Update(ordered[i].StoreId, ordered[i].FormId, i);
            }
            else
            {
                _items.Add(new VisitPlanItem(Id, ordered[i].StoreId, ordered[i].FormId, i));
            }
        }
        while (_items.Count > ordered.Count)
        {
            _items.RemoveAt(_items.Count - 1);
        }
    }

    public void Approve(DateTimeOffset now)
    {
        EnsurePending();
        Status = VisitPlanStatus.Approved;
        UpdatedAt = now;
    }

    public void Reject(DateTimeOffset now)
    {
        EnsurePending();
        Status = VisitPlanStatus.Rejected;
        UpdatedAt = now;
    }

    /// <summary>
    /// Link a post-visit form submission to one item (AC-30). Allowed only on an approved plan.
    /// When every item has a submission the plan transitions to <see cref="VisitPlanStatus.Executed"/>.
    /// </summary>
    public void ExecuteItem(Guid itemId, Guid formSubmissionId, DateTimeOffset now)
    {
        if (Status is not (VisitPlanStatus.Approved or VisitPlanStatus.Executed))
        {
            throw new InvalidOperationException("Only an approved visit plan can be executed.");
        }

        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException("Visit plan item not found.");

        item.Execute(formSubmissionId, now);
        UpdatedAt = now;

        if (_items.Count > 0 && _items.All(i => i.IsExecuted))
        {
            Status = VisitPlanStatus.Executed;
        }
    }

    private void EnsurePending()
    {
        if (Status != VisitPlanStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending visit plan can be decided.");
        }
    }

    private void SetItems(IReadOnlyList<VisitItemInput> inputs)
    {
        var ordered = ValidateAndOrder(inputs);
        _items.Clear();
        for (var i = 0; i < ordered.Count; i++)
        {
            _items.Add(new VisitPlanItem(Id, ordered[i].StoreId, ordered[i].FormId, i));
        }
    }

    /// <summary>Validate (non-empty, no duplicate store) and return items in given order.</summary>
    private static List<VisitItemInput> ValidateAndOrder(IReadOnlyList<VisitItemInput> inputs)
    {
        if (inputs is null || inputs.Count == 0)
        {
            throw new InvalidOperationException("A visit plan must have at least one store.");
        }
        if (inputs.Select(i => i.StoreId).Distinct().Count() != inputs.Count)
        {
            throw new InvalidOperationException("A store cannot appear twice in the same visit plan.");
        }
        return inputs.ToList();
    }
}
