import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:nova_haven_companion/account_api.dart';
import 'package:nova_haven_companion/account_screens.dart';
import 'package:nova_haven_companion/nova_theme.dart';

class MemoryAccountSecretStore implements AccountSecretStore {
  @override
  Future<String?> read(String key) async => null;
  @override
  Future<void> write(String key, String value) async {}
  @override
  Future<void> delete(String key) async {}
}

void main() {
  testWidgets('account center confirms email from the Development local outbox',
      (tester) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    var confirmRequestSeen = false;
    final client = MockClient((request) async {
      if (request.url.path.endsWith('/auth/me')) return http.Response('', 401);
      if (request.url.path.endsWith('/dev/mailbox')) {
        return http.Response.bytes(
            utf8.encode(jsonEncode([
              {
                'id': 'message-1',
                'recipient': 'player@example.test',
                'subject': 'Xác nhận tài khoản Nova Haven',
                'confirmationUrl':
                    'https://nova.example/account?email=player%40example.test&token=local%2Btoken%3D',
                'createdAtUtc': '2026-09-29T12:00:00Z',
              }
            ])),
            200,
            headers: {'content-type': 'application/json; charset=utf-8'});
      }
      if (request.url.path.endsWith('/auth/csrf')) {
        return http.Response('{"token":"csrf-token"}', 200);
      }
      if (request.url.path.endsWith('/auth/confirm-email')) {
        confirmRequestSeen = true;
        final body = jsonDecode(request.body) as Map<String, dynamic>;
        expect(body['email'], 'player@example.test');
        expect(body['token'], 'local+token=');
        return http.Response('', 204);
      }
      throw StateError('Unexpected request: ${request.method} ${request.url}');
    });
    final api = UserAccountApi(
      base: Uri.parse('https://nova.example/'),
      storage: MemoryAccountSecretStore(),
      client: client,
    );
    await tester.pumpWidget(
        MaterialApp(theme: NovaTheme.dark, home: AccountCenterPage(api: api)));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Tạo tài khoản'));
    await tester.pumpAndSettle();
    await tester.enterText(
        find.byType(TextFormField).first, 'player@example.test');
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('load-local-confirmation')));
    await tester.pumpAndSettle();
    expect(
        find.text(
            'Đã tải thư xác nhận local. Bạn có thể xác nhận email ngay bên dưới.'),
        findsOneWidget);
    await tester.tap(find.text('Xác nhận email bằng mã'));
    await tester.pumpAndSettle();

    expect(confirmRequestSeen, isTrue);
    expect(find.text('Email đã được xác nhận. Bạn có thể đăng nhập.'),
        findsOneWidget);
    client.close();
  });

  testWidgets('unauthenticated notification inbox offers account navigation',
      (tester) async {
    final client = MockClient((request) async {
      if (request.url.path.endsWith('/notifications')) {
        return http.Response('', 401);
      }
      if (request.url.path.endsWith('/auth/me')) return http.Response('', 401);
      throw StateError('Unexpected request: ${request.method} ${request.url}');
    });
    final api = UserAccountApi(
      base: Uri.parse('https://nova.example/'),
      storage: MemoryAccountSecretStore(),
      client: client,
    );
    await tester.pumpWidget(MaterialApp(
        theme: NovaTheme.dark, home: AccountNotificationsPage(api: api)));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Đăng nhập hoặc tạo tài khoản'));
    await tester.pumpAndSettle();

    expect(find.text('Tài khoản Nova Haven'), findsOneWidget);
    expect(find.text('Tài khoản phiêu lưu'), findsOneWidget);
    client.close();
  });

  testWidgets('email confirmation rejects a missing address and token locally',
      (tester) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final paths = <String>[];
    final client = MockClient((request) async {
      paths.add(request.url.path);
      if (request.url.path.endsWith('/auth/me')) return http.Response('', 401);
      throw StateError('Confirmation should not be sent with empty fields.');
    });
    final api = UserAccountApi(
      base: Uri.parse('https://nova.example/'),
      storage: MemoryAccountSecretStore(),
      client: client,
    );
    await tester.pumpWidget(
        MaterialApp(theme: NovaTheme.dark, home: AccountCenterPage(api: api)));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Tạo tài khoản'));
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.text('Xác nhận email bằng mã'));
    await tester.tap(find.text('Xác nhận email bằng mã'));
    await tester.pumpAndSettle();

    expect(find.text('Nhập email và mã xác nhận trước khi tiếp tục.'),
        findsOneWidget);
    expect(paths, ['/api/v1/auth/me']);
    client.close();
  });

  testWidgets('account form remains usable at 320dp with enlarged text',
      (tester) async {
    tester.view.physicalSize = const Size(320, 720);
    tester.view.devicePixelRatio = 1;
    tester.platformDispatcher.textScaleFactorTestValue = 1.2;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);
    final client = MockClient((_) async => http.Response('', 401));
    final api = UserAccountApi(
      base: Uri.parse('https://nova.example/'),
      storage: MemoryAccountSecretStore(),
      client: client,
    );

    await tester.pumpWidget(
        MaterialApp(theme: NovaTheme.dark, home: AccountCenterPage(api: api)));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Tạo tài khoản'));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('load-local-confirmation')), findsOneWidget);
    expect(tester.getSize(find.byType(TextFormField).first).width,
        lessThanOrEqualTo(320));
    expect(tester.getSize(find.byType(FilledButton).last).height,
        greaterThanOrEqualTo(48));
    expect(tester.takeException(), isNull);
    client.close();
  });

  testWidgets(
      'notification inbox wraps Vietnamese copy at 320dp with larger text',
      (tester) async {
    tester.view.physicalSize = const Size(320, 720);
    tester.view.devicePixelRatio = 1;
    tester.platformDispatcher.textScaleFactorTestValue = 1.2;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);
    final client = MockClient((request) async {
      if (!request.url.path.endsWith('/notifications')) {
        throw StateError(
            'Unexpected request: ${request.method} ${request.url}');
      }
      return http.Response.bytes(
        utf8.encode(jsonEncode({
          'unreadCount': 1,
          'items': [
            {
              'id': 'n-1',
              'title': 'Lịch cộng đồng cuối tuần tại Thung lũng Sao',
              'body':
                  'Cùng tham gia hoạt động mới và xem các hướng dẫn đã cập nhật trong thư viện Nova Haven.',
              'href': '/community',
              'createdAtUtc': '2026-09-29T12:00:00Z',
              'readAtUtc': null,
            }
          ],
        })),
        200,
        headers: {'content-type': 'application/json; charset=utf-8'},
      );
    });
    final api = UserAccountApi(
      base: Uri.parse('https://nova.example/'),
      storage: MemoryAccountSecretStore(),
      client: client,
    );

    await tester.pumpWidget(MaterialApp(
        theme: NovaTheme.dark, home: AccountNotificationsPage(api: api)));
    await tester.pumpAndSettle();

    expect(find.text('1 tin chưa đọc'), findsOneWidget);
    expect(find.text('Lịch cộng đồng cuối tuần tại Thung lũng Sao'),
        findsOneWidget);
    expect(tester.takeException(), isNull);
    client.close();
  });
}
