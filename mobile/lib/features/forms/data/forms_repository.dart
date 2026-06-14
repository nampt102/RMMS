import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/form_models.dart';
import 'forms_api.dart';

final formsApiProvider = Provider<FormsApi>((ref) => FormsApi(ref.watch(dioProvider)));

final formsRepositoryProvider = Provider<FormsRepository>((ref) {
  return FormsRepository(ref.watch(formsApiProvider));
});

/// Mobile fill surface for the Form Engine (M10). Dio failures → [ApiException].
class FormsRepository {
  FormsRepository(this._api);

  final FormsApi _api;

  Future<List<AssignedForm>> myForms() => _guard(_api.myForms);

  Future<FormFill> getForm(String id) => _guard(() => _api.getForm(id));

  Future<List<ProductLite>> products() => _guard(_api.products);

  Future<({String objectKey, String? url})> uploadAttachment(String formId, String filePath) =>
      _guard(() => _api.uploadAttachment(formId, filePath));

  Future<String> submit({
    required String formId,
    required Map<String, dynamic> answers,
    String? storeId,
    required int timeSpentSeconds,
    required String clientIdempotencyKey,
  }) =>
      _guard(() => _api.submit(
            formId: formId,
            answers: answers,
            storeId: storeId,
            timeSpentSeconds: timeSpentSeconds,
            clientIdempotencyKey: clientIdempotencyKey,
          ));

  Future<T> _guard<T>(Future<T> Function() action) async {
    try {
      return await action();
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}

/// Forms assigned to the current user.
final myFormsProvider = FutureProvider.autoDispose<List<AssignedForm>>((ref) {
  return ref.watch(formsRepositoryProvider).myForms();
});

/// A single form's schema to render.
final formFillProvider = FutureProvider.autoDispose.family<FormFill, String>((ref, id) {
  return ref.watch(formsRepositoryProvider).getForm(id);
});

/// Products for product/SKU selector fields (cached while a fill screen is open).
final formProductsProvider = FutureProvider.autoDispose<List<ProductLite>>((ref) {
  return ref.watch(formsRepositoryProvider).products();
});
