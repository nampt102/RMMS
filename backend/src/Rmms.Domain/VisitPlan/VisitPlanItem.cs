using Rmms.Domain.Common;

namespace Rmms.Domain.VisitPlan;

/// <summary>
/// One store visit within a <see cref="VisitPlan"/> (M11, <c>04-data-model.md</c>
/// visit_plan_items): the store to visit and the form to fill there. After the visit the
/// Leader links the post-visit Form Engine submission (<see cref="FormSubmissionId"/>) which
/// stamps <see cref="ExecutedAt"/>.
///
/// Child of the <see cref="VisitPlan"/> aggregate — never created or queried on its own.
/// </summary>
public sealed class VisitPlanItem : Entity
{
    public Guid VisitPlanId { get; private set; }
    public Guid StoreId { get; private set; }

    /// <summary>The form to fill at this store (post-visit report).</summary>
    public Guid FormId { get; private set; }

    /// <summary>0-based position within the plan.</summary>
    public int Ordering { get; private set; }

    public DateTimeOffset? ExecutedAt { get; private set; }
    public Guid? FormSubmissionId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private VisitPlanItem() { } // EF Core

    internal VisitPlanItem(Guid visitPlanId, Guid storeId, Guid formId, int ordering)
    {
        if (storeId == Guid.Empty) throw new ArgumentException("Store id is required.", nameof(storeId));
        if (formId == Guid.Empty) throw new ArgumentException("Form id is required.", nameof(formId));

        VisitPlanId = visitPlanId;
        StoreId = storeId;
        FormId = formId;
        Ordering = ordering;
    }

    /// <summary>Mutate this item in place (used when reconciling a pending-plan edit).</summary>
    internal void Update(Guid storeId, Guid formId, int ordering)
    {
        if (storeId == Guid.Empty) throw new ArgumentException("Store id is required.", nameof(storeId));
        if (formId == Guid.Empty) throw new ArgumentException("Form id is required.", nameof(formId));

        StoreId = storeId;
        FormId = formId;
        Ordering = ordering;
    }

    public bool IsExecuted => FormSubmissionId is not null;

    /// <summary>Link the post-visit form submission for this store and stamp the execution time.</summary>
    internal void Execute(Guid formSubmissionId, DateTimeOffset now)
    {
        if (formSubmissionId == Guid.Empty) throw new ArgumentException("Form submission id is required.", nameof(formSubmissionId));
        FormSubmissionId = formSubmissionId;
        ExecutedAt = now;
    }
}
