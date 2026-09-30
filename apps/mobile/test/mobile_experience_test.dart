import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:nova_haven_companion/bookmark_store.dart';
import 'package:nova_haven_companion/main.dart';
import 'package:nova_haven_companion/wiki_api.dart';

class _MemoryBookmarkStore extends BookmarkStore {
  final _saved = <String>{};

  @override
  Future<Set<String>> read() async => Set<String>.of(_saved);

  @override
  Future<Set<String>> toggle(String slug) async {
    if (!_saved.add(slug)) _saved.remove(slug);
    return Set<String>.of(_saved);
  }
}

void main() {
  testWidgets(
      'Wiki moves taxonomy filters into a sheet to surface articles sooner',
      (tester) async {
    final api = WikiApi(
      base: Uri.parse('https://nova.example/'),
      client: MockClient((request) async => _responseFor(request)),
    );
    tester.view.physicalSize = const Size(320, 720);
    tester.view.devicePixelRatio = 1;
    tester.platformDispatcher.textScaleFactorTestValue = 1.2;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);

    await tester.pumpWidget(
      NovaCompanion(api: api, bookmarks: _MemoryBookmarkStore()),
    );
    await tester.pumpAndSettle();

    expect(find.text('Danh mục'), findsNothing);
    expect(find.text('Chủ đề'), findsNothing);
    final filterButton = find.byTooltip('Lọc danh mục và chủ đề');
    expect(filterButton, findsOneWidget);
    expect(tester.getSize(filterButton).height, greaterThanOrEqualTo(48));
    expect(tester.getTopLeft(find.text('Bản đồ Thung lũng Sao')).dy,
        lessThan(450));

    await tester.tap(find.byTooltip('Lọc danh mục và chủ đề'));
    await tester.pumpAndSettle();
    expect(find.text('Lọc thư viện'), findsOneWidget);
    expect(find.text('Chủ đề'), findsOneWidget);
    await tester.tap(find.text('Khởi hành · 1'));
    await tester.tap(find.text('#Hướng dẫn · 2'));
    await tester.tap(find.text('Áp dụng'));
    await tester.pumpAndSettle();
    expect(find.text('Lọc · 2'), findsOneWidget);
    expect(tester.takeException(), isNull);
    api.dispose();
  });

  testWidgets('Explore stays operable on a 320dp phone with larger text',
      (tester) async {
    final api = WikiApi(
      base: Uri.parse('https://nova.example/'),
      client: MockClient((request) async => _responseFor(request)),
    );
    tester.view.physicalSize = const Size(320, 720);
    tester.view.devicePixelRatio = 1;
    tester.platformDispatcher.textScaleFactorTestValue = 1.2;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);

    await tester.pumpWidget(
      NovaCompanion(api: api, bookmarks: _MemoryBookmarkStore()),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.text('Khám phá'));
    await tester.pumpAndSettle();

    expect(find.text('Đi đâu tiếp?'), findsOneWidget);
    final firstCard = find.ancestor(
      of: find.text('Bách khoa thế giới'),
      matching: find.byType(Card),
    );
    expect(tester.getSize(firstCard.first).height, lessThanOrEqualTo(168));
    expect(tester.takeException(), isNull);
    api.dispose();
  });

  testWidgets('mobile screens survive landscape with enlarged system text',
      (tester) async {
    final api = WikiApi(
      base: Uri.parse('https://nova.example/'),
      client: MockClient((request) async => _responseFor(request)),
    );
    tester.view.physicalSize = const Size(812, 375);
    tester.view.devicePixelRatio = 1;
    tester.platformDispatcher.textScaleFactorTestValue = 1.3;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);

    await tester.pumpWidget(
      NovaCompanion(api: api, bookmarks: _MemoryBookmarkStore()),
    );
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);

    await tester.tap(find.text('Khám phá'));
    await tester.pumpAndSettle();
    expect(find.text('Đi đâu tiếp?'), findsOneWidget);
    expect(tester.takeException(), isNull);
    api.dispose();
  });

  testWidgets(
      'mobile home uses the approved earth palette and readable contrast',
      (tester) async {
    final api = WikiApi(
      base: Uri.parse('https://nova.example/'),
      client: MockClient((request) async => _responseFor(request)),
    );
    await tester.pumpWidget(
      NovaCompanion(api: api, bookmarks: _MemoryBookmarkStore()),
    );
    await tester.pumpAndSettle();

    final theme = tester.widget<MaterialApp>(find.byType(MaterialApp)).theme!;
    expect(theme.scaffoldBackgroundColor, const Color(0xFF1D2422));
    expect(theme.colorScheme.surface, const Color(0xFF252D2A));
    expect(
      _contrastRatio(theme.colorScheme.onSurface, theme.colorScheme.surface),
      greaterThanOrEqualTo(4.5),
    );
    expect(tester.takeException(), isNull);
    api.dispose();
  });

  testWidgets(
      'mobile commerce clearly explains local demo has no charge or game delivery',
      (tester) async {
    final api = WikiApi(
      base: Uri.parse('https://nova.example/'),
      client: MockClient((request) async => _responseFor(request)),
    );
    await tester.pumpWidget(
      NovaCompanion(api: api, bookmarks: _MemoryBookmarkStore()),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.text('Khám phá'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Hỗ trợ máy chủ'));
    await tester.pumpAndSettle();

    expect(find.textContaining('Không thu tiền thật'), findsOneWidget);
    expect(find.textContaining('không giao vật phẩm'), findsOneWidget);
    expect(tester.takeException(), isNull);
    api.dispose();
  });

  testWidgets('published Wiki cards open readable details and save bookmarks',
      (tester) async {
    final api = WikiApi(
      base: Uri.parse('https://nova.example/'),
      client: MockClient((request) async => _responseFor(request)),
    );
    final bookmarks = _MemoryBookmarkStore();
    tester.view.physicalSize = const Size(360, 800);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      NovaCompanion(api: api, bookmarks: bookmarks),
    );
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(
      find.text('Bản đồ Thung lũng Sao'),
      250,
      scrollable: find.byType(Scrollable).first,
    );
    await tester.tap(find.text('Bản đồ Thung lũng Sao'));
    await tester.pumpAndSettle();

    expect(find.text('Bài viết Wiki'), findsOneWidget);
    expect(find.text('VÙNG ĐẤT'), findsOneWidget);
    expect(find.text('Phiên bản 4'), findsOneWidget);
    expect(find.text('#Hướng dẫn'), findsOneWidget);
    expect(find.text('Nội dung đã xuất bản.'), findsOneWidget);
    expect(
      tester
          .widget<Text>(find.byKey(const ValueKey('wiki-detail-title')))
          .style
          ?.fontSize,
      25,
    );
    await tester.tap(find.byTooltip('Lưu bookmark'));
    await tester.pumpAndSettle();
    expect(find.byTooltip('Bỏ bookmark'), findsOneWidget);
    expect(find.text('Đã lưu bookmark'), findsOneWidget);
    api.dispose();
  });

  testWidgets('primary sections are reachable through compact labeled tabs',
      (tester) async {
    final api = WikiApi(
      base: Uri.parse('https://nova.example/'),
      client: MockClient((request) async => _responseFor(request)),
    );
    tester.view.physicalSize = const Size(360, 800);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      NovaCompanion(api: api, bookmarks: _MemoryBookmarkStore()),
    );
    await tester.pumpAndSettle();

    expect(find.byType(NavigationBar), findsOneWidget);
    for (final label in ['Wiki', 'Tin tức', 'Vật phẩm', 'Khám phá']) {
      expect(find.text(label), findsOneWidget);
    }
    await tester.tap(find.byTooltip('Lọc danh mục và chủ đề'));
    await tester.pumpAndSettle();
    final renderedLabels = tester
        .widgetList<Text>(find.descendant(
          of: find.byType(FilterChip),
          matching: find.byType(Text),
        ))
        .map((text) => text.data)
        .toList();
    expect(renderedLabels, contains('Khởi hành · 1'));
    expect(find.text('Smoke 97314a47 · 0'), findsNothing);
    await tester.tap(find.byTooltip('Đóng bộ lọc'));
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(
      find.text('Bản đồ Thung lũng Sao'),
      250,
      scrollable: find.byType(Scrollable).first,
    );
    expect(find.text('Vùng đất'), findsOneWidget);
    await tester.tap(find.text('Tin tức'));
    await tester.pumpAndSettle();
    expect(find.text('Chưa có tin đã xuất bản.'), findsOneWidget);
    expect(tester.takeException(), isNull);
    api.dispose();
  });
}

http.Response _responseFor(http.Request request) {
  if (request.url.path.endsWith('/commerce/offers')) {
    return _jsonResponse({
      'items': [
        {
          'id': 'demo-support',
          'slug': 'goi-ho-tro-demo',
          'name': 'Gói hỗ trợ demo',
          'summary': 'Mục chỉ dùng để kiểm thử quy trình.',
          'kind': 'donation',
          'revision': 1,
          'definitionOnly': false,
          'updatedAt': '2026-09-29T00:00:00Z',
        },
      ],
      'page': 1,
      'total': 1,
    });
  }
  if (request.url.path.endsWith('/categories')) {
    return _jsonResponse([
      {
        'id': 'category-start',
        'slug': 'khoi-hanh',
        'name': 'Khởi hành',
        'articleCount': 1,
      },
      {
        'id': 'category-smoke',
        'slug': 'smoke-97314a47',
        'name': 'Smoke 97314a47',
        'articleCount': 0,
      },
    ]);
  }
  if (request.url.path.endsWith('/tags')) {
    return _jsonResponse([
      {
        'id': 'tag-guide',
        'slug': 'huong-dan',
        'name': 'Hướng dẫn',
        'articleCount': 2,
      },
      {
        'id': 'tag-legend',
        'slug': 'truyen-thuyet',
        'name': 'Truyền thuyết',
        'articleCount': 2,
      },
    ]);
  }
  if (request.url.path.endsWith('/articles/thung-lung-sao')) {
    return _jsonResponse({
      'slug': 'thung-lung-sao',
      'title': 'Bản đồ Thung lũng Sao',
      'summary':
          'Những địa danh đầu tiên quanh khu vực khởi hành của Nova Haven.',
      'category': 'vung-dat',
      'revision': 4,
      'tags': ['huong-dan'],
      'markdown': '# Bản đồ Thung lũng Sao\n\nNội dung đã xuất bản.',
      'related': <Object>[],
    });
  }
  if (request.url.path.endsWith('/articles')) {
    return _jsonResponse({
      'items': [
        {
          'slug': 'thung-lung-sao',
          'title': 'Bản đồ Thung lũng Sao',
          'summary': 'Những địa danh đầu tiên của Nova Haven.',
          'category': 'vung-dat',
          'revision': 4,
          'tags': <String>[],
        },
      ],
      'page': 1,
      'total': 1,
    });
  }
  if (request.url.path.endsWith('/news')) {
    return _jsonResponse({'items': <Object>[], 'page': 1, 'total': 0});
  }
  return http.Response('{}', 200);
}

http.Response _jsonResponse(Object body) => http.Response.bytes(
      utf8.encode(jsonEncode(body)),
      200,
      headers: {'content-type': 'application/json; charset=utf-8'},
    );

double _contrastRatio(Color foreground, Color background) {
  final first = foreground.computeLuminance();
  final second = background.computeLuminance();
  final lighter = first > second ? first : second;
  final darker = first > second ? second : first;
  return (lighter + 0.05) / (darker + 0.05);
}
