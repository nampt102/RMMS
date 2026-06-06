import 'package:dio/dio.dart';

import '../../../core/utils/app_uuid.dart';
import '../domain/attendance.dart';

/// GPS + photo payload for a check-in / check-out submission.
class AttendanceSubmission {
  const AttendanceSubmission({
    required this.latitude,
    required this.longitude,
    this.accuracyMeters,
    this.fakeGpsDetected = false,
    this.selfiePath,
    this.storePhotoPath,
    this.note,
  });

  final double latitude;
  final double longitude;
  final double? accuracyMeters;
  final bool fakeGpsDetected;
  final String? selfiePath;
  final String? storePhotoPath;
  final String? note;

  Future<Map<String, dynamic>> toFormMap({String? storeId}) async {
    return {
      if (storeId != null) 'storeId': storeId,
      'latitude': latitude,
      'longitude': longitude,
      if (accuracyMeters != null) 'accuracyMeters': accuracyMeters,
      'fakeGpsDetected': fakeGpsDetected,
      if (note != null && note!.trim().isNotEmpty) 'note': note!.trim(),
      if (selfiePath != null)
        'selfie': await MultipartFile.fromFile(selfiePath!, filename: 'selfie.jpg'),
      if (storePhotoPath != null)
        'storePhoto':
            await MultipartFile.fromFile(storePhotoPath!, filename: 'store.jpg'),
    };
  }
}

/// Hand-written Dio client for the M05 attendance self-service surface.
class AttendanceApi {
  AttendanceApi(this._dio);

  final Dio _dio;

  /// Today's expected shifts + their status — `GET /attendance/today`.
  Future<List<TodayShift>> today() async {
    final response = await _dio.get<Map<String, dynamic>>('/attendance/today');
    return _dataList(response)
        .whereType<Map<String, dynamic>>()
        .map(TodayShift.fromJson)
        .toList(growable: false);
  }

  /// Assigned stores + thresholds + today's shifts — `GET /attendance/check-in/info`.
  Future<CheckInInfo> checkInInfo() async {
    final response =
        await _dio.get<Map<String, dynamic>>('/attendance/check-in/info');
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw DioException(
        requestOptions: response.requestOptions,
        response: response,
        message: 'Malformed success envelope (missing "data" object).',
      );
    }
    return CheckInInfo.fromJson(data);
  }

  /// Submit a check-in (multipart) — `POST /attendance/check-in`.
  Future<AttendanceRecord> checkIn(
      String storeId, AttendanceSubmission submission) async {
    final form = FormData.fromMap(await submission.toFormMap(storeId: storeId));
    final response = await _dio.post<Map<String, dynamic>>(
      '/attendance/check-in',
      data: form,
      options: _idempotent(),
    );
    return _record(response);
  }

  /// Submit a check-out (multipart) — `POST /attendance/{id}/check-out`.
  Future<AttendanceRecord> checkOut(
      String attendanceId, AttendanceSubmission submission) async {
    final form = FormData.fromMap(await submission.toFormMap());
    final response = await _dio.post<Map<String, dynamic>>(
      '/attendance/$attendanceId/check-out',
      data: form,
      options: _idempotent(),
    );
    return _record(response);
  }

  /// The caller's attendance history (paginated) — `GET /attendance/history`.
  Future<List<AttendanceRecord>> history({
    String? from,
    String? to,
    int page = 1,
    int pageSize = 30,
  }) async {
    final response = await _dio.get<Map<String, dynamic>>(
      '/attendance/history',
      queryParameters: {
        if (from != null) 'from': from,
        if (to != null) 'to': to,
        'page': page,
        'pageSize': pageSize,
      },
    );
    // Paginated envelope: { data: { data: [...], meta: {...} } }.
    final outer = response.data?['data'];
    final list = outer is Map<String, dynamic> ? outer['data'] : null;
    if (list is! List) {
      throw DioException(
        requestOptions: response.requestOptions,
        response: response,
        message: 'Malformed paginated envelope.',
      );
    }
    return list
        .whereType<Map<String, dynamic>>()
        .map(AttendanceRecord.fromJson)
        .toList(growable: false);
  }

  AttendanceRecord _record(Response<Map<String, dynamic>> response) {
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw DioException(
        requestOptions: response.requestOptions,
        response: response,
        message: 'Malformed success envelope (missing "data" object).',
      );
    }
    return AttendanceRecord.fromJson(data);
  }

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

  Options _idempotent() =>
      Options(headers: {'X-Idempotency-Key': generateUuidV4()});
}
