import 'package:flutter/material.dart';

import 'account_api.dart';
import 'nova_theme.dart';

class AccountUnavailablePage extends StatelessWidget {
  const AccountUnavailablePage({super.key});
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Tài khoản Nova Haven')),
      body: const Center(
          child: Padding(
              padding: EdgeInsets.all(24),
              child: Text(
                  'Chức năng tài khoản cần cấu hình API_BASE_URL khi khởi chạy ứng dụng.',
                  textAlign: TextAlign.center))),
    );
  }
}

class AccountCenterPage extends StatefulWidget {
  const AccountCenterPage({super.key, required this.api});
  final UserAccountApi api;
  @override
  State<AccountCenterPage> createState() => _AccountCenterPageState();
}

class _AccountCenterPageState extends State<AccountCenterPage> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _password = TextEditingController();
  bool _register = false;
  bool _busy = false;
  bool _checkingUser = true;
  String? _message;
  String? _error;
  AccountUser? _user;

  @override
  void initState() {
    super.initState();
    _loadUser();
  }

  Future<void> _loadUser() async {
    try {
      final user = await widget.api.currentUser();
      if (mounted) setState(() => _user = user);
    } catch (error) {
      if (mounted) {
        setState(
            () => _error = error.toString().replaceFirst('Bad state: ', ''));
      }
    } finally {
      if (mounted) setState(() => _checkingUser = false);
    }
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _busy = true;
      _error = null;
      _message = null;
    });
    try {
      if (_register) {
        final user = await widget.api.register(_email.text, _password.text);
        if (mounted) {
          setState(() {
            _user = user;
            _message =
                'Tài khoản đã sẵn sàng. Bạn đang đăng nhập vào Nova Haven.';
          });
        }
      } else {
        final user = await widget.api.login(_email.text, _password.text);
        if (mounted) {
          setState(() {
            _user = user;
            _message = 'Đăng nhập thành công.';
          });
        }
      }
    } catch (error) {
      if (mounted) {
        setState(
            () => _error = error.toString().replaceFirst('Bad state: ', ''));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _logout() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await widget.api.logout();
      if (mounted) {
        setState(() {
          _user = null;
          _password.clear();
          _message = 'Bạn đã đăng xuất.';
        });
      }
    } catch (error) {
      if (mounted) {
        setState(
            () => _error = error.toString().replaceFirst('Bad state: ', ''));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Tài khoản Nova Haven')),
      body: SafeArea(
          child: _checkingUser
              ? const Center(
                  child: Column(mainAxisSize: MainAxisSize.min, children: [
                  CircularProgressIndicator(),
                  SizedBox(height: 12),
                  Text('Đang kiểm tra tài khoản…')
                ]))
              : ListView(padding: const EdgeInsets.all(20), children: [
                  Text('Tài khoản phiêu lưu',
                      style: Theme.of(context).textTheme.headlineSmall),
                  const SizedBox(height: 6),
                  Text(
                      'Hộp thư thông báo và cập nhật sẽ theo tài khoản của bạn.',
                      style: Theme.of(context).textTheme.bodyMedium),
                  const SizedBox(height: 18),
                  if (_user != null)
                    Card(
                        child: Padding(
                            padding: const EdgeInsets.all(18),
                            child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(_user!.email,
                                      style: Theme.of(context)
                                          .textTheme
                                          .titleLarge),
                                  const SizedBox(height: 6),
                                  const Text('Tài khoản người chơi'),
                                  if (_user!.isAdmin)
                                    const Text('Quản trị viên'),
                                  const SizedBox(height: 12),
                                  FilledButton.icon(
                                    onPressed: () => Navigator.of(context).push(
                                        MaterialPageRoute<void>(
                                            builder: (_) =>
                                                AccountNotificationsPage(
                                                    api: widget.api))),
                                    icon: const Icon(
                                        Icons.notifications_outlined),
                                    label: const Text('Mở thông báo'),
                                  ),
                                  const SizedBox(height: 8),
                                  OutlinedButton(
                                      onPressed: _busy ? null : _logout,
                                      child: const Text('Đăng xuất')),
                                ])))
                  else
                    Card(
                        child: Padding(
                            padding: const EdgeInsets.all(18),
                            child: Form(
                                key: _formKey,
                                child: Column(
                                    crossAxisAlignment:
                                        CrossAxisAlignment.stretch,
                                    children: [
                                      SegmentedButton<bool>(
                                          segments: const [
                                            ButtonSegment(
                                                value: false,
                                                label: Text('Đăng nhập')),
                                            ButtonSegment(
                                                value: true,
                                                label: Text('Tạo tài khoản'))
                                          ],
                                          selected: {
                                            _register
                                          },
                                          onSelectionChanged: _busy
                                              ? null
                                              : (value) => setState(() {
                                                    _register = value.first;
                                                    _message = null;
                                                    _error = null;
                                                  })),
                                      const SizedBox(height: 16),
                                      TextFormField(
                                          controller: _email,
                                          keyboardType:
                                              TextInputType.emailAddress,
                                          textInputAction: TextInputAction.next,
                                          autofillHints: const [
                                            AutofillHints.email
                                          ],
                                          decoration: const InputDecoration(
                                              labelText: 'Email'),
                                          validator: (value) => value == null ||
                                                  !value.contains('@')
                                              ? 'Nhập email hợp lệ.'
                                              : null),
                                      const SizedBox(height: 12),
                                      TextFormField(
                                          controller: _password,
                                          obscureText: true,
                                          textInputAction: TextInputAction.done,
                                          autofillHints: [
                                            _register
                                                ? AutofillHints.newPassword
                                                : AutofillHints.password
                                          ],
                                          decoration: const InputDecoration(
                                              labelText: 'Mật khẩu'),
                                          validator: (value) => value == null ||
                                                  value.length <
                                                      (_register ? 12 : 1)
                                              ? (_register
                                                  ? 'Mật khẩu cần tối thiểu 12 ký tự.'
                                                  : 'Nhập mật khẩu.')
                                              : null,
                                          onFieldSubmitted: (_) => _submit()),
                                      if (_register) ...[
                                        const SizedBox(height: 8),
                                        Text(
                                            'Mật khẩu tối thiểu 12 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt. Tạo xong là dùng được ngay.',
                                            style: Theme.of(context)
                                                .textTheme
                                                .bodySmall),
                                      ],
                                      const SizedBox(height: 12),
                                      FilledButton(
                                          onPressed: _busy ? null : _submit,
                                          child: Text(_busy
                                              ? 'Đang xử lý…'
                                              : _register
                                                  ? 'Tạo tài khoản'
                                                  : 'Đăng nhập')),
                                    ])))),
                  if (_message != null)
                    _FeedbackCard(message: _message!, isError: false),
                  if (_error != null)
                    _FeedbackCard(message: _error!, isError: true),
                ])),
    );
  }
}

class AccountNotificationsPage extends StatefulWidget {
  const AccountNotificationsPage({super.key, required this.api});
  final UserAccountApi api;
  @override
  State<AccountNotificationsPage> createState() =>
      _AccountNotificationsPageState();
}

class _AccountNotificationsPageState extends State<AccountNotificationsPage> {
  late Future<AccountInbox> _inbox;
  int _page = 1;
  bool _busy = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _inbox = widget.api.notifications(page: _page);
  }

  Future<void> _loadPage(int page) async {
    setState(() {
      _page = page;
      _inbox = widget.api.notifications(page: page);
      _error = null;
    });
    await _inbox;
  }

  Future<void> _reload() => _loadPage(_page);

  Future<void> _mark(AccountNotification item) async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await widget.api.markRead(item.id);
      await _reload();
    } catch (error) {
      setState(() => _error = error.toString().replaceFirst('Bad state: ', ''));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _markAll() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await widget.api.markAllRead();
      await _reload();
    } catch (error) {
      setState(() => _error = error.toString().replaceFirst('Bad state: ', ''));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Thông báo')),
        body: FutureBuilder<AccountInbox>(
            future: _inbox,
            builder: (context, snapshot) {
              if (snapshot.connectionState != ConnectionState.done) {
                return const Center(child: CircularProgressIndicator());
              }
              if (snapshot.error is AccountAuthenticationRequired) {
                return Center(
                    child: Padding(
                        padding: const EdgeInsets.all(24),
                        child:
                            Column(mainAxisSize: MainAxisSize.min, children: [
                          Icon(Icons.lock_outline,
                              size: 36,
                              color: Theme.of(context).colorScheme.primary),
                          const SizedBox(height: 12),
                          Text('Hãy đăng nhập để xem hộp thư thông báo.',
                              textAlign: TextAlign.center,
                              style: Theme.of(context).textTheme.bodyLarge),
                          const SizedBox(height: 12),
                          FilledButton.icon(
                            onPressed: () => Navigator.of(context).push(
                                MaterialPageRoute<void>(
                                    builder: (_) =>
                                        AccountCenterPage(api: widget.api))),
                            icon: const Icon(Icons.person_outline),
                            label: const Text('Đăng nhập hoặc tạo tài khoản'),
                          ),
                        ])));
              }
              if (snapshot.hasError) {
                return Center(
                    child: Padding(
                        padding: const EdgeInsets.all(24),
                        child:
                            Column(mainAxisSize: MainAxisSize.min, children: [
                          Text(
                              snapshot.error
                                  .toString()
                                  .replaceFirst('Bad state: ', ''),
                              textAlign: TextAlign.center),
                          const SizedBox(height: 12),
                          OutlinedButton(
                              onPressed: _reload, child: const Text('Thử lại'))
                        ])));
              }
              final inbox = snapshot.data!;
              return RefreshIndicator(
                  onRefresh: _reload,
                  child: ListView(padding: const EdgeInsets.all(16), children: [
                    Row(children: [
                      Expanded(
                          child: Text('${inbox.unreadCount} tin chưa đọc',
                              style: Theme.of(context).textTheme.titleMedium)),
                      TextButton(
                          onPressed:
                              _busy || inbox.unreadCount == 0 ? null : _markAll,
                          child: const Text('Đọc tất cả'))
                    ]),
                    if (inbox.items.isEmpty)
                      const Padding(
                          padding: EdgeInsets.symmetric(vertical: 56),
                          child: Center(
                              child: Text('Bạn chưa có thông báo mới.'))),
                    ...inbox.items.map((item) => Card(
                        child: ListTile(
                            isThreeLine: true,
                            leading: Icon(
                                item.isRead
                                    ? Icons.mark_email_read_outlined
                                    : Icons.mark_email_unread_outlined,
                                color: item.isRead ? null : NovaPalette.olive),
                            title: Text(item.title),
                            subtitle: Text(
                                '${item.body}\n${item.createdAtUtc.toLocal()}'),
                            onTap: item.isRead || _busy
                                ? null
                                : () => _mark(item)))),
                    if (inbox.total > inbox.pageSize)
                      Padding(
                        padding: const EdgeInsets.symmetric(vertical: 12),
                        child: Wrap(
                          alignment: WrapAlignment.spaceBetween,
                          crossAxisAlignment: WrapCrossAlignment.center,
                          spacing: 12,
                          runSpacing: 8,
                          children: [
                            OutlinedButton(
                              onPressed: _busy || inbox.page <= 1
                                  ? null
                                  : () => _loadPage(inbox.page - 1),
                              child: const Text('← Trang trước'),
                            ),
                            Text(
                                'Trang ${inbox.page} / ${((inbox.total + inbox.pageSize - 1) ~/ inbox.pageSize)}'),
                            OutlinedButton(
                              onPressed: _busy ||
                                      inbox.page >=
                                          ((inbox.total + inbox.pageSize - 1) ~/
                                              inbox.pageSize)
                                  ? null
                                  : () => _loadPage(inbox.page + 1),
                              child: const Text('Trang tiếp →'),
                            ),
                          ],
                        ),
                      ),
                    if (_error != null)
                      _FeedbackCard(message: _error!, isError: true),
                  ]));
            }),
      );
}

class _FeedbackCard extends StatelessWidget {
  const _FeedbackCard({required this.message, required this.isError});
  final String message;
  final bool isError;
  @override
  Widget build(BuildContext context) => Card(
      color: isError ? const Color(0xFF493027) : const Color(0xFF39402D),
      child: Padding(padding: const EdgeInsets.all(12), child: Text(message)));
}
