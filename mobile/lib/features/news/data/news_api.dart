import 'package:dio/dio.dart';

import '../domain/news_models.dart';

/// Mobile News surface (M14). Base path /api/v1 set on the Dio instance.
class NewsApi {
  NewsApi(this._dio);
  final Dio _dio;

  /// GET /news/me — published news assigned to the current user (with read/confirm state).
  Future<List<NewsItem>> myNews() async {
    final res = await _dio.get<Map<String, dynamic>>('/news/me');
    return _list(res).whereType<Map<String, dynamic>>().map(NewsItem.fromJson).toList(growable: false);
  }

  /// POST /news/:id/read — mark as read.
  Future<void> markRead(String id) async {
    await _dio.post<void>('/news/$id/read');
  }

  /// POST /news/:id/confirm — acknowledge an important news item (AC-34).
  Future<void> confirm(String id) async {
    await _dio.post<void>('/news/$id/confirm');
  }

  List<dynamic> _list(Response<Map<String, dynamic>> res) {
    final body = res.data;
    if (body == null || body['data'] is! List) {
      throw DioException(requestOptions: res.requestOptions, response: res, message: 'Malformed envelope.');
    }
    return body['data'] as List<dynamic>;
  }
}
