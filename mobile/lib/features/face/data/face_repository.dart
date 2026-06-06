import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/face_status.dart';
import 'face_api.dart';

final faceApiProvider = Provider<FaceApi>((ref) {
  return FaceApi(ref.watch(dioProvider));
});

final faceRepositoryProvider = Provider<FaceRepository>((ref) {
  return FaceRepository(ref.watch(faceApiProvider));
});

/// Self-service Face Verification for the mobile app (M06). Dio failures are
/// normalised to [ApiException] so callers branch on a stable error code.
class FaceRepository {
  FaceRepository(this._api);

  final FaceApi _api;

  Future<FaceStatus> status() => _guard(_api.status);

  Future<FaceStatus> enroll(List<String> photoPaths) =>
      _guard(() => _api.enroll(photoPaths));

  Future<T> _guard<T>(Future<T> Function() action) async {
    try {
      return await action();
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}

/// The caller's current face-enrollment status. Invalidate after enrolling.
final faceStatusProvider = FutureProvider.autoDispose<FaceStatus>((ref) {
  return ref.watch(faceRepositoryProvider).status();
});
