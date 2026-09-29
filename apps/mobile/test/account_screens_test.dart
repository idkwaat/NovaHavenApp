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
  testWidgets('account center registers and signs in without sending email',
      (tester) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    var registerRequestSeen = false;
    final client = MockClient((request) async {
      if (request.url.path.endsWith('/auth/me')) {
        return registerRequestSeen
            ? http.Response(jsonEncode({
                'id': '11111111-1111-1111-1111-111111111111',
                'email': 'player@example.test',
                'emailConfirmed': true,
                'isAdmin': false,
              }), 200)
            : http.Response('', 401);
      }
      if (request.url.path.endsWith('/auth/csrf')) {
        return http.Response('{"token":"csrf-token"}', 200);
      }
      if (request.url.path.endsWith('/auth/register')) {
        registerRequestSeen = true;
        expect(request.headers['x-csrf-token'], 'csrf-token');
        final body = jsonDecode(request.body) as Map<String, dynamic>;
        expect(body['email'], 'player@example.test');
        return http.Response(jsonEncode({
          'id': '11111111-1111-1111-1111-111111111111',
          'email': 'player@example.test',
          'emailConfirmed': true,
          'isAdmin': false,
        }), 201, headers: {
          'set-cookie':
              '.AspNetCore.Identity.Application=identity-cookie; path=/; httponly'
        });
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
    await tester.enterText(find.byType(TextFormField).last, 'VeryStrongPass123!');
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(FilledButton, 'Tạo tài khoản'));
    await tester.pumpAndSettle();

    expect(registerRequestSeen, isTrue);
    expect(find.text('player@example.test'), findsOneWidget);
    expect(find.text('Tài khoản người chơi'), findsOneWidget);
    expect(find.textContaining('đang đăng nhập vào Nova Haven'), findsOneWidget);
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

    expect(find.textContaining('Tạo xong là dùng được ngay'), findsOneWidget);
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
