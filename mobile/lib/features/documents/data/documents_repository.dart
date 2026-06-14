import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/document_models.dart';
import 'documents_api.dart';

final documentsApiProvider = Provider<DocumentsApi>((ref) => DocumentsApi(ref.watch(dioProvider)));

final documentsRepositoryProvider = Provider<DocumentsRepository>((ref) {
  return DocumentsRepository(ref.watch(documentsApiProvider));
});

/// Mobile Document Center (M13). Dio failures → [ApiException].
class DocumentsRepository {
  DocumentsRepository(this._api);
  final DocumentsApi _api;

  Future<List<DocumentItem>> myDocuments({String? search}) => _guard(() => _api.myDocuments(search: search));

  Future<String> downloadUrl(String id) => _guard(() => _api.downloadUrl(id));

  Future<T> _guard<T>(Future<T> Function() action) async {
    try {
      return await action();
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}

/// Documents accessible to the current user (optionally name-filtered).
final myDocumentsProvider = FutureProvider.autoDispose.family<List<DocumentItem>, String?>((ref, search) {
  return ref.watch(documentsRepositoryProvider).myDocuments(search: search);
});
