/// Plain model for News (M14). Mirrors `Rmms.Application.News.NewsDto` (includes this
/// user's read/confirm state).
class NewsItem {
  const NewsItem({
    required this.id,
    required this.titleVi,
    required this.titleEn,
    required this.contentVi,
    required this.contentEn,
    this.category,
    required this.isImportant,
    this.publishedAt,
    required this.isRead,
    required this.isConfirmed,
  });

  final String id;
  final String titleVi;
  final String titleEn;
  final String contentVi;
  final String contentEn;
  final String? category;
  final bool isImportant;
  final DateTime? publishedAt;
  final bool isRead;
  final bool isConfirmed;

  String localizedTitle(String lang) => lang == 'en' ? titleEn : titleVi;
  String localizedContent(String lang) => lang == 'en' ? contentEn : contentVi;

  /// Important news must be acknowledged; it stays "unread" until confirmed.
  bool get needsAction => isImportant && !isConfirmed;

  factory NewsItem.fromJson(Map<String, dynamic> j) => NewsItem(
        id: j['id'] as String,
        titleVi: j['titleVi'] as String? ?? '',
        titleEn: j['titleEn'] as String? ?? '',
        contentVi: j['contentVi'] as String? ?? '',
        contentEn: j['contentEn'] as String? ?? '',
        category: j['category'] as String?,
        isImportant: j['isImportant'] == true,
        publishedAt: j['publishedAt'] == null ? null : DateTime.tryParse(j['publishedAt'] as String),
        isRead: j['isRead'] == true,
        isConfirmed: j['isConfirmed'] == true,
      );
}
