import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/visit_plan_models.dart';
import 'visit_plans_api.dart';

final visitPlansApiProvider = Provider<VisitPlansApi>((ref) => VisitPlansApi(ref.watch(dioProvider)));

final visitPlansRepositoryProvider = Provider<VisitPlansRepository>((ref) {
  return VisitPlansRepository(ref.watch(visitPlansApiProvider));
});

/// Leader Visit Plan surface (M11). Dio failures → [ApiException].
class VisitPlansRepository {
  VisitPlansRepository(this._api);
  final VisitPlansApi _api;

  Future<List<VisitPlan>> myPlans() => _guard(_api.myPlans);

  Future<VisitPlan> getPlan(String id) => _guard(() => _api.getPlan(id));

  Future<VisitPlan> create({
    required DateTime visitDate,
    String? notes,
    required List<VisitItemDraft> items,
  }) =>
      _guard(() => _api.create(visitDate: visitDate, notes: notes, items: items));

  Future<VisitPlan> executeItem({
    required String planId,
    required String itemId,
    required String formSubmissionId,
  }) =>
      _guard(() => _api.executeItem(planId: planId, itemId: itemId, formSubmissionId: formSubmissionId));

  Future<T> _guard<T>(Future<T> Function() action) async {
    try {
      return await action();
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}

/// The signed-in Leader's visit plans.
final myVisitPlansProvider = FutureProvider.autoDispose<List<VisitPlan>>((ref) {
  return ref.watch(visitPlansRepositoryProvider).myPlans();
});

/// A single visit plan with its items.
final visitPlanProvider = FutureProvider.autoDispose.family<VisitPlan, String>((ref, id) {
  return ref.watch(visitPlansRepositoryProvider).getPlan(id);
});
