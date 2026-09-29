import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:nova_haven_companion/account_api.dart';

class MemorySecretStore implements AccountSecretStore {
  final values = <String, String>{};
  @override
  Future<String?> read(String key) async => values[key];
  @override
  Future<void> write(String key, String value) async => values[key] = value;
  @override
  Future<void> delete(String key) async => values.remove(key);
}

void main() {
  test('login carries CSRF and secure cookies across the account session',
      () async {
    final storage = MemorySecretStore();
    var calls = 0;
    final client = MockClient((request) async {
      calls++;
      if (request.url.path.endsWith('/auth/csrf')) {
        return http.Response(jsonEncode({'token': 'csrf-token'}), 200,
            headers: {
              'set-cookie':
                  '.AspNetCore.Antiforgery.test=anti-cookie; path=/; samesite=lax'
            });
      }
      if (request.url.path.endsWith('/auth/login')) {
        expect(request.method, 'POST');
        expect(request.headers['x-csrf-token'], 'csrf-token');
        expect(request.headers['cookie'],
            contains('.AspNetCore.Antiforgery.test=anti-cookie'));
        return http.Response('', 204, headers: {
          'set-cookie':
              '.AspNetCore.Identity.Application=identity-cookie; path=/; httponly'
        });
      }
      expect(request.url.path, '/api/v1/auth/me');
      expect(request.headers['cookie'],
          contains('.AspNetCore.Identity.Application=identity-cookie'));
      expect(request.headers['cookie'],
          contains('.AspNetCore.Antiforgery.test=anti-cookie'));
      return http.Response(
          jsonEncode({
            'id': '11111111-1111-1111-1111-111111111111',
            'email': 'player@example.test',
            'emailConfirmed': true,
            'isAdmin': false
          }),
          200);
    });
    final api = UserAccountApi(
        base: Uri.parse('https://nova.example/'),
        storage: storage,
        client: client);
    final player = await api.login('player@example.test', 'secret');
    expect(player.emailConfirmed, isTrue);
    expect(player.isAdmin, isFalse);
    expect(calls, 3);
    expect(storage.values, contains('nova_haven.identity.cookies.v1'));
    client.close();
  });

  test(
      'account and inbox mutations use the web API routes with fresh CSRF tokens',
      () async {
    final paths = <String>[];
    final client = MockClient((request) async {
      paths.add('${request.method} ${request.url.path}');
      if (request.url.path.endsWith('/auth/csrf')) {
        return http.Response('{"token":"csrf-token"}', 200);
      }
      if (request.url.path.endsWith('/auth/register')) {
        expect(request.method, 'POST');
        expect(request.headers['x-csrf-token'], 'csrf-token');
        expect(jsonDecode(request.body), {
          'email': 'player@example.test',
          'password': 'VeryStrongPass123!',
        });
        return http.Response('', 202);
      }
      if (request.url.path.endsWith('/auth/confirm-email')) {
        expect(request.headers['x-csrf-token'], 'csrf-token');
        expect(jsonDecode(request.body), {
          'email': 'player@example.test',
          'token': 'mail-token',
        });
        return http.Response('', 204);
      }
      if (request.url.path.endsWith('/auth/resend-confirmation')) {
        expect(request.headers['x-csrf-token'], 'csrf-token');
        return http.Response('', 202);
      }
      if (request.url.path.endsWith('/notifications/read-all')) {
        expect(request.method, 'POST');
        expect(request.headers['x-csrf-token'], 'csrf-token');
        return http.Response('', 204);
      }
      if (request.url.path.endsWith('/read') &&
          request.url.path.contains('/notifications/')) {
        expect(request.method, 'PUT');
        expect(request.headers['x-csrf-token'], 'csrf-token');
        expect(request.url.path,
            '/api/v1/notifications/11111111-1111-1111-1111-111111111111/read');
        return http.Response('', 204);
      }
      if (request.url.path.endsWith('/notifications')) {
        return http.Response('{"items":[],"unreadCount":0}', 200);
      }
      throw StateError('Unexpected request: ${request.method} ${request.url}');
    });
    final api = UserAccountApi(
      base: Uri.parse('https://nova.example/'),
      storage: MemorySecretStore(),
      client: client,
    );

    await api.register(' player@example.test ', 'VeryStrongPass123!');
    await api.confirmEmail(' player@example.test ', 'mail-token');
    await api.resendConfirmation(' player@example.test ');
    expect((await api.notifications()).unreadCount, 0);
    await api.markRead('11111111-1111-1111-1111-111111111111');
    await api.markAllRead();

    expect(
        paths
            .where((path) =>
                path.startsWith('GET ') && path.endsWith('/auth/csrf'))
            .length,
        5);
    expect(
        paths,
        contains(
            'PUT /api/v1/notifications/11111111-1111-1111-1111-111111111111/read'));
    client.close();
  });

  test('logout sends the account cookies then clears them from secure storage',
      () async {
    final storage = MemorySecretStore()
      ..values['nova_haven.identity.cookies.v1'] = jsonEncode({
        '.AspNetCore.Identity.Application': 'identity-cookie',
        '.AspNetCore.Antiforgery.test': 'anti-cookie',
      });
    final client = MockClient((request) async {
      if (request.url.path.endsWith('/auth/csrf')) {
        expect(request.headers['cookie'], contains('identity-cookie'));
        return http.Response(jsonEncode({'token': 'csrf-token'}), 200);
      }
      expect(request.url.path, '/api/v1/auth/logout');
      expect(request.headers['x-csrf-token'], 'csrf-token');
      expect(request.headers['cookie'], contains('identity-cookie'));
      return http.Response('', 204);
    });
    final api = UserAccountApi(
        base: Uri.parse('https://nova.example/'),
        storage: storage,
        client: client);

    await api.logout();

    expect(storage.values, isNot(contains('nova_haven.identity.cookies.v1')));
    client.close();
  });

  test(
      'local confirmation outbox returns a URL-decoded token from the latest message',
      () async {
    final client = MockClient((request) async {
      expect(request.url.path, '/api/v1/dev/mailbox');
      expect(request.url.queryParameters['email'], 'player+local@example.test');
      return http.Response.bytes(
          utf8.encode(jsonEncode([
            {
              'id': 'message-1',
              'recipient': 'player+local@example.test',
              'subject': 'Xác nhận tài khoản Nova Haven',
              'confirmationUrl':
                  'https://nova.example/account?email=player%2Blocal%40example.test&token=abc%2B123%3D',
              'createdAtUtc': '2026-09-29T12:00:00Z',
            }
          ])),
          200,
          headers: {'content-type': 'application/json; charset=utf-8'});
    });
    final api = UserAccountApi(
        base: Uri.parse('https://nova.example/'),
        storage: MemorySecretStore(),
        client: client);

    final message =
        await api.latestLocalConfirmation(' player+local@example.test ');

    expect(message?.token, 'abc+123=');
    expect(message?.createdAtUtc.isUtc, isTrue);
    client.close();
  });

  test(
      'local confirmation outbox returns null when no development message exists',
      () async {
    final client = MockClient((_) async => http.Response('[]', 200));
    final api = UserAccountApi(
        base: Uri.parse('https://nova.example/'),
        storage: MemorySecretStore(),
        client: client);

    expect(await api.latestLocalConfirmation('player@example.test'), isNull);
    client.close();
  });

  test(
      'local confirmation outbox explains that production has no local mailbox route',
      () async {
    final client = MockClient((_) async => http.Response('', 404));
    final api = UserAccountApi(
        base: Uri.parse('https://nova.example/'),
        storage: MemorySecretStore(),
        client: client);

    await expectLater(api.latestLocalConfirmation('player@example.test'),
        throwsA(isA<StateError>()));
    client.close();
  });

  test(
      'local confirmation outbox rejects a message without a confirmation token',
      () async {
    final client = MockClient((_) async => http.Response(
          jsonEncode([
            {
              'confirmationUrl':
                  'https://nova.example/account?email=player%40example.test',
              'createdAtUtc': '2026-09-29T12:00:00Z',
            }
          ]),
          200,
        ));
    final api = UserAccountApi(
        base: Uri.parse('https://nova.example/'),
        storage: MemorySecretStore(),
        client: client);

    await expectLater(api.latestLocalConfirmation('player@example.test'),
        throwsA(isA<FormatException>()));
    client.close();
  });

  test('inbox parses unread count and UTC timestamps', () {
    final inbox = AccountInbox.fromJson({
      'unreadCount': 1,
      'items': [
        {
          'id': 'n-1',
          'title': 'Chào mừng',
          'body': 'Xin chào',
          'href': '/wiki',
          'createdAtUtc': '2026-09-29T12:00:00Z',
          'readAtUtc': null
        }
      ],
    });
    expect(inbox.unreadCount, 1);
    expect(inbox.items.single.isRead, isFalse);
    expect(inbox.items.single.createdAtUtc.isUtc, isTrue);
  });
}
