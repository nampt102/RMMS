import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/news_models.dart';
import 'news_api.dart';

final newsApiProvider = Provider<NewsApi>((ref) => NewsApi(ref.watch(dioProvider)));

final newsRepositoryProvider = Provider<NewsRepository>((ref) {
  return NewsRepository(ref.watch(newsApiProvider));
});

/// Mobile News (M14). Dio failures → [ApiException].
class NewsRepository {
  NewsRepository(this._api);
  final NewsApi _api;

  Future<List<NewsItem>> myNews() => _guard(_api.myNews);
  Future<void> markRead(String id) => _guard(() => _api.markRead(id));
  Future<void> confirm(String id) => _guard(() => _api.confirm(id));

  Future<T> _guard<T>(Future<T> Function() action) async {
    try {
      return await action();
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}

/// Published news assigned to the current user.
final myNewsProvider = FutureProvider.autoDispose<List<NewsItem>>((ref) {
  return ref.watch(newsRepositoryProvider).myNews();
});

/// Count of news needing the reader's attention (unread, or important-not-confirmed) for a badge.
final unreadNewsCountProvider = Provider.autoDispose<int>((ref) {
  final async = ref.watch(myNewsProvider);
  return async.maybeWhen(
    data: (items) => items.where((n) => !n.isRead || n.needsAction).length,
    orElse: () => 0,
  );
});
