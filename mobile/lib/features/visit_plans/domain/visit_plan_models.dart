/// Plain (non-Freezed) models for Visit Plans (M11). Mirror
/// `Rmms.Application.VisitPlans.VisitPlanDto` / `VisitPlanItemDto` (camelCase).

/// One planned store visit within a plan.
class VisitPlanItem {
  const VisitPlanItem({
    required this.id,
    required this.storeId,
    this.storeName,
    required this.formId,
    this.formName,
    required this.ordering,
    this.executedAt,
    this.formSubmissionId,
  });

  final String id;
  final String storeId;
  final String? storeName;
  final String formId;
  final String? formName;
  final int ordering;
  final DateTime? executedAt;
  final String? formSubmissionId;

  bool get isDone => formSubmissionId != null;

  factory VisitPlanItem.fromJson(Map<String, dynamic> j) => VisitPlanItem(
        id: j['id'] as String,
        storeId: j['storeId'] as String? ?? '',
        storeName: j['storeName'] as String?,
        formId: j['formId'] as String? ?? '',
        formName: j['formName'] as String?,
        ordering: (j['ordering'] as num?)?.toInt() ?? 0,
        executedAt: j['executedAt'] == null ? null : DateTime.tryParse(j['executedAt'] as String),
        formSubmissionId: j['formSubmissionId'] as String?,
      );
}

/// A Leader's visit plan for one day.
class VisitPlan {
  const VisitPlan({
    required this.id,
    required this.leaderUserId,
    required this.visitDate,
    this.notes,
    required this.status,
    this.approvalId,
    required this.createdAt,
    required this.items,
    this.leaderName,
  });

  final String id;
  final String leaderUserId;
  final DateTime visitDate;
  final String? notes;
  final String status; // pending | approved | rejected | executed
  final String? approvalId;
  final DateTime createdAt;
  final List<VisitPlanItem> items;
  final String? leaderName;

  int get doneCount => items.where((i) => i.isDone).length;
  bool get isApproved => status == 'approved' || status == 'executed';

  factory VisitPlan.fromJson(Map<String, dynamic> j) => VisitPlan(
        id: j['id'] as String,
        leaderUserId: j['leaderUserId'] as String? ?? '',
        visitDate: DateTime.tryParse(j['visitDate'] as String? ?? '') ?? DateTime.now(),
        notes: j['notes'] as String?,
        status: j['status'] as String? ?? 'pending',
        approvalId: j['approvalId'] as String?,
        createdAt: DateTime.tryParse(j['createdAt'] as String? ?? '') ?? DateTime.now(),
        items: ((j['items'] as List?) ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(VisitPlanItem.fromJson)
            .toList(growable: false),
        leaderName: j['leaderName'] as String?,
      );
}

/// One store+form line when creating a plan.
class VisitItemDraft {
  const VisitItemDraft({required this.storeId, required this.formId});
  final String storeId;
  final String formId;

  Map<String, dynamic> toJson() => {'storeId': storeId, 'formId': formId};
}
