import 'package:dio/dio.dart';

import '../domain/document_models.dart';

/// Mobile Document Center surface (M13). Base path /api/v1 set on the Dio instance.
class DocumentsApi {
  DocumentsApi(this._dio);
  final Dio _dio;

  /// GET /documents/me?search= — documents accessible to the current user.
  Future<List<DocumentItem>> myDocuments({String? search}) async {
    final res = await _dio.get<Map<String, dynamic>>(
      '/documents/me',
      queryParameters: {if (search != null && search.isNotEmpty) 'search': search},
    );
    return _list(res).whereType<Map<String, dynamic>>().map(DocumentItem.fromJson).toList(growable: false);
  }

  /// GET /documents/:id/download — mint a short-lived signed URL.
  Future<String> downloadUrl(String id) async {
    final res = await _dio.get<Map<String, dynamic>>('/documents/$id/download');
    final url = _data(res)['url'];
    if (url is! String || url.isEmpty) {
      throw DioException(requestOptions: res.requestOptions, response: res, message: 'No download URL.');
    }
    return url;
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
