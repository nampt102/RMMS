import 'package:dio/dio.dart';

import '../domain/assigned_leader.dart';
import '../domain/assigned_store.dart';

/// Hand-written Dio client for the M03 self-service reads. Envelope unwrapping
/// (`{ "data": ... }`) stays explicit, matching `AuthApi`. Failures surface as
/// raw [DioException]; the repository converts them to `ApiException`.
class OrganizationApi {
  OrganizationApi(this._dio);

  final Dio _dio;

  /// Active stores assigned to the current user — `GET /users/me/stores`.
  Future<List<AssignedStore>> myStores() async {
    final response = await _dio.get<Map<String, dynamic>>('/users/me/stores');
    final list = _dataList(response);
    return list
        .whereType<Map<String, dynamic>>()
        .map(AssignedStore.fromJson)
        .toList(growable: false);
  }

  /// The current PG's active Leader, or null if none — `GET /users/me/leader`.
  /// The server omits `data` (or sends null) when there is no Leader.
  Future<AssignedLeader?> myLeader() async {
    final response = await _dio.get<Map<String, dynamic>>('/users/me/leader');
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      return null;
    }
    return AssignedLeader.fromJson(data);
  }

  /// Unwraps a `{ "data": [...] }` success envelope into a list.
  List<dynamic> _dataList(Response<Map<String, dynamic>> response) {
    final body = response.data;
    if (body == null || body['data'] is! List) {
      throw DioException(
        requestOptions: response.requestOptions,
        response: response,
        message: 'Malformed success envelope (missing "data" array).',
      );
    }
    return body['data'] as List<dynamic>;
  }
}
