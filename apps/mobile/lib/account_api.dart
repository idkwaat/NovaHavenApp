import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;

class AccountUser {
  const AccountUser(
      {required this.id, required this.email, required this.isAdmin});
  final String id;
  final String email;
  final bool isAdmin;
  factory AccountUser.fromJson(Map<String, dynamic> json) => AccountUser(
        id: json['id'] as String,
        email: json['email'] as String,
        isAdmin: json['isAdmin'] as bool? ?? false,
      );
}

class AccountNotification {
  const AccountNotification(
      {required this.id,
      required this.title,
      required this.body,
      required this.href,
      required this.createdAtUtc,
      required this.readAtUtc});
  final String id;
  final String title;
  final String body;
  final String? href;
  final DateTime createdAtUtc;
  final DateTime? readAtUtc;
  bool get isRead => readAtUtc != null;
  factory AccountNotification.fromJson(Map<String, dynamic> json) =>
      AccountNotification(
        id: json['id'] as String,
        title: json['title'] as String,
        body: json['body'] as String,
        href: json['href'] as String?,
        createdAtUtc: DateTime.parse(json['createdAtUtc'] as String).toUtc(),
        readAtUtc: json['readAtUtc'] == null
            ? null
            : DateTime.parse(json['readAtUtc'] as String).toUtc(),
      );
}

class AccountInbox {
  const AccountInbox(
      {required this.items,
      required this.page,
      required this.pageSize,
      required this.total,
      required this.unreadCount});
  final List<AccountNotification> items;
  final int page;
  final int pageSize;
  final int total;
  final int unreadCount;
  factory AccountInbox.fromJson(Map<String, dynamic> json) => AccountInbox(
        items: (json['items'] as List<dynamic>? ?? [])
            .map((item) =>
                AccountNotification.fromJson(item as Map<String, dynamic>))
            .toList(),
        page: (json['page'] as num? ?? 1).toInt(),
        pageSize: (json['pageSize'] as num? ?? 20).toInt(),
        total: (json['total'] as num? ?? 0).toInt(),
        unreadCount: (json['unreadCount'] as num? ?? 0).toInt(),
      );
}

class AccountAuthenticationRequired implements Exception {
  const AccountAuthenticationRequired();
}

abstract interface class AccountSecretStore {
  Future<String?> read(String key);
  Future<void> write(String key, String value);
  Future<void> delete(String key);
}

class FlutterAccountSecretStore implements AccountSecretStore {
  FlutterAccountSecretStore([FlutterSecureStorage? storage])
      : _storage = storage ?? const FlutterSecureStorage();
  final FlutterSecureStorage _storage;
  @override
  Future<String?> read(String key) => _storage.read(key: key);
  @override
  Future<void> write(String key, String value) =>
      _storage.write(key: key, value: value);
  @override
  Future<void> delete(String key) => _storage.delete(key: key);
}

/// Cookie/CSRF account client. Identity cookies are kept in platform secure storage,
/// never shared with the webview or saved in ordinary preferences.
class UserAccountApi {
  UserAccountApi(
      {required Uri base, AccountSecretStore? storage, http.Client? client})
      : _base = base,
        _storage = storage ?? FlutterAccountSecretStore(),
        _client = client ?? http.Client(),
        _ownsClient = client == null;

  final Uri _base;
  final AccountSecretStore _storage;
  final http.Client _client;
  final bool _ownsClient;
  static const _cookieKey = 'nova_haven.identity.cookies.v1';

  Future<Map<String, String>> _cookies() async {
    final raw = await _storage.read(_cookieKey);
    if (raw == null) return {};
    try {
      return (jsonDecode(raw) as Map<String, dynamic>)
          .map((key, value) => MapEntry(key, value as String));
    } on FormatException {
      await _storage.delete(_cookieKey);
      return {};
    }
  }

  Future<void> _saveResponseCookies(http.Response response) async {
    final raw = response.headers['set-cookie'];
    if (raw == null || raw.isEmpty) return;
    final cookies = await _cookies();
    final matcher = RegExp(r'(?:^|,\s*)(\.AspNetCore\.[^=;,\s]+)=([^;]*)');
    for (final match in matcher.allMatches(raw)) {
      cookies[match.group(1)!] = match.group(2)!;
    }
    await _storage.write(_cookieKey, jsonEncode(cookies));
  }

  Future<Map<String, String>> _headers({bool json = false}) async {
    final cookies = await _cookies();
    return {
      'Accept': 'application/json',
      if (json) 'Content-Type': 'application/json',
      if (cookies.isNotEmpty)
        'Cookie': cookies.entries
            .map((entry) => '${entry.key}=${entry.value}')
            .join('; '),
    };
  }

  Future<Map<String, dynamic>> _json(http.Response response) async {
    await _saveResponseCookies(response);
    final decoded = jsonDecode(utf8.decode(response.bodyBytes));
    if (decoded is! Map<String, dynamic>) {
      throw const FormatException('Phản hồi tài khoản không hợp lệ.');
    }
    return decoded;
  }

  Future<String> _csrf() async {
    final response = await _client
        .get(_base.resolve('api/v1/auth/csrf'), headers: await _headers())
        .timeout(const Duration(seconds: 10));
    final json = await _json(response);
    if (response.statusCode != 200 || json['token'] is! String) {
      throw StateError('Không lấy được mã bảo vệ phiên.');
    }
    return json['token'] as String;
  }

  Future<http.Response> _mutate(String path,
      {required String method, Object? body}) async {
    final token = await _csrf();
    final headers = await _headers(json: body != null);
    headers['X-CSRF-TOKEN'] = token;
    final uri = _base.resolve(path);
    final response = switch (method) {
      'PUT' => await _client.put(uri,
          headers: headers, body: body == null ? null : jsonEncode(body)),
      'DELETE' => await _client.delete(uri,
          headers: headers, body: body == null ? null : jsonEncode(body)),
      _ => await _client.post(uri,
          headers: headers, body: body == null ? null : jsonEncode(body)),
    };
    await _saveResponseCookies(response);
    return response;
  }

  Future<void> _expectSuccess(
      http.Response response, Set<int> accepted, String message) async {
    if (accepted.contains(response.statusCode)) return;
    final detail = <String, dynamic>{};
    try {
      detail.addAll(
          jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>);
    } catch (_) {}
    final errors = detail['errors'];
    String? firstValidation;
    if (errors is Map) {
      for (final value in errors.values) {
        if (value is List && value.isNotEmpty) {
          firstValidation = value.first.toString();
          break;
        }
      }
    }
    throw StateError(
        (detail['detail'] ?? firstValidation ?? detail['title'] ?? message)
            .toString());
  }

  Future<AccountUser> register(String email, String password) async {
    await _expectSuccess(
        await _mutate('api/v1/auth/register',
            method: 'POST',
            body: {'email': email.trim(), 'password': password}),
        {201},
        'Không thể tạo tài khoản.');
    final user = await currentUser();
    if (user == null) throw StateError('Phiên đăng nhập chưa được tạo.');
    return user;
  }

  Future<AccountUser> login(String email, String password) async {
    await _expectSuccess(
        await _mutate('api/v1/auth/login',
            method: 'POST',
            body: {'email': email.trim(), 'password': password}),
        {204},
        'Email hoặc mật khẩu không đúng.');
    final user = await currentUser();
    if (user == null) throw StateError('Phiên đăng nhập chưa được tạo.');
    return user;
  }

  Future<AccountUser?> currentUser() async {
    final response = await _client
        .get(_base.resolve('api/v1/auth/me'), headers: await _headers())
        .timeout(const Duration(seconds: 10));
    await _saveResponseCookies(response);
    if (response.statusCode == 401) return null;
    if (response.statusCode != 200) {
      throw StateError(
          'Không đọc được tài khoản (HTTP ${response.statusCode}).');
    }
    final json = jsonDecode(utf8.decode(response.bodyBytes));
    return AccountUser.fromJson(json as Map<String, dynamic>);
  }

  Future<AccountInbox> notifications({int page = 1, int pageSize = 20}) async {
    if (page < 1 || pageSize < 1 || pageSize > 50) {
      throw ArgumentError('Trang thông báo không hợp lệ.');
    }
    final response = await _client
        .get(
            _base.resolve('api/v1/notifications?page=$page&pageSize=$pageSize'),
            headers: await _headers())
        .timeout(const Duration(seconds: 10));
    await _saveResponseCookies(response);
    if (response.statusCode == 401) throw const AccountAuthenticationRequired();
    if (response.statusCode != 200) {
      throw StateError(
          'Không tải được thông báo (HTTP ${response.statusCode}).');
    }
    return AccountInbox.fromJson(
        jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>);
  }

  Future<void> markRead(String id) async => _expectSuccess(
      await _mutate('api/v1/notifications/${Uri.encodeComponent(id)}/read',
          method: 'PUT'),
      {204},
      'Không thể đánh dấu đã đọc.');

  Future<void> markAllRead() async => _expectSuccess(
      await _mutate('api/v1/notifications/read-all', method: 'POST'),
      {204},
      'Không thể đánh dấu đã đọc.');

  Future<void> logout() async {
    await _expectSuccess(await _mutate('api/v1/auth/logout', method: 'POST'),
        {204}, 'Không thể đăng xuất.');
    await _storage.delete(_cookieKey);
  }

  Future<void> dispose() async {
    if (_ownsClient) _client.close();
  }
}
