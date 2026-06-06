import 'package:flutter/material.dart';

import '../../../../l10n/generated/app_localizations.dart';

/// Pill chip for an attendance status. Conveys meaning by icon + label (not
/// color alone) per the accessibility guideline.
class AttendanceStatusChip extends StatelessWidget {
  const AttendanceStatusChip({super.key, required this.status});

  final String status;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final (Color bg, Color fg, IconData icon, String label) = switch (status) {
      'valid' => (const Color(0xFFE7F4EA), const Color(0xFF1B5E20), Icons.check_circle_outline, l.attendanceStatusValid),
      'late' => (const Color(0xFFFFF4E5), const Color(0xFF8A5300), Icons.schedule, l.attendanceStatusLate),
      'gps_violation_pending_review' => (const Color(0xFFFFF0E6), const Color(0xFFB5430E), Icons.location_off_outlined, l.attendanceStatusGpsReview),
      'face_fail_pending_review' => (const Color(0xFFFFF0E6), const Color(0xFFB5430E), Icons.sentiment_dissatisfied_outlined, l.attendanceStatusFaceReview),
      'fake_gps_blocked' => (const Color(0xFFFDECEA), const Color(0xFFB3261E), Icons.block, l.attendanceStatusFakeGps),
      'admin_approved' => (const Color(0xFFE0F7FA), const Color(0xFF00696E), Icons.verified_outlined, l.attendanceStatusAdminApproved),
      'admin_rejected' => (const Color(0xFFEDEDED), const Color(0xFF5F5F5F), Icons.cancel_outlined, l.attendanceStatusAdminRejected),
      _ => (const Color(0xFFE8F0FE), const Color(0xFF174EA6), Icons.help_outline, status),
    };
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(999)),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 14, color: fg),
          const SizedBox(width: 4),
          Text(label, style: TextStyle(color: fg, fontWeight: FontWeight.w600, fontSize: 12)),
        ],
      ),
    );
  }
}
