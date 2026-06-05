import 'package:freezed_annotation/freezed_annotation.dart';

part 'assigned_store.freezed.dart';
part 'assigned_store.g.dart';

/// A store assigned to the current user — mirrors `MyStoreDto` from
/// `GET /api/v1/users/me/stores` (M03). Used by PG/Leader to know where they work.
@freezed
sealed class AssignedStore with _$AssignedStore {
  const factory AssignedStore({
    required String id,
    required String code,
    required String name,
    String? address,
    required double latitude,
    required double longitude,
    @Default('active') String status,
  }) = _AssignedStore;

  factory AssignedStore.fromJson(Map<String, dynamic> json) =>
      _$AssignedStoreFromJson(json);
}
