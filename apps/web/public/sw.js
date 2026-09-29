self.addEventListener('push', event => {
  let payload = {};
  try { payload = event.data ? event.data.json() : {}; } catch { payload = { body: event.data?.text() ?? '' }; }
  const title = typeof payload.title === 'string' ? payload.title : 'Nova Haven';
  const body = typeof payload.body === 'string' ? payload.body : 'Bạn có thông báo mới.';
  const href = typeof payload.href === 'string' && payload.href.startsWith('/') && !payload.href.startsWith('//') && !payload.href.includes('\\') ? payload.href : '/notifications';
  event.waitUntil(self.registration.showNotification(title, { body, data: { href } }));
});

self.addEventListener('notificationclick', event => {
  event.notification.close();
  const path = event.notification.data?.href;
  const target = typeof path === 'string' && path.startsWith('/') && !path.startsWith('//') ? new URL(path, self.location.origin).href : new URL('/notifications', self.location.origin).href;
  event.waitUntil(self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then(clients => {
    const existing = clients.find(client => new URL(client.url).origin === self.location.origin);
    return existing ? existing.navigate(target).then(() => existing.focus()) : self.clients.openWindow(target);
  }));
});
