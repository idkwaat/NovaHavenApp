import 'package:flutter/material.dart';
import 'package:flutter_markdown_plus/flutter_markdown_plus.dart';
import 'account_api.dart';
import 'account_screens.dart';
import 'bookmark_store.dart';
import 'nova_theme.dart';
import 'wiki_api.dart';

String _localizedContentKind(String value) => switch (value.toLowerCase()) {
      'npc' => 'Nhân vật',
      'quest' => 'Nhiệm vụ',
      'location' => 'Địa danh',
      'season' => 'Mùa sự kiện',
      'event' => 'Sự kiện',
      'guild' => 'Guild',
      'player' => 'Người chơi',
      'housing' => 'Nhà ở',
      'leaderboard' => 'Bảng xếp hạng',
      'weapon' => 'Vũ khí',
      'armor' => 'Giáp trụ',
      'consumable' => 'Vật phẩm tiêu hao',
      'recipe' => 'Công thức',
      'donation' => 'Ủng hộ máy chủ',
      'item' => 'Vật phẩm',
      'khoi-hanh' => 'Khởi hành',
      'lop-nhan-vat' => 'Lớp nhân vật',
      'vung-dat' => 'Vùng đất',
      'huong-dan' => 'Hướng dẫn',
      'truyen-thuyet' => 'Truyền thuyết',
      _ => value,
    };

void main() {
  const origin = String.fromEnvironment('API_BASE_URL',
      defaultValue: 'http://10.0.2.2:5080/');
  runApp(NovaCompanion(
      api: WikiApi(base: Uri.parse(origin.endsWith('/') ? origin : '$origin/')),
      accountApi: UserAccountApi(
          base: Uri.parse(origin.endsWith('/') ? origin : '$origin/')),
      bookmarks: BookmarkStore()));
}

class NovaCompanion extends StatelessWidget {
  const NovaCompanion(
      {super.key, required this.api, this.accountApi, required this.bookmarks});
  final WikiApi api;
  final UserAccountApi? accountApi;
  final BookmarkStore bookmarks;
  @override
  Widget build(BuildContext context) => MaterialApp(
        title: 'Nova Haven',
        debugShowCheckedModeBanner: false,
        theme: NovaTheme.dark,
        home: NovaHomeShell(
            api: api, accountApi: accountApi, bookmarks: bookmarks),
      );
}

class NovaHomeShell extends StatefulWidget {
  const NovaHomeShell(
      {super.key, required this.api, this.accountApi, required this.bookmarks});
  final WikiApi api;
  final UserAccountApi? accountApi;
  final BookmarkStore bookmarks;
  @override
  State<NovaHomeShell> createState() => _NovaHomeShellState();
}

class _NovaHomeShellState extends State<NovaHomeShell> {
  int selectedIndex = 0;
  late final List<Widget?> pages = [
    WikiHome(api: widget.api, bookmarks: widget.bookmarks),
    null,
    null,
    null,
  ];

  void _selectPage(int index) {
    setState(() {
      selectedIndex = index;
      pages[index] ??= switch (index) {
        1 => NewsList(api: widget.api),
        2 => CatalogList(api: widget.api),
        3 => ExploreHub(api: widget.api, accountApi: widget.accountApi),
        _ => null,
      };
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(
        index: selectedIndex,
        children: pages.map((page) => page ?? const SizedBox.shrink()).toList(),
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: selectedIndex,
        onDestinationSelected: _selectPage,
        destinations: const [
          NavigationDestination(
              icon: Icon(Icons.menu_book_outlined),
              selectedIcon: Icon(Icons.menu_book),
              label: 'Wiki'),
          NavigationDestination(
              icon: Icon(Icons.newspaper_outlined),
              selectedIcon: Icon(Icons.newspaper),
              label: 'Tin tức'),
          NavigationDestination(
              icon: Icon(Icons.inventory_2_outlined),
              selectedIcon: Icon(Icons.inventory_2),
              label: 'Vật phẩm'),
          NavigationDestination(
              icon: Icon(Icons.explore_outlined),
              selectedIcon: Icon(Icons.explore),
              label: 'Khám phá'),
        ],
      ),
    );
  }
}

class ExploreHub extends StatelessWidget {
  const ExploreHub({super.key, required this.api, this.accountApi});
  final WikiApi api;
  final UserAccountApi? accountApi;

  void _open(BuildContext context, Widget page) => Navigator.push(
        context,
        MaterialPageRoute<void>(builder: (_) => page),
      );

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Khám phá')),
        body: SafeArea(
          top: false,
          child: LayoutBuilder(builder: (context, constraints) {
            final gutter = constraints.maxWidth < 360 ? 16.0 : 20.0;
            return ListView(
              padding: EdgeInsets.fromLTRB(gutter, 8, gutter, 20),
              children: [
                Text('Đi đâu tiếp?',
                    style: Theme.of(context).textTheme.headlineSmall),
                const SizedBox(height: 4),
                Text(
                  'Tra cứu nhân vật, vùng đất và hoạt động của cộng đồng.',
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
                const SizedBox(height: 10),
                _ExploreDestination(
                  icon: Icons.public_outlined,
                  title: 'Bách khoa thế giới',
                  subtitle: 'Nhân vật, nhiệm vụ, địa danh và mùa sự kiện.',
                  onTap: () => _open(context, KnowledgeList(api: api)),
                ),
                _ExploreDestination(
                  icon: Icons.groups_outlined,
                  title: 'Cộng đồng',
                  subtitle: 'Guild, sự kiện và hoạt động của người chơi.',
                  onTap: () => _open(context, CommunityList(api: api)),
                ),
                _ExploreDestination(
                  icon: Icons.card_giftcard_outlined,
                  title: 'Thông tin phần thưởng',
                  subtitle:
                      'Danh mục tham khảo; ứng dụng không tự phát thưởng trong game.',
                  onTap: () => _open(context, RewardList(api: api)),
                ),
                _ExploreDestination(
                  icon: Icons.storefront_outlined,
                  title: 'Hỗ trợ máy chủ',
                  subtitle:
                      'Các gói đã công bố; chưa hỗ trợ thanh toán trong ứng dụng.',
                  onTap: () => _open(context, CommerceList(api: api)),
                ),
                _ExploreDestination(
                  icon: Icons.person_outline,
                  title: 'Tài khoản người chơi',
                  subtitle: 'Đăng nhập, tạo tài khoản và theo dõi thông báo.',
                  onTap: () => _open(
                      context,
                      accountApi == null
                          ? const AccountUnavailablePage()
                          : AccountCenterPage(api: accountApi!)),
                ),
                _ExploreDestination(
                  icon: Icons.notifications_none,
                  title: 'Thông báo',
                  subtitle: 'Đọc cập nhật gửi tới tài khoản Nova Haven.',
                  onTap: () => _open(
                      context,
                      accountApi == null
                          ? const AccountUnavailablePage()
                          : AccountNotificationsPage(api: accountApi!)),
                ),
              ],
            );
          }),
        ),
      );
}

class _ExploreDestination extends StatelessWidget {
  const _ExploreDestination(
      {required this.icon,
      required this.title,
      required this.subtitle,
      required this.onTap});
  final IconData icon;
  final String title, subtitle;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Card(
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(12),
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Row(children: [
              Container(
                width: 44,
                height: 44,
                decoration: BoxDecoration(
                    color: NovaPalette.paper,
                    borderRadius: BorderRadius.circular(10)),
                child: Icon(icon, color: NovaPalette.olive),
              ),
              const SizedBox(width: 14),
              Expanded(
                  child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                    Text(
                      title,
                      style: Theme.of(context)
                          .textTheme
                          .titleLarge
                          ?.copyWith(fontSize: 17, height: 1.25),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      subtitle,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: Theme.of(context)
                          .textTheme
                          .bodyMedium
                          ?.copyWith(fontSize: 14, height: 1.4),
                    ),
                  ])),
              const SizedBox(width: 8),
              const Icon(Icons.chevron_right, color: NovaPalette.harvest),
            ]),
          ),
        ),
      );
}

class WikiHome extends StatefulWidget {
  const WikiHome({super.key, required this.api, required this.bookmarks});
  final WikiApi api;
  final BookmarkStore bookmarks;
  @override
  State<WikiHome> createState() => _WikiHomeState();
}

class _WikiHomeState extends State<WikiHome> {
  final _search = TextEditingController();
  List<WikiCategory> categories = [];
  List<WikiTag> tags = [];
  WikiPage? page;
  String selected = '';
  String selectedTag = '';
  Set<String> bookmarks = <String>{};
  bool onlyBookmarks = false;
  bool loading = true;
  String? error;
  int get activeFilterCount =>
      (selected.isNotEmpty ? 1 : 0) + (selectedTag.isNotEmpty ? 1 : 0);
  @override
  void initState() {
    super.initState();
    _loadBookmarks();
    _refresh();
  }

  Future<void> _loadBookmarks() async {
    final saved = await widget.bookmarks.read();
    if (mounted) setState(() => bookmarks = saved);
  }

  Future<void> _refresh() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final catalogs = await Future.wait<dynamic>(
          [widget.api.categories(), widget.api.tags()]);
      if (!mounted) return;
      final freshCategories = (catalogs[0] as List<WikiCategory>)
          .where((category) => category.articleCount > 0)
          .toList(growable: false);
      final freshTags = catalogs[1] as List<WikiTag>;
      final category =
          freshCategories.any((c) => c.slug == selected) ? selected : '';
      final tag =
          freshTags.any((t) => t.slug == selectedTag) ? selectedTag : '';
      final articles = await widget.api
          .articles(q: _search.text, category: category, tag: tag);
      if (!mounted) return;
      final visible = onlyBookmarks
          ? articles.items
              .where((item) => bookmarks.contains(item.slug))
              .toList()
          : articles.items;
      setState(() {
        categories = freshCategories;
        tags = freshTags;
        selected = category;
        selectedTag = tag;
        page = WikiPage(
            items: visible, page: articles.page, total: visible.length);
        loading = false;
      });
    } catch (_) {
      if (mounted) {
        setState(() {
          loading = false;
          error = 'Không tải được Wiki. Kiểm tra kết nối rồi thử lại.';
        });
      }
    }
  }

  Future<void> _nextPage() async {
    if (page == null) return;
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final next = await widget.api.articles(
          q: _search.text,
          category: selected,
          tag: selectedTag,
          page: page!.page + 1);
      if (mounted) {
        setState(() {
          page = next;
          loading = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          loading = false;
          error = 'Không tải được trang tiếp theo.';
        });
      }
    }
  }

  Future<void> _showFilters() async {
    final selection = await showModalBottomSheet<_WikiFilterSelection>(
      context: context,
      isScrollControlled: true,
      backgroundColor: NovaPalette.paper,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(18)),
        side: BorderSide(color: NovaPalette.timber),
      ),
      builder: (sheetContext) {
        var draftCategory = selected;
        var draftTag = selectedTag;
        return StatefulBuilder(builder: (context, setSheetState) {
          final maxHeight = MediaQuery.sizeOf(context).height * .78;
          return SafeArea(
            child: ConstrainedBox(
              constraints: BoxConstraints(maxHeight: maxHeight),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Padding(
                    padding: const EdgeInsets.fromLTRB(16, 8, 8, 4),
                    child: Row(children: [
                      Expanded(
                        child: Text('Lọc thư viện',
                            style: Theme.of(context).textTheme.titleLarge),
                      ),
                      IconButton(
                        tooltip: 'Đóng bộ lọc',
                        onPressed: () => Navigator.pop(sheetContext),
                        icon: const Icon(Icons.close),
                      ),
                    ]),
                  ),
                  Flexible(
                    child: SingleChildScrollView(
                      padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          _FilterSection(
                            title: 'Danh mục',
                            children: [
                              FilterChip(
                                label: const Text('Tất cả'),
                                selected: draftCategory.isEmpty,
                                onSelected: (_) =>
                                    setSheetState(() => draftCategory = ''),
                              ),
                              ...categories.map((category) => FilterChip(
                                    label: Text(
                                        '${category.name} · ${category.articleCount}'),
                                    selected: draftCategory == category.slug,
                                    onSelected: (_) => setSheetState(
                                        () => draftCategory = category.slug),
                                  )),
                            ],
                          ),
                          if (tags.isNotEmpty) ...[
                            const SizedBox(height: 16),
                            _FilterSection(
                              title: 'Chủ đề',
                              children: [
                                FilterChip(
                                  label: const Text('Tất cả tag'),
                                  selected: draftTag.isEmpty,
                                  onSelected: (_) =>
                                      setSheetState(() => draftTag = ''),
                                ),
                                ...tags.map((tag) => FilterChip(
                                      label: Text(
                                          '#${tag.name} · ${tag.articleCount}'),
                                      selected: draftTag == tag.slug,
                                      onSelected: (_) => setSheetState(
                                          () => draftTag = tag.slug),
                                    )),
                              ],
                            ),
                          ],
                        ],
                      ),
                    ),
                  ),
                  const Divider(height: 1, thickness: 1),
                  Padding(
                    padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
                    child: Row(children: [
                      Expanded(
                        child: OutlinedButton.icon(
                          onPressed: () => setSheetState(() {
                            draftCategory = '';
                            draftTag = '';
                          }),
                          icon: const Icon(Icons.restart_alt),
                          label: const Text('Xóa'),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        flex: 2,
                        child: FilledButton(
                          onPressed: () => Navigator.pop(
                            sheetContext,
                            _WikiFilterSelection(draftCategory, draftTag),
                          ),
                          child: const Text('Áp dụng'),
                        ),
                      ),
                    ]),
                  ),
                ],
              ),
            ),
          );
        });
      },
    );
    if (selection == null || !mounted) return;
    setState(() {
      selected = selection.category;
      selectedTag = selection.tag;
    });
    await _refresh();
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final filterCount = activeFilterCount;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Nova Haven'),
        actions: [
          IconButton(
            tooltip: onlyBookmarks ? 'Hiện tất cả bài' : 'Chỉ hiện bài đã lưu',
            icon: Icon(
                onlyBookmarks ? Icons.bookmarks : Icons.bookmarks_outlined),
            onPressed: () {
              setState(() => onlyBookmarks = !onlyBookmarks);
              _refresh();
            },
          ),
        ],
      ),
      body: SafeArea(
        top: false,
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 820),
            child: LayoutBuilder(builder: (context, constraints) {
              final gutter = constraints.maxWidth < 360 ? 16.0 : 20.0;
              return ListView(
                padding: EdgeInsets.fromLTRB(gutter, 8, gutter, 20),
                children: [
                  Text('Thư viện Wiki',
                      style: Theme.of(context).textTheme.headlineSmall),
                  const SizedBox(height: 3),
                  Text(
                    'Nhiệm vụ, nhân vật và những vùng đất đang chờ bạn.',
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: Theme.of(context).textTheme.bodyMedium,
                  ),
                  const SizedBox(height: 12),
                  Row(children: [
                    Expanded(
                      child: TextField(
                        controller: _search,
                        textInputAction: TextInputAction.search,
                        decoration: const InputDecoration(
                          hintText: 'Tìm bài',
                          prefixIcon: Icon(Icons.search),
                        ),
                        onSubmitted: (_) => _refresh(),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Tooltip(
                      message: 'Lọc danh mục và chủ đề',
                      child: OutlinedButton.icon(
                        key: const ValueKey('wiki-filter-button'),
                        onPressed: _showFilters,
                        icon: const Icon(Icons.tune, size: 18),
                        label: Text(
                            filterCount == 0 ? 'Lọc' : 'Lọc · $filterCount'),
                      ),
                    ),
                  ]),
                  const SizedBox(height: 14),
                  if (loading)
                    const _ReaderStatus(
                      icon: Icons.auto_stories_outlined,
                      title: 'Đang tải Wiki',
                      message: 'Đang tải các bài viết đã xuất bản.',
                      progress: true,
                    )
                  else if (error != null)
                    _ReaderStatus(
                      icon: Icons.wifi_off_outlined,
                      title: 'Chưa thể tải nội dung',
                      message: error!,
                      action: FilledButton.icon(
                        onPressed: _refresh,
                        icon: const Icon(Icons.refresh),
                        label: const Text('Thử kết nối lại'),
                      ),
                    )
                  else if (page == null || page!.items.isEmpty)
                    const _ReaderStatus(
                      icon: Icons.menu_book_outlined,
                      title: 'Chưa tìm thấy bài phù hợp',
                      message:
                          'Thử đổi từ khóa hoặc chọn một danh mục khác nhé.',
                    )
                  else ...[
                    Row(children: [
                      Expanded(
                          child: Text(
                        onlyBookmarks ? 'Bài đã lưu' : 'Bài viết',
                        style:
                            Theme.of(context).textTheme.titleMedium?.copyWith(
                                  color: NovaPalette.cream,
                                  fontWeight: FontWeight.w700,
                                ),
                      )),
                      Text('${page!.total} bài',
                          style: Theme.of(context).textTheme.bodyMedium),
                    ]),
                    const SizedBox(height: 2),
                    ...page!.items.map((item) => _WikiArticleCard(
                          item: item,
                          onTap: () => Navigator.push(
                              context,
                              MaterialPageRoute<void>(
                                builder: (_) => WikiDetail(
                                  api: widget.api,
                                  slug: item.slug,
                                  bookmarks: widget.bookmarks,
                                ),
                              )).then((_) {
                            _loadBookmarks();
                            if (onlyBookmarks) _refresh();
                          }),
                        )),
                    if (page!.page * 20 < page!.total)
                      Padding(
                        padding: const EdgeInsets.only(top: 12),
                        child: OutlinedButton.icon(
                          onPressed: _nextPage,
                          icon: const Icon(Icons.expand_more),
                          label: const Text('Xem thêm bài viết'),
                        ),
                      ),
                  ],
                ],
              );
            }),
          ),
        ),
      ),
    );
  }
}

class _WikiFilterSelection {
  const _WikiFilterSelection(this.category, this.tag);
  final String category;
  final String tag;
}

class _FilterSection extends StatelessWidget {
  const _FilterSection({required this.title, required this.children});
  final String title;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.only(bottom: 4),
            child: Text(title,
                style: Theme.of(context)
                    .textTheme
                    .titleMedium
                    ?.copyWith(fontWeight: FontWeight.w700)),
          ),
          Wrap(spacing: 8, runSpacing: 0, children: children),
        ],
      );
}

class _ReaderStatus extends StatelessWidget {
  const _ReaderStatus(
      {required this.icon,
      required this.title,
      required this.message,
      this.progress = false,
      this.action});
  final IconData icon;
  final String title, message;
  final bool progress;
  final Widget? action;

  @override
  Widget build(BuildContext context) => Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child:
              Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Icon(icon, color: NovaPalette.olive, size: 28),
            const SizedBox(height: 12),
            Text(title, style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 6),
            Text(message, style: Theme.of(context).textTheme.bodyMedium),
            if (progress) ...[
              const SizedBox(height: 16),
              const LinearProgressIndicator()
            ],
            if (action != null) ...[const SizedBox(height: 16), action!],
          ]),
        ),
      );
}

class _WikiArticleCard extends StatelessWidget {
  const _WikiArticleCard({required this.item, required this.onTap});
  final WikiArticleSummary item;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Card(
        margin: const EdgeInsets.only(bottom: 8),
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.fromLTRB(14, 12, 12, 12),
            child:
                Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Row(children: [
                const Icon(Icons.auto_stories_outlined,
                    size: 17, color: NovaPalette.olive),
                const SizedBox(width: 8),
                Expanded(
                    child: Text(_localizedContentKind(item.category),
                        style: Theme.of(context)
                            .textTheme
                            .labelMedium
                            ?.copyWith(color: NovaPalette.harvest))),
                const Icon(Icons.arrow_outward,
                    size: 18, color: NovaPalette.mutedCream),
              ]),
              const SizedBox(height: 6),
              Text(
                item.title,
                style: Theme.of(context)
                    .textTheme
                    .titleLarge
                    ?.copyWith(fontSize: 18, height: 1.3),
              ),
              if (item.summary.isNotEmpty) ...[
                const SizedBox(height: 4),
                Text(
                  item.summary,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
              ],
              if (item.tags.isNotEmpty) ...[
                const SizedBox(height: 6),
                Text(
                  item.tags
                      .map((tag) => '#${_localizedContentKind(tag)}')
                      .join(' · '),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: Theme.of(context)
                      .textTheme
                      .labelMedium
                      ?.copyWith(color: NovaPalette.teal),
                ),
              ],
            ]),
          ),
        ),
      );
}

class WikiDetail extends StatefulWidget {
  const WikiDetail(
      {super.key,
      required this.api,
      required this.slug,
      required this.bookmarks});
  final WikiApi api;
  final String slug;
  final BookmarkStore bookmarks;
  @override
  State<WikiDetail> createState() => _WikiDetailState();
}

class _WikiDetailState extends State<WikiDetail> {
  late Future<WikiArticleDetail> article;
  bool bookmarked = false;
  @override
  void initState() {
    super.initState();
    article = widget.api.article(widget.slug);
    _loadBookmark();
  }

  Future<void> _loadBookmark() async {
    final saved = await widget.bookmarks.read();
    if (mounted) setState(() => bookmarked = saved.contains(widget.slug));
  }

  Future<void> _toggleBookmark() async {
    final saved = await widget.bookmarks.toggle(widget.slug);
    if (mounted) {
      setState(() => bookmarked = saved.contains(widget.slug));
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text(bookmarked ? 'Đã lưu bookmark' : 'Đã bỏ bookmark')));
    }
  }

  void retry() {
    setState(() => article = widget.api.article(widget.slug));
  }

  @override
  Widget build(BuildContext context) => Scaffold(
      appBar: AppBar(title: const Text('Bài viết Wiki'), actions: [
        IconButton(
            onPressed: _toggleBookmark,
            tooltip: bookmarked ? 'Bỏ bookmark' : 'Lưu bookmark',
            icon: Icon(bookmarked ? Icons.bookmark : Icons.bookmark_border))
      ]),
      body: FutureBuilder<WikiArticleDetail>(
          future: article,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: CircularProgressIndicator());
            }
            if (snapshot.hasError || !snapshot.hasData) {
              return Center(
                  child: Column(mainAxisSize: MainAxisSize.min, children: [
                const Text('Không tìm thấy bài viết hoặc API đang lỗi.'),
                TextButton(onPressed: retry, child: const Text('Thử lại'))
              ]));
            }
            final item = snapshot.data!;
            final toc = extractMarkdownToc(item.markdown);
            return ListView(padding: const EdgeInsets.all(20), children: [
              Text(_localizedContentKind(item.category).toUpperCase(),
                  style: const TextStyle(color: NovaPalette.harvest)),
              const SizedBox(height: 8),
              Text(item.title,
                  key: const ValueKey('wiki-detail-title'),
                  style: Theme.of(context).textTheme.headlineMedium),
              const SizedBox(height: 6),
              Text(
                'Phiên bản ${item.revision}',
                style: Theme.of(context).textTheme.labelLarge?.copyWith(
                      color: NovaPalette.mutedCream,
                    ),
              ),
              const SizedBox(height: 12),
              Text(item.summary),
              if (item.tags.isNotEmpty)
                Padding(
                    padding: const EdgeInsets.symmetric(vertical: 12),
                    child: Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children: item.tags
                            .map((tag) => Chip(
                                label: Text('#${_localizedContentKind(tag)}')))
                            .toList())),
              if (toc.isNotEmpty)
                Card(
                    child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text('Mục lục',
                                  style:
                                      TextStyle(fontWeight: FontWeight.bold)),
                              ...toc.map((heading) => Padding(
                                  padding: EdgeInsets.only(
                                      left: (heading.level - 1) * 12.0, top: 4),
                                  child: Text(heading.text))),
                            ]))),
              const Divider(height: 35),
              MarkdownBody(
                  data: item.markdown,
                  selectable: true,
                  styleSheet: MarkdownStyleSheet.fromTheme(Theme.of(context)),
                  onTapLink: (_, __, ___) {}),
              if (item.related.isNotEmpty) ...[
                const Divider(height: 35),
                const Text('Bài viết liên quan',
                    style:
                        TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                ...item.related.map((related) => ListTile(
                    title: Text(related.title),
                    subtitle: Text(related.summary),
                    onTap: () => Navigator.push(
                        context,
                        MaterialPageRoute(
                            builder: (_) => WikiDetail(
                                api: widget.api,
                                slug: related.slug,
                                bookmarks: widget.bookmarks))))),
              ],
            ]);
          }));
}

class CatalogList extends StatefulWidget {
  const CatalogList({super.key, required this.api});
  final WikiApi api;
  @override
  State<CatalogList> createState() => _CatalogListState();
}

class _CatalogListState extends State<CatalogList> {
  late Future<CatalogPage> catalog;
  @override
  void initState() {
    super.initState();
    catalog = widget.api.catalog();
  }

  void retry() => setState(() => catalog = widget.api.catalog());
  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Vật phẩm'), actions: [
          IconButton(
              tooltip: 'Công thức chế tạo',
              icon: const Icon(Icons.menu_book_outlined),
              onPressed: () => Navigator.push(
                  context,
                  MaterialPageRoute(
                      builder: (_) => RecipeList(api: widget.api))))
        ]),
        body: FutureBuilder<CatalogPage>(
            future: catalog,
            builder: (context, snapshot) {
              if (snapshot.connectionState != ConnectionState.done) {
                return const Center(child: CircularProgressIndicator());
              }
              if (snapshot.hasError) {
                return Center(
                    child: TextButton(
                        onPressed: retry,
                        child:
                            const Text('Không tải được vật phẩm · Thử lại')));
              }
              final items = snapshot.data?.items ?? <CatalogSummary>[];
              if (items.isEmpty) {
                return const Center(
                    child: Text('Chưa có vật phẩm đã xuất bản.'));
              }
              return ListView(
                  padding: const EdgeInsets.all(16),
                  children: items
                      .map((item) => Card(
                            child: ListTile(
                                title: Text(item.name),
                                subtitle: Text(item.summary.isEmpty
                                    ? _localizedContentKind(item.kind)
                                    : '${_localizedContentKind(item.kind)} · ${item.summary}'),
                                trailing: const Icon(Icons.chevron_right),
                                onTap: () => Navigator.push(
                                    context,
                                    MaterialPageRoute(
                                        builder: (_) => CatalogDetailPage(
                                            api: widget.api,
                                            slug: item.slug)))),
                          ))
                      .toList());
            }),
      );
}

class CatalogDetailPage extends StatefulWidget {
  const CatalogDetailPage({super.key, required this.api, required this.slug});
  final WikiApi api;
  final String slug;
  @override
  State<CatalogDetailPage> createState() => _CatalogDetailPageState();
}

class _CatalogDetailPageState extends State<CatalogDetailPage> {
  late Future<CatalogDetail> item;
  @override
  void initState() {
    super.initState();
    item = widget.api.catalogItem(widget.slug);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Chi tiết vật phẩm')),
        body: FutureBuilder<CatalogDetail>(
            future: item,
            builder: (context, snapshot) {
              if (snapshot.connectionState != ConnectionState.done) {
                return const Center(child: CircularProgressIndicator());
              }
              if (snapshot.hasError || !snapshot.hasData) {
                return const Center(child: Text('Không tìm thấy vật phẩm.'));
              }
              final value = snapshot.data!;
              return ListView(padding: const EdgeInsets.all(20), children: [
                Text(_localizedContentKind(value.kind).toUpperCase(),
                    style: const TextStyle(color: NovaPalette.harvest)),
                const SizedBox(height: 8),
                Text(value.name,
                    style: Theme.of(context).textTheme.headlineMedium),
                const SizedBox(height: 12),
                Text(value.summary),
                const Divider(height: 35),
                MarkdownBody(
                    data: value.markdown,
                    selectable: true,
                    styleSheet: MarkdownStyleSheet.fromTheme(Theme.of(context)),
                    onTapLink: (_, __, ___) {}),
              ]);
            }),
      );
}

class RecipeList extends StatefulWidget {
  const RecipeList({super.key, required this.api});
  final WikiApi api;
  @override
  State<RecipeList> createState() => _RecipeListState();
}

class _RecipeListState extends State<RecipeList> {
  late Future<List<RecipeSummary>> recipes;
  @override
  void initState() {
    super.initState();
    recipes = widget.api.recipes();
  }

  void retry() => setState(() => recipes = widget.api.recipes());
  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Công thức chế tạo')),
        body: FutureBuilder<List<RecipeSummary>>(
            future: recipes,
            builder: (context, snapshot) {
              if (snapshot.connectionState != ConnectionState.done) {
                return const Center(child: CircularProgressIndicator());
              }
              if (snapshot.hasError) {
                return Center(
                    child: TextButton(
                        onPressed: retry,
                        child:
                            const Text('Không tải được công thức · Thử lại')));
              }
              final items = snapshot.data ?? <RecipeSummary>[];
              if (items.isEmpty) {
                return const Center(
                    child: Text('Chưa có công thức đã xuất bản.'));
              }
              return ListView(
                  padding: const EdgeInsets.all(16),
                  children: items
                      .map((item) => Card(
                            child: ListTile(
                                title: Text(item.name),
                                subtitle: Text(item.summary),
                                trailing: const Icon(Icons.chevron_right),
                                onTap: () => Navigator.push(
                                    context,
                                    MaterialPageRoute(
                                        builder: (_) => RecipeDetailPage(
                                            api: widget.api,
                                            slug: item.slug)))),
                          ))
                      .toList());
            }),
      );
}

class RecipeDetailPage extends StatefulWidget {
  const RecipeDetailPage({super.key, required this.api, required this.slug});
  final WikiApi api;
  final String slug;
  @override
  State<RecipeDetailPage> createState() => _RecipeDetailPageState();
}

class _RecipeDetailPageState extends State<RecipeDetailPage> {
  late Future<RecipeDetail> recipe;
  @override
  void initState() {
    super.initState();
    recipe = widget.api.recipe(widget.slug);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Chi tiết công thức')),
        body: FutureBuilder<RecipeDetail>(
            future: recipe,
            builder: (context, snapshot) {
              if (snapshot.connectionState != ConnectionState.done) {
                return const Center(child: CircularProgressIndicator());
              }
              if (snapshot.hasError || !snapshot.hasData) {
                return const Center(child: Text('Không tìm thấy công thức.'));
              }
              final value = snapshot.data!;
              Widget components(String title, List<RecipeComponent> values) =>
                  Card(
                      child: Padding(
                          padding: const EdgeInsets.all(12),
                          child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(title,
                                    style: const TextStyle(
                                        fontWeight: FontWeight.bold)),
                                ...values.map((item) =>
                                    Text('${item.quantity} × ${item.itemName}'))
                              ])));
              return ListView(padding: const EdgeInsets.all(20), children: [
                Text(value.name,
                    style: Theme.of(context).textTheme.headlineMedium),
                const SizedBox(height: 12),
                Text(value.summary),
                const SizedBox(height: 12),
                components('Nguyên liệu', value.ingredients),
                components('Kết quả', value.outputs),
                const Divider(height: 35),
                MarkdownBody(
                    data: value.markdown,
                    selectable: true,
                    styleSheet: MarkdownStyleSheet.fromTheme(Theme.of(context)),
                    onTapLink: (_, __, ___) {}),
              ]);
            }),
      );
}

class NewsList extends StatefulWidget {
  const NewsList({super.key, required this.api});
  final WikiApi api;
  @override
  State<NewsList> createState() => _NewsListState();
}

class _NewsListState extends State<NewsList> {
  late Future<List<NewsSummary>> news;
  @override
  void initState() {
    super.initState();
    news = widget.api.news();
  }

  void retry() => setState(() => news = widget.api.news());
  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Tin tức & nhật ký')),
        body: FutureBuilder<List<NewsSummary>>(
            future: news,
            builder: (context, snapshot) {
              if (snapshot.connectionState != ConnectionState.done) {
                return const Center(child: CircularProgressIndicator());
              }
              if (snapshot.hasError) {
                return Center(
                    child: TextButton(
                        onPressed: retry,
                        child: const Text('Không tải được tin tức · Thử lại')));
              }
              final items = snapshot.data ?? <NewsSummary>[];
              if (items.isEmpty) {
                return const Center(child: Text('Chưa có tin đã xuất bản.'));
              }
              return ListView(
                  padding: const EdgeInsets.all(16),
                  children: items
                      .map((item) => Card(
                            child: ListTile(
                                title: Text(item.title),
                                subtitle: Text(item.summary),
                                trailing: const Icon(Icons.chevron_right),
                                onTap: () => Navigator.push(
                                    context,
                                    MaterialPageRoute(
                                        builder: (_) => NewsDetailPage(
                                            api: widget.api,
                                            slug: item.slug)))),
                          ))
                      .toList());
            }),
      );
}

class NewsDetailPage extends StatefulWidget {
  const NewsDetailPage({super.key, required this.api, required this.slug});
  final WikiApi api;
  final String slug;
  @override
  State<NewsDetailPage> createState() => _NewsDetailPageState();
}

class KnowledgeList extends StatefulWidget {
  const KnowledgeList({super.key, required this.api});
  final WikiApi api;
  @override
  State<KnowledgeList> createState() => _KnowledgeListState();
}

class _KnowledgeListState extends State<KnowledgeList> {
  String selected = '';
  late Future<KnowledgePage> page;
  @override
  void initState() {
    super.initState();
    page = widget.api.knowledge();
  }

  void reload() => setState(() => page = widget.api.knowledge(kind: selected));
  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Bách khoa thế giới')),
        body: FutureBuilder<KnowledgePage>(
            future: page,
            builder: (context, snapshot) {
              if (snapshot.connectionState != ConnectionState.done) {
                return const Center(child: CircularProgressIndicator());
              }
              if (snapshot.hasError) {
                return Center(
                    child: TextButton(
                        onPressed: reload,
                        child:
                            const Text('Không tải được bách khoa · Thử lại')));
              }
              final items = snapshot.data?.items ?? <KnowledgeSummary>[];
              return ListView(padding: const EdgeInsets.all(16), children: [
                DropdownButtonFormField<String>(
                  initialValue: selected,
                  decoration: const InputDecoration(labelText: 'Nhóm nội dung'),
                  items: const [
                    DropdownMenuItem(value: '', child: Text('Tất cả')),
                    DropdownMenuItem(value: 'npc', child: Text('Nhân vật')),
                    DropdownMenuItem(value: 'quest', child: Text('Nhiệm vụ')),
                    DropdownMenuItem(
                        value: 'location', child: Text('Địa danh')),
                    DropdownMenuItem(
                        value: 'season', child: Text('Mùa sự kiện')),
                  ],
                  onChanged: (value) {
                    selected = value ?? '';
                    reload();
                  },
                ),
                const SizedBox(height: 16),
                if (items.isEmpty)
                  const Padding(
                      padding: EdgeInsets.all(20),
                      child: Text('Chưa có nội dung đã xuất bản.')),
                ...items.map((item) => Card(
                        child: ListTile(
                      title: Text(item.name),
                      subtitle: Text('${item.kind} · ${item.summary}'),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () => Navigator.push(
                          context,
                          MaterialPageRoute(
                              builder: (_) => KnowledgeDetailPage(
                                  api: widget.api,
                                  kind: item.kind,
                                  slug: item.slug))),
                    ))),
              ]);
            }),
      );
}

class KnowledgeDetailPage extends StatefulWidget {
  const KnowledgeDetailPage(
      {super.key, required this.api, required this.kind, required this.slug});
  final WikiApi api;
  final String kind;
  final String slug;
  @override
  State<KnowledgeDetailPage> createState() => _KnowledgeDetailPageState();
}

class CommunityList extends StatefulWidget {
  const CommunityList({super.key, required this.api});
  final WikiApi api;
  @override
  State<CommunityList> createState() => _CommunityListState();
}

class _CommunityListState extends State<CommunityList> {
  String selected = '';
  late Future<CommunityPage> page;
  @override
  void initState() {
    super.initState();
    page = widget.api.community();
  }

  void reload() => setState(() => page = widget.api.community(kind: selected));
  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Cộng đồng')),
        body: FutureBuilder<CommunityPage>(
            future: page,
            builder: (context, snapshot) {
              if (snapshot.connectionState != ConnectionState.done) {
                return const Center(child: CircularProgressIndicator());
              }
              if (snapshot.hasError) {
                return Center(
                    child: TextButton(
                        onPressed: reload,
                        child:
                            const Text('Không tải được Community · Thử lại')));
              }
              final items = snapshot.data?.items ?? <CommunitySummary>[];
              return ListView(padding: const EdgeInsets.all(16), children: [
                DropdownButtonFormField<String>(
                    initialValue: selected,
                    decoration:
                        const InputDecoration(labelText: 'Nhóm cộng đồng'),
                    items: const [
                      DropdownMenuItem(value: '', child: Text('Tất cả')),
                      DropdownMenuItem(value: 'event', child: Text('Sự kiện')),
                      DropdownMenuItem(value: 'guild', child: Text('Guild')),
                      DropdownMenuItem(
                          value: 'player', child: Text('Người chơi')),
                      DropdownMenuItem(value: 'housing', child: Text('Nhà ở')),
                      DropdownMenuItem(
                          value: 'leaderboard', child: Text('Bảng xếp hạng')),
                    ],
                    onChanged: (value) {
                      selected = value ?? '';
                      reload();
                    }),
                const SizedBox(height: 16),
                if (items.isEmpty)
                  const Padding(
                      padding: EdgeInsets.all(20),
                      child: Text('Chưa có nội dung cộng đồng đã xuất bản.')),
                ...items.map((item) => Card(
                      child: ListTile(
                        title: Text(item.name),
                        subtitle: Text('${item.kind} · ${item.summary}'),
                        trailing: const Icon(Icons.chevron_right),
                        onTap: () => Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (_) => CommunityDetailPage(
                                  api: widget.api,
                                  kind: item.kind,
                                  slug: item.slug),
                            )),
                      ),
                    )),
              ]);
            }),
      );
}

class CommunityDetailPage extends StatefulWidget {
  const CommunityDetailPage(
      {super.key, required this.api, required this.kind, required this.slug});
  final WikiApi api;
  final String kind;
  final String slug;
  @override
  State<CommunityDetailPage> createState() => _CommunityDetailPageState();
}

class _CommunityDetailPageState extends State<CommunityDetailPage> {
  late Future<CommunityDetail> detail;
  @override
  void initState() {
    super.initState();
    detail = widget.api.communityDetail(widget.kind, widget.slug);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
      appBar: AppBar(title: const Text('Chi tiết cộng đồng')),
      body: FutureBuilder<CommunityDetail>(
          future: detail,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: CircularProgressIndicator());
            }
            if (snapshot.hasError || !snapshot.hasData) {
              return const Center(
                  child: Text('Không tìm thấy nội dung cộng đồng.'));
            }
            final item = snapshot.data!;
            final metadata = item.metadata;
            return ListView(padding: const EdgeInsets.all(20), children: [
              Text(_localizedContentKind(item.kind).toUpperCase(),
                  style: const TextStyle(color: NovaPalette.harvest)),
              const SizedBox(height: 8),
              Text(item.name,
                  style: Theme.of(context).textTheme.headlineMedium),
              const SizedBox(height: 12),
              Text(item.summary),
              if (item.kind == 'event')
                Text(
                    '${metadata.startsAt?.toLocal().toString().split(' ').first ?? ''} – ${metadata.endsAt?.toLocal().toString().split(' ').first ?? ''} · ${metadata.capacity ?? '-'} chỗ'),
              if (item.kind == 'guild' && metadata.motto != null)
                Text(metadata.motto!),
              if (item.kind == 'player' && metadata.handle != null)
                Text('Handle: ${metadata.handle}'),
              if (item.kind == 'housing' && metadata.ownerDisplayName != null)
                Text('Chủ công trình: ${metadata.ownerDisplayName}'),
              if (item.kind == 'leaderboard') ...[
                Text('Hạng mục: ${metadata.leaderboardCategory ?? ''}'),
                ...metadata.rows.map((row) => ListTile(
                    title: Text(row.participantName),
                    subtitle: Text('${row.score} điểm · ${row.note}'),
                    leading: Text('${row.rank}')))
              ],
              const Divider(height: 35),
              MarkdownBody(
                  data: item.markdown,
                  selectable: true,
                  styleSheet: MarkdownStyleSheet.fromTheme(Theme.of(context)),
                  onTapLink: (_, __, ___) {}),
              if (item.kind == 'housing' &&
                  metadata.galleryMarkdown != null) ...[
                const Divider(height: 35),
                MarkdownBody(
                    data: metadata.galleryMarkdown!,
                    selectable: true,
                    styleSheet: MarkdownStyleSheet.fromTheme(Theme.of(context)),
                    onTapLink: (_, __, ___) {})
              ],
            ]);
          }));
}

class _KnowledgeDetailPageState extends State<KnowledgeDetailPage> {
  late Future<KnowledgeDetail> detail;
  @override
  void initState() {
    super.initState();
    detail = widget.api.knowledgeDetail(widget.kind, widget.slug);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Bách khoa thế giới')),
        body: FutureBuilder<KnowledgeDetail>(
            future: detail,
            builder: (context, snapshot) {
              if (snapshot.connectionState != ConnectionState.done) {
                return const Center(child: CircularProgressIndicator());
              }
              if (snapshot.hasError || !snapshot.hasData) {
                return const Center(child: Text('Không tìm thấy nội dung.'));
              }
              final item = snapshot.data!;
              final metadata = item.metadata;
              return ListView(padding: const EdgeInsets.all(20), children: [
                Text(_localizedContentKind(item.kind).toUpperCase(),
                    style: const TextStyle(color: NovaPalette.harvest)),
                const SizedBox(height: 8),
                Text(item.name,
                    style: Theme.of(context).textTheme.headlineMedium),
                const SizedBox(height: 12),
                Text(item.summary),
                if (item.kind == 'npc' && metadata.role != null)
                  Text('Vai trò: ${metadata.role}'),
                if (item.kind == 'quest') ...[
                  Text('Độ khó: ${metadata.difficulty ?? '-'} / 5'),
                  if (metadata.rewardDescription != null)
                    Text('Phần thưởng mô tả: ${metadata.rewardDescription}'),
                  ...metadata.steps.map((step) => ListTile(
                      title: Text(step.title),
                      subtitle: Text(step.description),
                      leading: Text('${step.position}'))),
                ],
                if (item.kind == 'location')
                  Text(
                      '${metadata.region ?? ''} · ${metadata.locationType ?? ''}'),
                if (item.kind == 'season')
                  Text(
                      '${metadata.theme ?? ''} · ${metadata.startsAt?.toLocal().toString().split(' ').first ?? ''} – ${metadata.endsAt?.toLocal().toString().split(' ').first ?? ''}'),
                const Divider(height: 35),
                MarkdownBody(
                    data: item.markdown,
                    selectable: true,
                    styleSheet: MarkdownStyleSheet.fromTheme(Theme.of(context)),
                    onTapLink: (_, __, ___) {}),
                if (item.links.isNotEmpty) ...[
                  const Divider(height: 35),
                  const Text('Liên kết trong đồ thị',
                      style:
                          TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                  ...item.links.map((link) => ListTile(
                      title: Text(link.name),
                      subtitle: Text(_localizedContentKind(link.linkType)),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () {
                        if (link.type == 'catalog') {
                          Navigator.push(
                              context,
                              MaterialPageRoute(
                                  builder: (_) => CatalogDetailPage(
                                      api: widget.api, slug: link.slug)));
                        } else {
                          Navigator.push(
                              context,
                              MaterialPageRoute(
                                  builder: (_) => KnowledgeDetailPage(
                                      api: widget.api,
                                      kind: link.type,
                                      slug: link.slug)));
                        }
                      })),
                ],
              ]);
            }),
      );
}

class _NewsDetailPageState extends State<NewsDetailPage> {
  late Future<NewsDetail> news;
  @override
  void initState() {
    super.initState();
    news = widget.api.newsArticle(widget.slug);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Tin tức Nova Haven')),
        body: FutureBuilder<NewsDetail>(
            future: news,
            builder: (context, snapshot) {
              if (snapshot.connectionState != ConnectionState.done) {
                return const Center(child: CircularProgressIndicator());
              }
              if (snapshot.hasError || !snapshot.hasData) {
                return const Center(child: Text('Không tìm thấy tin.'));
              }
              final item = snapshot.data!;
              return ListView(padding: const EdgeInsets.all(20), children: [
                Text(item.title,
                    style: Theme.of(context).textTheme.headlineMedium),
                const SizedBox(height: 8),
                Text(item.summary),
                const Divider(height: 35),
                MarkdownBody(
                    data: item.markdown,
                    selectable: true,
                    styleSheet: MarkdownStyleSheet.fromTheme(Theme.of(context)),
                    onTapLink: (_, __, ___) {}),
              ]);
            }),
      );
}

class RewardList extends StatefulWidget {
  const RewardList({super.key, required this.api});
  final WikiApi api;
  @override
  State<RewardList> createState() => _RewardListState();
}

class _RewardListState extends State<RewardList> {
  late Future<RewardPage> page;
  @override
  void initState() {
    super.initState();
    page = widget.api.rewards();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Phần thưởng')),
        body: FutureBuilder<RewardPage>(
            future: page,
            builder: (context, snapshot) {
              if (snapshot.connectionState != ConnectionState.done) {
                return const Center(child: CircularProgressIndicator());
              }
              if (snapshot.hasError) {
                return const Center(
                    child: Text('Chưa kết nối được dữ liệu phần thưởng.'));
              }
              final items = snapshot.data?.items ?? <RewardSummary>[];
              if (items.isEmpty) {
                return const Center(child: Text('Chưa có reward definition.'));
              }
              return ListView(
                  padding: const EdgeInsets.all(16),
                  children: items
                      .map((item) => Card(
                            child: ListTile(
                                title: Text(item.name),
                                subtitle: Text(
                                    '${_localizedContentKind(item.kind)} · ${item.summary}'),
                                trailing: const Icon(Icons.chevron_right),
                                onTap: () => Navigator.push(
                                    context,
                                    MaterialPageRoute(
                                        builder: (_) => RewardDetailPage(
                                            api: widget.api,
                                            slug: item.slug)))),
                          ))
                      .toList());
            }),
      );
}

class RewardDetailPage extends StatefulWidget {
  const RewardDetailPage({super.key, required this.api, required this.slug});
  final WikiApi api;
  final String slug;
  @override
  State<RewardDetailPage> createState() => _RewardDetailPageState();
}

class _RewardDetailPageState extends State<RewardDetailPage> {
  late Future<RewardDetail> detail;
  @override
  void initState() {
    super.initState();
    detail = widget.api.reward(widget.slug);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
      appBar: AppBar(title: const Text('Chi tiết phần thưởng')),
      body: FutureBuilder<RewardDetail>(
          future: detail,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: CircularProgressIndicator());
            }
            if (snapshot.hasError || !snapshot.hasData) {
              return const Center(child: Text('Không tìm thấy reward.'));
            }
            final item = snapshot.data!;
            return ListView(padding: const EdgeInsets.all(20), children: [
              Text(item.name,
                  style: Theme.of(context).textTheme.headlineMedium),
              Text(item.summary),
              const SizedBox(height: 12),
              const Card(
                  child: Padding(
                      padding: EdgeInsets.all(12),
                      child: Text(
                          'Việc trao thưởng được xác nhận bên ngoài ứng dụng. Ứng dụng không tự phát thưởng trong game.'))),
              MarkdownBody(
                  data: item.markdown,
                  selectable: true,
                  styleSheet: MarkdownStyleSheet.fromTheme(Theme.of(context)),
                  onTapLink: (_, __, ___) {})
            ]);
          }));
}

class CommerceList extends StatefulWidget {
  const CommerceList({super.key, required this.api});
  final WikiApi api;
  @override
  State<CommerceList> createState() => _CommerceListState();
}

class _CommerceListState extends State<CommerceList> {
  late Future<CommercePage> page;
  @override
  void initState() {
    super.initState();
    page = widget.api.commerce();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
      appBar: AppBar(title: const Text('Hỗ trợ máy chủ')),
      body: FutureBuilder<CommercePage>(
          future: page,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: CircularProgressIndicator());
            }
            if (snapshot.hasError) {
              return const Center(
                  child: Text('Chưa kết nối được thông tin hỗ trợ.'));
            }
            final items = snapshot.data?.items ?? <CommerceSummary>[];
            if (items.isEmpty) {
              return const Center(child: Text('Chưa có thông tin hỗ trợ.'));
            }
            return ListView(padding: const EdgeInsets.all(16), children: [
              const CommerceDemoNotice(),
              ...items.map((item) => Card(
                    child: ListTile(
                        title: Text(item.name),
                        subtitle: Text(
                            '${_localizedContentKind(item.kind)} · ${item.summary}'),
                        trailing: const Icon(Icons.chevron_right),
                        onTap: () => Navigator.push(
                            context,
                            MaterialPageRoute(
                                builder: (_) => CommerceDetailPage(
                                    api: widget.api, slug: item.slug)))),
                  )),
            ]);
          }));
}

class CommerceDetailPage extends StatefulWidget {
  const CommerceDetailPage({super.key, required this.api, required this.slug});
  final WikiApi api;
  final String slug;
  @override
  State<CommerceDetailPage> createState() => _CommerceDetailPageState();
}

class _CommerceDetailPageState extends State<CommerceDetailPage> {
  late Future<CommerceDetail> detail;
  @override
  void initState() {
    super.initState();
    detail = widget.api.commerceOffer(widget.slug);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
      appBar: AppBar(title: const Text('Chi tiết hỗ trợ')),
      body: FutureBuilder<CommerceDetail>(
          future: detail,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: CircularProgressIndicator());
            }
            if (snapshot.hasError || !snapshot.hasData) {
              return const Center(child: Text('Không tìm thấy offer.'));
            }
            final item = snapshot.data!;
            return ListView(padding: const EdgeInsets.all(20), children: [
              const CommerceDemoNotice(),
              Text(item.name,
                  style: Theme.of(context).textTheme.headlineMedium),
              Text(item.summary),
              Text('Giá hiển thị: ${item.displayPrice}'),
              const SizedBox(height: 12),
              MarkdownBody(
                  data: item.markdown,
                  selectable: true,
                  styleSheet: MarkdownStyleSheet.fromTheme(Theme.of(context)),
                  onTapLink: (_, __, ___) {})
            ]);
          }));
}

class CommerceDemoNotice extends StatelessWidget {
  const CommerceDemoNotice({super.key});

  @override
  Widget build(BuildContext context) => const Card(
        child: Padding(
          padding: EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('Mô phỏng local',
                  style: TextStyle(fontWeight: FontWeight.w700)),
              SizedBox(height: 6),
              Text(
                  'Không thu tiền thật và không giao vật phẩm hoặc quyền lợi trong game.'),
            ],
          ),
        ),
      );
}
