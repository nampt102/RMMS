/// Plain model for the Document Center (M13). Mirrors `Rmms.Application.Documents.DocumentDto`.
class DocumentItem {
  const DocumentItem({
    required this.id,
    required this.name,
    this.description,
    required this.folderType, // 'public' | 'private'
    required this.fileSizeBytes,
    required this.mimeType,
    required this.createdAt,
  });

  final String id;
  final String name;
  final String? description;
  final String folderType;
  final int fileSizeBytes;
  final String mimeType;
  final DateTime createdAt;

  bool get isPrivate => folderType == 'private';

  String get sizeLabel {
    if (fileSizeBytes < 1024) return '$fileSizeBytes B';
    if (fileSizeBytes < 1024 * 1024) return '${(fileSizeBytes / 1024).toStringAsFixed(0)} KB';
    return '${(fileSizeBytes / 1024 / 1024).toStringAsFixed(1)} MB';
  }

  factory DocumentItem.fromJson(Map<String, dynamic> j) => DocumentItem(
        id: j['id'] as String,
        name: j['name'] as String? ?? '',
        description: j['description'] as String?,
        folderType: j['folderType'] as String? ?? 'public',
        fileSizeBytes: (j['fileSizeBytes'] as num?)?.toInt() ?? 0,
        mimeType: j['mimeType'] as String? ?? '',
        createdAt: DateTime.tryParse(j['createdAt'] as String? ?? '') ?? DateTime.now(),
      );
}
