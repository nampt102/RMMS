import 'dart:math';

/// Generates a RFC-4122 version-4 UUID using a cryptographically secure RNG.
///
/// We avoid pulling in the `uuid` package for a single helper. This is used for
/// the install-scoped device fingerprint (sprint-01 R-7): generated once on
/// first launch, persisted in secure storage, so a reinstall yields a new id
/// and goes through the device-approval flow (BR-105 / BR-106).
String generateUuidV4() {
  final rnd = Random.secure();
  final bytes = List<int>.generate(16, (_) => rnd.nextInt(256));

  // Set version (4) and variant (RFC 4122) bits.
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;

  String hex(int b) => b.toRadixString(16).padLeft(2, '0');
  final h = bytes.map(hex).join();

  return '${h.substring(0, 8)}-${h.substring(8, 12)}-${h.substring(12, 16)}'
      '-${h.substring(16, 20)}-${h.substring(20)}';
}
