import 'package:dio/dio.dart';

import '../domain/visit_plan_models.dart';

/// Leader surface for Visit Plans (M11). Mirrors `VisitPlansController`
/// (base path /api/v1 set on the Dio instance).
class VisitPlansApi {
  VisitPlansApi(this._dio);
  final Dio _dio;

  /// GET /visit-plans/me — the signed-in Leader's plans.
  Future<List<VisitPlan>> myPlans() async {
    final res = await _dio.get<Map<String, dynamic>>('/visit-plans/me');
    return _list(res).whereType<Map<String, dynamic>>().map(VisitPlan.fromJson).toList(growable: false);
  }

  /// GET /visit-plans/:id — one plan with its items.
  Future<VisitPlan> getPlan(String id) async {
    final res = await _dio.get<Map<String, dynamic>>('/visit-plans/$id');
    return VisitPlan.fromJson(_data(res));
  }

  /// POST /visit-plans — create a plan; returns the new plan (DTO).
  Future<VisitPlan> create({
    required DateTime visitDate,
    String? notes,
    required List<VisitItemDraft> items,
  }) async {
    final res = await _dio.post<Map<String, dynamic>>('/visit-plans', data: {
      // DateOnly on the server — send the date part only.
      'visitDate': visitDate.toIso8601String().split('T').first,
      if (notes != null && notes.isNotEmpty) 'notes': notes,
      'items': items.map((i) => i.toJson()).toList(),
    });
    return VisitPlan.fromJson(_data(res));
  }

  /// PATCH /visit-plans/:id — edit a still-pending plan (same payload as create).
  Future<VisitPlan> edit({
    required String id,
    required DateTime visitDate,
    String? notes,
    required List<VisitItemDraft> items,
  }) async {
    final res = await _dio.patch<Map<String, dynamic>>('/visit-plans/$id', data: {
      'visitDate': visitDate.toIso8601String().split('T').first,
      if (notes != null && notes.isNotEmpty) 'notes': notes,
      'items': items.map((i) => i.toJson()).toList(),
    });
    return VisitPlan.fromJson(_data(res));
  }

  /// POST /visit-plans/:id/items/:itemId/execute — link a form submission.
  Future<VisitPlan> executeItem({
    required String planId,
    required String itemId,
    required String formSubmissionId,
  }) async {
    final res = await _dio.post<Map<String, dynamic>>(
      '/visit-plans/$planId/items/$itemId/execute',
      data: {'formSubmissionId': formSubmissionId},
    );
    return VisitPlan.fromJson(_data(res));
  }

  Map<String, dynamic> _data(Response<Map<String, dynamic>> res) {
    final data = res.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw DioException(requestOptions: res.requestOptions, response: res, message: 'Malformed envelope.');
    }
    return data;
  }

  List<dynamic> _list(Response<Map<String, dynamic>> res) {
    final body = res.data;
    if (body == null || body['data'] is! List) {
      throw DioException(requestOptions: res.requestOptions, response: res, message: 'Malformed envelope.');
    }
    return body['data'] as List<dynamic>;
  }
}
