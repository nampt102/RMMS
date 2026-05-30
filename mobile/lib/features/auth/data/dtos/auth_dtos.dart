import 'package:freezed_annotation/freezed_annotation.dart';

part 'auth_dtos.freezed.dart';
part 'auth_dtos.g.dart';

/// Response of `POST /auth/login` (the `data` envelope payload).
/// Mirrors `Rmms.Application.Auth.Login.LoginResponse`.
@freezed
sealed class LoginResponseDto with _$LoginResponseDto {
  const factory LoginResponseDto({
    required String accessToken,
    required DateTime accessTokenExpiresAt,
    required String refreshToken,
    required DateTime refreshTokenExpiresAt,
    required LoginUserDto user,
  }) = _LoginResponseDto;

  factory LoginResponseDto.fromJson(Map<String, dynamic> json) =>
      _$LoginResponseDtoFromJson(json);
}

/// Mirrors `Rmms.Application.Auth.Login.LoginUserInfo`.
@freezed
sealed class LoginUserDto with _$LoginUserDto {
  const factory LoginUserDto({
    required String userId,
    required String email,
    required String fullName,
    required String role,
    required String status,
    required String preferredLanguage,
  }) = _LoginUserDto;

  factory LoginUserDto.fromJson(Map<String, dynamic> json) =>
      _$LoginUserDtoFromJson(json);
}

/// Response of `POST /auth/refresh`.
/// Mirrors `Rmms.Application.Auth.Refresh.RefreshTokenResponse`.
@freezed
sealed class RefreshResponseDto with _$RefreshResponseDto {
  const factory RefreshResponseDto({
    required String accessToken,
    required DateTime accessTokenExpiresAt,
    required String refreshToken,
    required DateTime refreshTokenExpiresAt,
  }) = _RefreshResponseDto;

  factory RefreshResponseDto.fromJson(Map<String, dynamic> json) =>
      _$RefreshResponseDtoFromJson(json);
}

/// Response of `POST /auth/register`.
/// Mirrors `Rmms.Application.Auth.Register.RegisterUserResponse`.
/// No tokens are issued — the user must verify their email then log in.
@freezed
sealed class RegisterResponseDto with _$RegisterResponseDto {
  const factory RegisterResponseDto({
    required String userId,
    required String email,
    required String status,
  }) = _RegisterResponseDto;

  factory RegisterResponseDto.fromJson(Map<String, dynamic> json) =>
      _$RegisterResponseDtoFromJson(json);
}

/// Response of `POST /auth/verify-email`.
/// Mirrors `Rmms.Application.Auth.VerifyEmail.VerifyEmailResponse`.
@freezed
sealed class VerifyEmailResponseDto with _$VerifyEmailResponseDto {
  const factory VerifyEmailResponseDto({
    required String userId,
    required String email,
    required String status,
  }) = _VerifyEmailResponseDto;

  factory VerifyEmailResponseDto.fromJson(Map<String, dynamic> json) =>
      _$VerifyEmailResponseDtoFromJson(json);
}

/// Response of `GET /auth/me`. Mirrors `Rmms.Application.Auth.Me.MeDto`.
@freezed
sealed class MeDto with _$MeDto {
  const factory MeDto({
    required String id,
    required String email,
    required String fullName,
    String? phone,
    required String role,
    required String status,
    required String preferredLanguage,
    DateTime? emailVerifiedAt,
    DateTime? lastLoginAt,
    MeDeviceDto? currentDevice,
  }) = _MeDto;

  factory MeDto.fromJson(Map<String, dynamic> json) => _$MeDtoFromJson(json);
}

/// Mirrors `Rmms.Application.Auth.Me.MeDeviceDto`.
@freezed
sealed class MeDeviceDto with _$MeDeviceDto {
  const factory MeDeviceDto({
    required String id,
    required String deviceId,
    required String deviceName,
    required String os,
    required String osVersion,
    required String appVersion,
    required String status,
    DateTime? lastUsedAt,
  }) = _MeDeviceDto;

  factory MeDeviceDto.fromJson(Map<String, dynamic> json) =>
      _$MeDeviceDtoFromJson(json);
}
