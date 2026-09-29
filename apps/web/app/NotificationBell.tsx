'use client';

import Link from 'next/link';
import {useCallback, useEffect, useState} from 'react';
import SiteIcon from './SiteIcon';

export default function NotificationBell() {
  const [count, setCount] = useState<number | null>(null);
  const refresh = useCallback(async () => {
    try {
      const response = await fetch('/api/v1/notifications/unread-count', {
        credentials: 'same-origin',
        cache: 'no-store',
        headers: {Accept: 'application/json'},
      });
      if (response.status === 401) { setCount(null); return; }
      if (!response.ok) return;
      const result = await response.json() as {unreadCount?: unknown};
      if (typeof result.unreadCount === 'number' && Number.isFinite(result.unreadCount)) {
        setCount(Math.max(0, Math.floor(result.unreadCount)));
      }
    } catch {
      // Keep the last known badge during a temporary API/network interruption.
    }
  }, []);

  useEffect(() => {
    void refresh();
    const interval = window.setInterval(() => void refresh(), 60_000);
    const onFocus = () => void refresh();
    const onVisibility = () => { if (document.visibilityState === 'visible') void refresh(); };
    window.addEventListener('focus', onFocus);
    document.addEventListener('visibilitychange', onVisibility);
    return () => {
      window.clearInterval(interval);
      window.removeEventListener('focus', onFocus);
      document.removeEventListener('visibilitychange', onVisibility);
    };
  }, [refresh]);

  const label = count === null ? 'Thông báo' : `Thông báo, ${count} tin chưa đọc`;
  return <Link className="notification-bell-link" href="/notifications" aria-label={label} title={label}>
    <SiteIcon name="bell" className="ui-icon"/>
    {count !== null && count > 0 && <span className="notification-bell-count" aria-hidden="true">{count > 99 ? '99+' : count}</span>}
    <span className="sr-only" role="status" aria-live="polite" aria-atomic="true">{count === null ? '' : `Có ${count} thông báo chưa đọc.`}</span>
  </Link>;
}
