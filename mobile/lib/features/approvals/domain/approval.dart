import 'package:freezed_annotation/freezed_annotation.dart';

part 'approval.freezed.dart';
part 'approval.g.dart';

/// A pending approval routed to the current user (Leader queue) — mirrors
/// `ApprovalDto` (M09). Only the fields the mobile list needs are modelled.
@freezed
sealed class Approval with _$Approval {
  const factory Approval({
    required String id,
    required String entityType,
    required String requesterName,
    required String status,
    String? decisionReason,
    required DateTime createdAt,
  }) = _Approval;

  factory Approval.fromJson(Map<String, dynamic> json) => _$ApprovalFromJson(json);
}
