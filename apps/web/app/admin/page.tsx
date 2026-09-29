'use client';

import Link from 'next/link';
import {useCallback, useEffect, useState} from 'react';
import type {ArticleDraftInput} from '@/lib/wiki-model';
import {validateArticle} from '@/lib/wiki-model';
import CategoryManager, {type AdminCategory} from './CategoryManager';
import TagManager, {type AdminTag} from './TagManager';
import RevisionHistory from './RevisionHistory';
import NewsManager from './NewsManager';
import OperationsManager from './OperationsManager';
import RewardCommerceManager from './RewardCommerceManager';
import CommerceOrdersManager from './CommerceOrdersManager';
import KnowledgeManager from './KnowledgeManager';
import CatalogManager from './CatalogManager';
import RecipeManager from './RecipeManager';
import CommunityManager from './CommunityManager';
import IntegrationManager from './IntegrationManager';
import DashboardOverview from './DashboardOverview';
import MediaManager, {type MediaItem} from './MediaManager';
import NotificationManager from './NotificationManager';
import './admin.css';

type Category = AdminCategory;
type Entry = {id: string; slug: string; draftTitle: string; state: number; latestRevisionNumber: number};
const empty: ArticleDraftInput = {title: '', slug: '', summary: '', markdown: '', categoryId: '', tagIds: [], mediaIds: []};

export type AdminTab =
  | 'dashboard'
  | 'wiki-articles'
  | 'wiki-categories'
  | 'wiki-tags'
  | 'wiki-media'
  | 'news'
  | 'commerce-offers'
  | 'rewards'
  | 'orders'
  | 'knowledge'
  | 'catalog'
  | 'recipes'
  | 'community'
  | 'operations'
  | 'integrations'
  | 'notifications';

const tabTitles: Record<AdminTab, string> = {
  'dashboard': 'Tổng quan Dashboard',
  'wiki-articles': 'Bài viết Wiki',
  'wiki-categories': 'Danh mục Wiki',
  'wiki-tags': 'Thẻ phân loại (Tags)',
  'wiki-media': 'Thư viện Media & Ảnh',
  'news': 'Tin tức & Sự kiện',
  'commerce-offers': 'Gói vật phẩm Cửa hàng',
  'rewards': 'Định nghĩa Phần thưởng RPG',
  'orders': 'Lịch sử Đơn hàng',
  'knowledge': 'Knowledge Graph (NPC, Lore, Quest)',
  'catalog': 'Catalog Vật phẩm & Trang bị',
  'recipes': 'Công thức chế tạo (Recipes)',
  'community': 'Hệ thống Cộng đồng',
  'operations': 'Vận hành & Chẩn đoán hệ thống',
  'integrations': 'Khả năng tích hợp (Integrations)',
  'notifications': 'Thông báo người chơi'
};

async function fetchWithTimeout(input: RequestInfo | URL, init: RequestInit = {}, timeoutMs = 8000): Promise<Response> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  try {
    return await fetch(input, {...init, signal: controller.signal});
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw new Error('Backend không phản hồi trong 8 giây. Hãy kiểm tra API local rồi thử lại.');
    }
    throw error;
  } finally {
    clearTimeout(timer);
  }
}

async function csrfToken(): Promise<string> {
  const result = await fetchWithTimeout('/api/v1/auth/csrf', {credentials: 'same-origin', cache: 'no-store'});
  if (!result.ok) throw new Error('Không lấy được CSRF token.');
  return (await result.json() as {token: string}).token;
}

async function messageFrom(response: Response): Promise<string> {
  if (response.status === 412) return 'Dữ liệu đã thay đổi ở nơi khác. Hãy tải lại trước khi sửa.';
  if (response.status === 428) return 'Thiếu ETag. Hãy tải lại bản nháp.';
  if (response.status === 401 || response.status === 403) return 'Bạn không có quyền Admin hoặc phiên đăng nhập đã hết hạn.';
  const data = await response.json().catch(() => null) as {title?: string; errors?: Record<string, string[]>} | null;
  return data?.errors ? Object.values(data.errors).flat().join(' · ') : data?.title ?? `API trả về ${response.status}`;
}

export default function Admin() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [token, setToken] = useState('');
  const[ready,setReady]=useState(true);
  const [loggedIn, setLoggedIn] = useState(false);
  const [activeTab, setActiveTab] = useState<AdminTab>('dashboard');

  const [articles, setArticles] = useState<Entry[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [tags, setTags] = useState<AdminTag[]>([]);
  const [media, setMedia] = useState<MediaItem[]>([]);

  const [id, setId] = useState<string | null>(null);
  const [etag, setEtag] = useState<string | null>(null);
  const [draft, setDraft] = useState<ArticleDraftInput>(empty);
  const [changed, setChanged] = useState(false);
  const [status, setStatus] = useState('');
  const [busy, setBusy] = useState(false);
  const [connectionAttempt, setConnectionAttempt] = useState(0);

  // Sync tab with URL query parameter on mount and when changed
  useEffect(() => {
    const searchTab = new URLSearchParams(window.location.search).get('tab') as AdminTab | null;
    if (searchTab && tabTitles[searchTab]) {
      setActiveTab(searchTab);
    }
  }, []);

  const switchTab = useCallback((newTab: AdminTab | string, targetArticleId?: string) => {
    const tab = newTab as AdminTab;
    setActiveTab(tab);
    const url = new URL(window.location.href);
    url.searchParams.set('tab', tab);
    window.history.replaceState(null, '', url.toString());

    if (targetArticleId && tab === 'wiki-articles') {
      void load(targetArticleId);
    }
  }, []);

  const loadLists = useCallback(async () => {
    const [articleResult, categoryResult, tagResult, mediaResult] = await Promise.all([
      fetchWithTimeout('/api/v1/admin/wiki/articles', {cache: 'no-store'}),
      fetchWithTimeout('/api/v1/admin/wiki/categories', {cache: 'no-store'}),
      fetchWithTimeout('/api/v1/admin/wiki/tags', {cache: 'no-store'}),
      fetchWithTimeout('/api/v1/admin/wiki/media', {cache: 'no-store'})
    ]);
    if (!articleResult.ok || !categoryResult.ok || !tagResult.ok || !mediaResult.ok) {
      setLoggedIn(false);
      return;
    }
    const [items, categoryItems, tagItems, mediaItems] = await Promise.all([
      articleResult.json() as Promise<Entry[]>,
      categoryResult.json() as Promise<Category[]>,
      tagResult.json() as Promise<AdminTag[]>,
      mediaResult.json() as Promise<MediaItem[]>
    ]);
    setArticles(items);
    setCategories(categoryItems);
    setTags(tagItems);
    setMedia(mediaItems);
    setLoggedIn(true);
  }, []);

  useEffect(() => {
    let active = true;
    (async () => {
      try {
        const t = await csrfToken();
        if (!active) return;
        setToken(t);
        await loadLists();
      } catch (error) {
        if (active) setStatus((error as Error).message);
      } finally {
        if (active) setReady(true);
      }
    })();
    return () => { active = false; };
  }, [loadLists, connectionAttempt]);

  async function mutate(path: string, method: 'POST' | 'PATCH' | 'DELETE', body?: object, precondition?: string) {
    const current = token || await csrfToken();
    return fetch(path, {
      method,
      credentials: 'same-origin',
      headers: {
        'X-CSRF-TOKEN': current,
        ...(body ? {'Content-Type': 'application/json'} : {}),
        ...(precondition ? {'If-Match': precondition} : {})
      },
      body: body ? JSON.stringify(body) : undefined,
      cache: 'no-store'
    });
  }

  async function login(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setStatus('');
    try {
      const response = await mutate('/api/v1/auth/login', 'POST', {email, password});
      if (!response.ok) throw Error(await messageFrom(response));
      setPassword('');
      setToken(await csrfToken());
      await loadLists();
      setStatus('Đã đăng nhập thành công.');
    } catch (err) {
      setStatus((err as Error).message);
    } finally {
      setBusy(false);
    }
  }

  async function logout() {
    setBusy(true);
    try {
      await mutate('/api/v1/auth/logout', 'POST');
    } catch {}
    setLoggedIn(false);
    setBusy(false);
    setStatus('Đã đăng xuất.');
  }

  async function load(idToLoad: string) {
    if (changed && !window.confirm('Có thay đổi chưa lưu. Bỏ các thay đổi này?')) return;
    setBusy(true);
    setStatus('');
    try {
      const response = await fetch(`/api/v1/admin/wiki/articles/${idToLoad}`, {cache: 'no-store'});
      if (!response.ok) throw Error(await messageFrom(response));
      const item = await response.json() as ArticleDraftInput;
      setDraft({...item, tagIds: item.tagIds ?? [], mediaIds: item.mediaIds ?? []});
      setId(idToLoad);
      setEtag(response.headers.get('ETag'));
      setChanged(false);
    } catch (err) {
      setStatus((err as Error).message);
    } finally {
      setBusy(false);
    }
  }

  function fresh() {
    if (changed && !window.confirm('Bỏ bản nháp chưa lưu?')) return;
    setId(null);
    setEtag(null);
    setDraft({...empty, categoryId: categories[0]?.id ?? ''});
    setChanged(false);
    setStatus('');
  }

  function update(field: Exclude<keyof ArticleDraftInput, 'tagIds' | 'mediaIds'>, value: string) {
    setDraft(current => ({...current, [field]: value}));
    setChanged(true);
  }

  async function uploadMedia(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;
    setBusy(true);
    setStatus('');
    try {
      const current = token || await csrfToken();
      const form = new FormData();
      form.append('file', file);
      const response = await fetch('/api/v1/admin/wiki/media', {
        method: 'POST',
        credentials: 'same-origin',
        headers: {'X-CSRF-TOKEN': current},
        body: form
      });
      if (!response.ok) throw Error(await messageFrom(response));
      const item = await response.json() as MediaItem;
      setMedia(items => [item, ...items.filter(existing => existing.id !== item.id)]);
      setDraft(current => ({...current, mediaIds: [...(current.mediaIds ?? []), item.id]}));
      setChanged(true);
      setStatus('Đã upload media local và gắn vào bản nháp.');
    } catch (err) {
      setStatus((err as Error).message);
    } finally {
      setBusy(false);
    }
  }

  async function save(e: React.FormEvent) {
    e.preventDefault();
    const errors = validateArticle(draft);
    if (Object.keys(errors).length) {
      setStatus(Object.values(errors).join(' · '));
      return;
    }
    setBusy(true);
    setStatus('');
    try {
      const response = id
        ? await mutate(`/api/v1/admin/wiki/articles/${id}`, 'PATCH', draft, etag ?? undefined)
        : await mutate('/api/v1/admin/wiki/articles', 'POST', draft);
      if (!response.ok) throw Error(await messageFrom(response));
      const json = await response.json() as {id: string; etag: string};
      setId(json.id);
      setEtag(response.headers.get('ETag') ?? json.etag);
      setChanged(false);
      setStatus('Đã lưu bản nháp. Người chơi chưa thấy thay đổi cho đến khi xuất bản.');
      await loadLists();
    } catch (err) {
      setStatus((err as Error).message);
    } finally {
      setBusy(false);
    }
  }

  async function lifecycle(action: 'publish' | 'unpublish') {
    if (!id || !etag || changed) {
      setStatus('Hãy lưu bản nháp và tải ETag mới trước khi xuất bản/hủy xuất bản.');
      return;
    }
    setBusy(true);
    setStatus('');
    try {
      const response = await mutate(`/api/v1/admin/wiki/articles/${id}/${action}`, 'POST', undefined, etag);
      if (!response.ok) throw Error(await messageFrom(response));
      setStatus(action === 'publish' ? 'Đã xuất bản revision mới.' : 'Đã hủy xuất bản.');
      await load(id);
      await loadLists();
    } catch (err) {
      setStatus((err as Error).message);
    } finally {
      setBusy(false);
    }
  }

  if (!ready) {
    return (
      <section className="content">
        <p className="eyebrow">PRIVATE EDITORIAL AREA</p>
        <h1>Nova CMS</h1>
        <p role="status">Đang kết nối Backend…</p>
        <p className="muted">Đang kiểm tra API local và phiên đăng nhập.</p>
      </section>
    );
  }

  if (!loggedIn) {
    return (
      <section className="content admin-login-page" style={{maxWidth: 480, margin: '40px auto'}}>
        <p className="eyebrow">EDITORIAL SYSTEM</p>
        <h1>Nova CMS · Admin</h1>
        <p className="muted">Đăng nhập tài khoản Quản trị viên để quản lý hệ thống Nova Haven RPG.</p>
        <form className="form admin-login-form" onSubmit={login}>
          <label>
            Email
            <input
              type="email"
              autoComplete="username"
              required
              placeholder="admin@novahaven.vn"
              value={email}
              onChange={e => setEmail(e.target.value)}
            />
          </label>
          <label>
            Mật khẩu
            <input
              type="password"
              autoComplete="current-password"
              required
              value={password}
              onChange={e => setPassword(e.target.value)}
            />
          </label>
          <button disabled={busy} type="submit" style={{width: '100%', minHeight: 44}}>
            {busy ? 'Đang xác thực...' : 'Đăng nhập Quản trị'}
          </button>
        </form>
        {status && <p className="error admin-login-error" role="alert" style={{marginTop: 12}}>{status}</p>}
        <div style={{marginTop: 16, textAlign: 'center'}}>
          <button
            className="button button-outline admin-login-retry"
            type="button"
            onClick={() => { setStatus(''); setConnectionAttempt(v => v + 1); }}
          >
            Thử lại kết nối
          </button>
        </div>
      </section>
    );
  }

  return (
    <div className="admin-wrapper">
      {/* Sidebar Navigation */}
      <aside className="admin-sidebar" aria-label="CMS Navigation">
        <div>
          <div className="admin-sidebar-header">
            <div className="admin-brand">
              <div className="admin-brand-icon">NH</div>
              <div>
                <h2>NOVA HAVEN</h2>
                <small>CMS Admin Console</small>
              </div>
            </div>
          </div>

          {/* Group: Dashboard */}
          <div className="admin-nav-group">
            <div className="admin-nav-title">Tổng quan</div>
            <ul className="admin-nav-list">
              <li className={`admin-nav-item ${activeTab === 'dashboard' ? 'is-active' : ''}`}>
                <button type="button" onClick={() => switchTab('dashboard')}>
                  <span className="nav-item-left">
                    <span className="nav-icon">📊</span>
                    <span>Dashboard</span>
                  </span>
                </button>
              </li>
            </ul>
          </div>

          {/* Group: Wiki Content */}
          <div className="admin-nav-group">
            <div className="admin-nav-title">Nội dung Wiki</div>
            <ul className="admin-nav-list">
              <li className={`admin-nav-item ${activeTab === 'wiki-articles' ? 'is-active' : ''}`}>
                <button type="button" onClick={() => switchTab('wiki-articles')}>
                  <span className="nav-item-left">
                    <span className="nav-icon">📝</span>
                    <span>Bài viết Wiki</span>
                  </span>
                  <span className="nav-badge">{articles.length}</span>
                </button>
              </li>
              <li className={`admin-nav-item ${activeTab === 'wiki-categories' ? 'is-active' : ''}`}>
                <button type="button" onClick={() => switchTab('wiki-categories')}>
                  <span className="nav-item-left">
                    <span className="nav-icon">📁</span>
                    <span>Danh mục</span>
                  </span>
                  <span className="nav-badge">{categories.length}</span>
                </button>
              </li>
              <li className={`admin-nav-item ${activeTab === 'wiki-tags' ? 'is-active' : ''}`}>
                <button type="button" onClick={() => switchTab('wiki-tags')}>
                  <span className="nav-item-left">
                    <span className="nav-icon">🏷️</span>
                    <span>Thẻ phân loại</span>
                  </span>
                  <span className="nav-badge">{tags.length}</span>
                </button>
              </li>
              <li className={`admin-nav-item ${activeTab === 'wiki-media' ? 'is-active' : ''}`}>
                <button type="button" onClick={() => switchTab('wiki-media')}>
                  <span className="nav-item-left">
                    <span className="nav-icon">🖼️</span>
                    <span>Thư viện Media</span>
                  </span>
                  <span className="nav-badge">{media.length}</span>
                </button>
              </li>
            </ul>
          </div>

          {/* Group: News & Changelog */}
          <div className="admin-nav-group">
            <div className="admin-nav-title">Bản tin &amp; Sự kiện</div>
            <ul className="admin-nav-list">
              <li className={`admin-nav-item ${activeTab === 'news' ? 'is-active' : ''}`}>
                <button type="button" onClick={() => switchTab('news')}>
                  <span className="nav-item-left">
                    <span className="nav-icon">📰</span>
                    <span>Tin tức / Event</span>
                  </span>
                </button>
              </li>
            </ul>
          </div>

          {/* Group: Commerce & Orders */}
          <div className="admin-nav-group">
            <div className="admin-nav-title">Cửa hàng &amp; Giao dịch</div>
            <ul className="admin-nav-list">
              <li className={`admin-nav-item ${activeTab === 'commerce-offers' ? 'is-active' : ''}`}>
                <button type="button" onClick={() => switchTab('commerce-offers')}>
                  <span className="nav-item-left">
                    <span className="nav-icon">💎</span>
                    <span>Gói Shop (Offers)</span>
                  </span>
                </button>
              </li>
              <li className={`admin-nav-item ${activeTab === 'rewards' ? 'is-active' : ''}`}>
                <button type="button" onClick={() => switchTab('rewards')}>
                  <span className="nav-item-left">
                    <span className="nav-icon">🎁</span>
                    <span>Phần thưởng RPG</span>
                  </span>
                </button>
              </li>
              <li className={`admin-nav-item ${activeTab === 'orders' ? 'is-active' : ''}`}>
                <button type="button" onClick={() => switchTab('orders')}>
                  <span className="nav-item-left">
                    <span className="nav-icon">📦</span>
                    <span>Lịch sử đơn hàng</span>
                  </span>
                </button>
              </li>
            </ul>
          </div>

          {/* Group: Knowledge & World */}
          <div className="admin-nav-group">
            <div className="admin-nav-title">Cốt truyện &amp; Thế giới</div>
            <ul className="admin-nav-list">
              <li className={`admin-nav-item ${activeTab === 'knowledge' ? 'is-active' : ''}`}>
                <button type="button" onClick={() => switchTab('knowledge')}>
                  <span className="nav-item-left">
                    <span className="nav-icon">📜</span>
                    <span>Knowledge Graph</span>
                  </span>
                </button>
              </li>
              <li className={`admin-nav-item ${activeTab === 'catalog' ? 'is-active' : ''}`}>
                <button type="button" onClick={() => switchTab('catalog')}>
                  <span className="nav-item-left">
                    <span className="nav-icon">⚔️</span>
                    <span>Catalog Vật phẩm</span>
                  </span>
                </button>
              </li>
              <li className={`admin-nav-item ${activeTab === 'recipes' ? 'is-active' : ''}`}>
                <button type="button" onClick={() => switchTab('recipes')}>
                  <span className="nav-item-left">
                    <span className="nav-icon">🔨</span>
                    <span>Công thức chế tạo</span>
                  </span>
                </button>
              </li>
              <li className={`admin-nav-item ${activeTab === 'community' ? 'is-active' : ''}`}>
                <button type="button" onClick={() => switchTab('community')}>
                  <span className="nav-item-left">
                    <span className="nav-icon">👥</span>
                    <span>Hệ thống Cộng đồng</span>
                  </span>
                </button>
              </li>
            </ul>
          </div>

          {/* Group: Operations & Diagnostics */}
          <div className="admin-nav-group">
            <div className="admin-nav-title">Hệ thống &amp; Kỹ thuật</div>
            <ul className="admin-nav-list">
              <li className={`admin-nav-item ${activeTab === 'operations' ? 'is-active' : ''}`}>
                <button type="button" onClick={() => switchTab('operations')}>
                  <span className="nav-item-left">
                    <span className="nav-icon">🩺</span>
                    <span>Vận hành &amp; DB</span>
                  </span>
                </button>
              </li>
              <li className={`admin-nav-item ${activeTab === 'integrations' ? 'is-active' : ''}`}>
                <button type="button" onClick={() => switchTab('integrations')}>
                  <span className="nav-item-left">
                    <span className="nav-icon">🔌</span>
                    <span>Tích hợp Server</span>
                  </span>
                </button>
              </li>
              <li className={`admin-nav-item ${activeTab === 'notifications' ? 'is-active' : ''}`}>
                <button type="button" onClick={() => switchTab('notifications')}>
                  <span className="nav-item-left"><span className="nav-icon">🔔</span><span>Thông báo người chơi</span></span>
                </button>
              </li>
            </ul>
          </div>
        </div>

        {/* Sidebar Footer */}
        <div className="admin-sidebar-footer">
          <div className="admin-user-pill">
            <div className="user-avatar">AD</div>
            <div className="user-meta">
              <strong>admin@novahaven.vn</strong>
              <small>Quyền: Quản trị viên</small>
            </div>
          </div>
          <div className="admin-footer-links">
            <Link href="/" target="_blank">↗ Xem Web</Link>
            <button type="button" className="logout-btn" onClick={logout}>Đăng xuất</button>
          </div>
        </div>
      </aside>

      {/* Main Content Pane */}
      <main className="admin-main">
        {/* Top Header Bar */}
        <header className="admin-topbar">
          <div className="admin-breadcrumbs">
            <span>Nova Haven CMS</span>
            <span>/</span>
            <strong>{tabTitles[activeTab]}</strong>
          </div>
          <div className="admin-topbar-actions">
            <div className="system-status-indicator">
              <span className="status-dot"></span>
              <span>API: Port 5080 (Online)</span>
            </div>
            <button
              type="button"
              className="btn-topbar-refresh"
              disabled={busy}
              onClick={() => void loadLists()}
            >
              Làm mới dữ liệu
            </button>
          </div>
        </header>

        {/* Main Body with Active Tab */}
        <div className="admin-body">
          {status && (
            <div className={`admin-alert-banner ${status.includes('Đã') || status.includes('thành công') ? 'success' : 'error'}`}>
              <span>{status}</span>
              <button
                type="button"
                onClick={() => setStatus('')}
                style={{background: 'transparent', border: 'none', color: 'inherit', cursor: 'pointer'}}
              >
                ✕
              </button>
            </div>
          )}

          {/* Tab 1: Dashboard Overview */}
          {activeTab === 'dashboard' && (
            <DashboardOverview
              articles={articles}
              categoriesCount={categories.length}
              tagsCount={tags.length}
              mediaCount={media.length}
              onNavigate={switchTab}
              onFreshArticle={fresh}
            />
          )}

          {/* Tab 2: Wiki Articles Editor & List */}
          {activeTab === 'wiki-articles' && (
            <section className="content" style={{maxWidth: '100%', margin: 0, padding: 0}}>
              <div className="admin-page-header">
                <p className="eyebrow">NOVA HAVEN · WIKI WORKBENCH</p>
                <h1>Quản lý bài viết Wiki</h1>
                <p>Biên tập bài viết, quản lý bản nháp và lịch sử phiên bản xuất bản.</p>
              </div>

              <div className="form-row">
                <button type="button" onClick={fresh}>+ Bài mới</button>
                <button type="button" onClick={() => void loadLists()}>Làm mới danh sách</button>
              </div>

              <div className="tile-grid">
                {articles.map(item => (
                  <button type="button" className="article-card" key={item.id} onClick={() => void load(item.id)}>
                    <strong>{item.draftTitle}</strong>
                    <small>
                      {item.slug} · {['Nháp', 'Đã xuất bản', 'Đã hủy xuất bản'][item.state] ?? item.state} · r{item.latestRevisionNumber}
                    </small>
                  </button>
                ))}
              </div>

              <h2>{id ? 'Biên tập bài Wiki' : 'Tạo bài Wiki mới'}</h2>
              <form className="form" onSubmit={save}>
                <label>
                  Tiêu đề bài viết
                  <input value={draft.title} maxLength={120} required onChange={e => update('title', e.target.value)} />
                </label>
                <label>
                  Đường dẫn tĩnh (Slug)
                  <input value={draft.slug} maxLength={120} required onChange={e => update('slug', e.target.value)} placeholder="huong-dan-khoi-dau" />
                </label>
                <label>
                  Danh mục bài viết
                  <select required value={draft.categoryId} onChange={e => update('categoryId', e.target.value)}>
                    <option value="">Chọn danh mục</option>
                    {categories.filter(c => c.isActive).map(c => (
                      <option key={c.id} value={c.id}>{c.name}</option>
                    ))}
                  </select>
                </label>

                <fieldset className="tag-fieldset">
                  <legend>Thẻ phân loại (Tags, tối đa 10)</legend>
                  {tags.filter(t => t.isActive).map(tag => (
                    <label key={tag.id} className="check-label">
                      <input
                        type="checkbox"
                        checked={(draft.tagIds ?? []).includes(tag.id)}
                        disabled={busy || ((draft.tagIds ?? []).length >= 10 && !(draft.tagIds ?? []).includes(tag.id))}
                        onChange={e => {
                          setDraft(current => ({
                            ...current,
                            tagIds: e.target.checked
                              ? [...(current.tagIds ?? []), tag.id]
                              : (current.tagIds ?? []).filter(item => item !== tag.id)
                          }));
                          setChanged(true);
                        }}
                      />
                      {tag.name}
                    </label>
                  ))}
                </fieldset>

                <fieldset className="tag-fieldset">
                  <legend>Media đính kèm (tối đa 20 ảnh)</legend>
                  <label>
                    Upload ảnh mới (PNG/JPEG/WebP, tối đa 5 MB)
                    <input type="file" accept="image/png,image/jpeg,image/webp" disabled={busy} onChange={uploadMedia} />
                  </label>
                  {media.map(item => (
                    <label key={item.id} className="check-label">
                      <input
                        type="checkbox"
                        checked={(draft.mediaIds ?? []).includes(item.id)}
                        disabled={busy || ((draft.mediaIds ?? []).length >= 20 && !(draft.mediaIds ?? []).includes(item.id))}
                        onChange={e => {
                          setDraft(current => ({
                            ...current,
                            mediaIds: e.target.checked
                              ? [...(current.mediaIds ?? []), item.id]
                              : (current.mediaIds ?? []).filter(i => i !== item.id)
                          }));
                          setChanged(true);
                        }}
                      />
                      <span>{item.originalFileName} · {item.width}×{item.height} · <a href={item.url} target="_blank" rel="noreferrer">xem ảnh</a></span>
                    </label>
                  ))}
                </fieldset>

                <label>
                  Tóm tắt ngắn (Summary)
                  <textarea style={{minHeight: 80}} maxLength={500} value={draft.summary} onChange={e => update('summary', e.target.value)} />
                </label>
                <label>
                  Nội dung Markdown
                  <textarea required style={{minHeight: 280}} maxLength={50000} value={draft.markdown} onChange={e => update('markdown', e.target.value)} />
                </label>

                <div className="form-row">
                  <button type="submit" disabled={busy}>Lưu bản nháp</button>
                  <button type="button" disabled={busy || !id || changed} onClick={() => void lifecycle('publish')}>Xuất bản bài viết</button>
                  <button className="danger" type="button" disabled={busy || !id || changed} onClick={() => void lifecycle('unpublish')}>Hủy xuất bản</button>
                </div>
              </form>

              {id && (
                <RevisionHistory
                  articleId={id}
                  etag={etag}
                  mutate={mutate}
                  onStatus={setStatus}
                  onRestored={async () => { await load(id); await loadLists(); }}
                  disabled={busy}
                />
              )}
            </section>
          )}

          {/* Tab 3: Wiki Categories */}
          {activeTab === 'wiki-categories' && (
            <CategoryManager
              categories={categories}
              mutate={mutate}
              refresh={loadLists}
              onStatus={setStatus}
              errorFor={messageFrom}
              disabled={busy}
            />
          )}

          {/* Tab 4: Wiki Tags */}
          {activeTab === 'wiki-tags' && (
            <TagManager
              tags={tags}
              mutate={mutate}
              refresh={loadLists}
              onStatus={setStatus}
              errorFor={messageFrom}
              disabled={busy}
            />
          )}

          {/* Tab 5: Wiki Media Library */}
          {activeTab === 'wiki-media' && (
            <MediaManager
              media={media}
              token={token}
              csrfToken={csrfToken}
              onRefresh={loadLists}
              onStatus={setStatus}
              disabled={busy}
            />
          )}

          {/* Tab 6: News Manager */}
          {activeTab === 'news' && (
            <NewsManager
              mutate={mutate}
              errorFor={messageFrom}
              onStatus={setStatus}
              disabled={busy}
            />
          )}

          {/* Tab 7: Commerce Offers (Shop) */}
          {activeTab === 'commerce-offers' && (
            <RewardCommerceManager
              mutate={mutate}
              errorFor={messageFrom}
              onStatus={setStatus}
              disabled={busy}
              initialMode="commerce"
            />
          )}

          {/* Tab 8: Rewards */}
          {activeTab === 'rewards' && (
            <RewardCommerceManager
              mutate={mutate}
              errorFor={messageFrom}
              onStatus={setStatus}
              disabled={busy}
              initialMode="reward"
            />
          )}

          {/* Tab 9: Orders */}
          {activeTab === 'orders' && (
            <CommerceOrdersManager />
          )}

          {/* Tab 10: Knowledge Graph */}
          {activeTab === 'knowledge' && (
            <KnowledgeManager
              mutate={mutate}
              errorFor={messageFrom}
              onStatus={setStatus}
              disabled={busy}
            />
          )}

          {/* Tab 11: Catalog Items */}
          {activeTab === 'catalog' && (
            <CatalogManager
              mutate={mutate}
              errorFor={messageFrom}
              onStatus={setStatus}
              disabled={busy}
            />
          )}

          {/* Tab 12: Recipes */}
          {activeTab === 'recipes' && (
            <RecipeManager
              mutate={mutate}
              errorFor={messageFrom}
              onStatus={setStatus}
              disabled={busy}
            />
          )}

          {/* Tab 13: Community */}
          {activeTab === 'community' && (
            <CommunityManager
              mutate={mutate}
              errorFor={messageFrom}
              onStatus={setStatus}
              disabled={busy}
            />
          )}

          {/* Tab 14: Operations */}
          {activeTab === 'operations' && (
            <OperationsManager />
          )}

          {/* Tab 15: Integrations */}
          {activeTab === 'integrations' && (
            <IntegrationManager
              mutate={mutate}
              errorFor={messageFrom}
              onStatus={setStatus}
              disabled={busy}
            />
          )}
          {activeTab === 'notifications' && (
            <NotificationManager mutate={mutate} errorFor={messageFrom} onStatus={setStatus} disabled={busy} />
          )}
        </div>
      </main>
    </div>
  );
}
