import 'dart:convert';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class BookmarkStore {
  BookmarkStore({FlutterSecureStorage? storage}) : _storage = storage ?? const FlutterSecureStorage();
  static const _key = 'nova_haven_bookmarks';
  final FlutterSecureStorage _storage;

  Future<Set<String>> read() async {
    final raw = await _storage.read(key: _key);
    if (raw == null || raw.isEmpty) return <String>{};
    try {
      final decoded = jsonDecode(raw);
      if (decoded is! List<dynamic>) return <String>{};
      return decoded.whereType<String>().where((slug) => slug.isNotEmpty).toSet();
    } catch (_) {
      return <String>{};
    }
  }

  Future<Set<String>> toggle(String slug) async {
    final next = await read();
    if (!next.add(slug)) next.remove(slug);
    final sorted = next.toList()..sort();
    await _storage.write(key: _key, value: jsonEncode(sorted));
    return next;
  }
}
