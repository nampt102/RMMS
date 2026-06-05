import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/assigned_leader.dart';
import '../domain/assigned_store.dart';
import 'organization_api.dart';

final organizationApiProvider = Provider<OrganizationApi>((ref) {
  return OrganizationApi(ref.watch(dioProvider));
});

final organizationRepositoryProvider = Provider<OrganizationRepository>((ref) {
  return OrganizationRepository(ref.watch(organizationApiProvider));
});

/// Self-service organization reads for the mobile app (M03). All Dio failures
/// are normalized to [ApiException] so callers branch on a stable error code.
class OrganizationRepository {
  OrganizationRepository(this._api);

  final OrganizationApi _api;

  Future<List<AssignedStore>> myStores() => _guard(_api.myStores);

  Future<AssignedLeader?> myLeader() => _guard(_api.myLeader);

  Future<T> _guard<T>(Future<T> Function() action) async {
    try {
      return await action();
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}

/// Assigned stores for the signed-in user. Auto-refetches when invalidated.
final myStoresProvider = FutureProvider.autoDispose<List<AssignedStore>>((ref) {
  return ref.watch(organizationRepositoryProvider).myStores();
});

/// Active Leader for the signed-in PG (null if none / non-PG).
final myLeaderProvider = FutureProvider.autoDispose<AssignedLeader?>((ref) {
  return ref.watch(organizationRepositoryProvider).myLeader();
});
