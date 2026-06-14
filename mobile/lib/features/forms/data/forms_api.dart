import 'package:dio/dio.dart';

import '../domain/form_models.dart';

/// Hand-written Dio client for the M10 mobile fill surface.
class FormsApi {
  FormsApi(this._dio);

  final Dio _dio;

  /// Forms assigned to the current user (GET /forms/me).
  Future<List<AssignedForm>> myForms() async {
    final res = await _dio.get<Map<String, dynamic>>('/forms/me');
    return _list(res).whereType<Map<String, dynamic>>().map(AssignedForm.fromJson).toList(growable: false);
  }

  /// A form's current published schema to render (GET /forms/:id).
  Future<FormFill> getForm(String id) async {
    final res = await _dio.get<Map<String, dynamic>>('/forms/$id');
    return FormFill.fromJson(_data(res));
  }

  /// Submit answers. [clientIdempotencyKey] dedups offline retries server-side.
  /// Returns the submission id.
  Future<String> submit({
    required String formId,
    required Map<String, dynamic> answers,
    Map<String, dynamic>? attachments,
    String? storeId,
    required int timeSpentSeconds,
    required String clientIdempotencyKey,
  }) async {
    final res = await _dio.post<Map<String, dynamic>>(
      '/forms/$formId/submit',
      data: {
        'answers': answers,
        if (attachments != null) 'attachments': attachments,
        if (storeId != null) 'storeId': storeId,
        'timeSpentSeconds': timeSpentSeconds,
        'clientIdempotencyKey': clientIdempotencyKey,
      },
    );
    return _data(res)['id'] as String;
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
