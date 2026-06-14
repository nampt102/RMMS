import 'package:dio/dio.dart';

import '../../../core/utils/app_uuid.dart';
import '../domain/face_status.dart';

/// Hand-written Dio client for the M06 self-service Face Verification surface
/// (ADR-011 CompreFace). Enrollment sends 1..5 face angles as multipart.
class FaceApi {
  FaceApi(this._dio);

  final Dio _dio;

  /// The caller's enrollment status — `GET /face/status`.
  Future<FaceStatus> status() async {
    final response = await _dio.get<Map<String, dynamic>>('/face/status');
    return FaceStatus.fromJson(_data(response));
  }

  /// Enroll the caller's face from captured angle photos — `POST /face/enroll`.
  /// Replaces any prior enrollment server-side (delete-then-enroll).
  Future<FaceStatus> enroll(List<String> photoPaths) async {
    final files = <MapEntry<String, MultipartFile>>[];
    for (var i = 0; i < photoPaths.length; i++) {
      files.add(MapEntry(
        'photos',
        await MultipartFile.fromFile(photoPaths[i], filename: 'face_$i.jpg'),
      ));
    }
    final form = FormData()..files.addAll(files);
    final response = await _dio.post<Map<String, dynamic>>(
      '/face/enroll',
      data: form,
      options: Options(headers: {'X-Idempotency-Key': generateUuidV4()}),
    );
    return FaceStatus.fromJson(_data(response));
  }

  /// Remove the caller's enrollment — `DELETE /face`. Returns 204 No Content.
  Future<void> remove() async {
    await _dio.delete<void>('/face');
  }

  Map<String, dynamic> _data(Response<Map<String, dynamic>> response) {
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw DioException(
        requestOptions: response.requestOptions,
        response: response,
        message: 'Malformed success envelope (missing "data" object).',
      );
    }
    return data;
  }
}
