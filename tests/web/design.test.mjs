import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import test from 'node:test';

const root = new URL('../../', import.meta.url);
const read = path => readFile(new URL(path, root), 'utf8');

test('public visual system uses the real home hero and stays scoped to Nova components', async () => {
  const css = await read('apps/web/app/wynn-parity.css');
  const home = await read('apps/web/app/page.tsx');
  const layout = await read('apps/web/app/layout.tsx');
  const navigation = await read('apps/web/app/SiteNavigation.tsx');

  assert.match(home, /className="hero hero-home"/);
  assert.match(home, /className="hero-landscape-image"/);
  assert.match(home, /className="hero-brand"/);
  assert.match(home, /className="home-intro-band"/);
  assert.doesNotMatch(home, /hero-art|scene-art|hero-character|hero-dragon/);
  assert.match(navigation, /game-toolbar/);
  assert.match(navigation, /server-pill/);
  assert.match(css, /--nh-paper:/);
  assert.match(css, /--nh-green:/);
  assert.match(layout, /import '\.\/wynn-parity\.css'/);
  assert.doesNotMatch(layout, /goodgames-theme|fonts\.googleapis\.com/);
  assert.match(layout, /Ảnh phong cảnh: Einar Storsul · Unsplash/);
  assert.match(layout, /green-meadow-with-trees-and-a-river-near-mountains-X7FEgrPDFNU/);
  assert.match(layout, /Minecraft_-_1\.18_mountains\.jpg/);
  assert.match(layout, /Xbox México · CC BY 3\.0/);
});

test('homepage hero fits short landscape viewports without hiding its primary actions', async () => {
  const css = await read('apps/web/app/wynn-parity.css');

  assert.match(css, /@media\s*\(orientation:\s*landscape\)\s*and\s*\(max-height:\s*500px\)[\s\S]*?\.hero\.hero-home\s*\{[^}]*min-height:\s*calc\(100svh - 63px\)/s);
  assert.match(css, /\.hero-home \.hero-copy\s*\{[^}]*top:\s*50%/s);
  assert.match(css, /\.hero-home \.button\s*\{[^}]*min-height:\s*44px/s);
});

test('admin surfaces use the slate palette and keep the Next dev badge hidden', async () => {
  const adminPage = await read('apps/web/app/admin/page.tsx');
  const adminCss = await read('apps/web/app/admin/admin.css');
  const siteIcons = await read('apps/web/app/SiteIcon.tsx');
  const nextConfig = await read('apps/web/next.config.ts');

  assert.match(adminPage, /className="content admin-login-page"/);
  assert.match(adminPage, /className="form admin-login-form"/);
  assert.match(adminPage, /className="error admin-login-error"/);
  assert.match(adminPage, /className="button button-outline admin-login-retry"/);
  assert.match(adminCss, /\.admin-login-page \.admin-login-form input[^}]*background:\s*var\(--gg-bg-main\)/s);
  assert.match(adminCss, /--gg-bg-main:\s*#1d2422/i);
  assert.match(adminCss, /--gg-red:\s*#9a9b70/i);
  assert.match(adminCss, /section\.content\.admin-login-page > p\.admin-login-error[^}]*background:\s*#351d25/s);
  assert.match(adminCss, /\.kpi-card,[\s\S]*?\.media-card\s*\{[^}]*background:\s*var\(--gg-bg-card\)/s);
  assert.doesNotMatch(siteIcons, /sparkles/);
  assert.match(nextConfig, /devIndicators:\s*false/);
});

test('the navigation bell reads the authenticated unread endpoint and keeps an accessible touch target', async () => {
  const bell = await read('apps/web/app/NotificationBell.tsx');
  const icon = await read('apps/web/app/SiteIcon.tsx');
  const navigation = await read('apps/web/app/SiteNavigation.tsx');
  const css = await read('apps/web/app/wynn-parity.css');

  assert.match(navigation, /<NotificationBell\/>/);
  assert.match(bell, /\/api\/v1\/notifications\/unread-count/);
  assert.match(bell, /credentials:\s*'same-origin'/);
  assert.match(bell, /aria-live="polite"/);
  assert.match(icon, /bell:\s*</);
  assert.match(css, /\.notification-bell-link\s*\{[^}]*width:\s*44px/s);
  assert.match(css, /\.pager a\s*\{[^}]*min-height:\s*44px/s);
});

test('public collections expose query-preserving page navigation', async () => {
  const pager = await read('apps/web/app/PageNavigation.tsx');
  const catalog = await read('apps/web/app/catalog/page.tsx');
  const news = await read('apps/web/app/news/page.tsx');
  const notifications = await read('apps/web/app/notifications/NotificationsClient.tsx');

  assert.match(pager, /rel="prev"/);
  assert.match(pager, /rel="next"/);
  assert.match(pager, /Trang \{page\} \/ \{pages\}/);
  assert.match(catalog, /PageNavigation[\s\S]*href=\{retryHref\}/);
  assert.match(news, /PageNavigation[\s\S]*href="\/news"/);
  assert.match(notifications, /\/api\/v1\/notifications\?page=\$\{requestedPage\}&pageSize=20/);
});
