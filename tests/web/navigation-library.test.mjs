import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import test from 'node:test';

const root = new URL('../../', import.meta.url);
const read = path => readFile(new URL(path, root), 'utf8').catch(() => '');
const lastRule = (css, selector) =>
  [...css.matchAll(new RegExp(`(?:^|\\n)${selector}\\s*\\{([^}]*)\\}`, 'g'))].at(-1)?.[1] ?? '';

test('shared navigation exposes home, centered server address and an accessible sidebar', async () => {
  const layout = await read('apps/web/app/layout.tsx');
  const navigation = await read('apps/web/app/SiteNavigation.tsx');
  const css = await read('apps/web/app/styles.css');

  assert.match(layout, /<SiteNavigation\s*\/>/);
  assert.match(navigation, /href="\/"/);
  assert.match(navigation, /aria-label="Mở menu"/);
  assert.match(navigation, /Nhấn ☰ để đóng/);
  assert.match(navigation, /minecraftServerAddress\?\?'Máy chủ chưa công bố'/);
  assert.doesNotMatch(navigation, /play\.novahaven\.(?:local|net)/);
  assert.match(css, /\.site-nav-drawer\s*\{/);
  assert.match(css, /\.game-toolbar\s*\{[^}]*position:\s*relative/s);
  assert.match(css, /\.server-pill\s*\{[^}]*position:\s*absolute;[^}]*left:\s*50%;[^}]*transform:\s*translate\(-50%,\s*-50%\)/s);
  assert.match(css, /\.site-nav-menu\s*\{[^}]*top:\s*calc\(50%\s*-\s*19px\);[^}]*transform:\s*none/s);
  assert.match(css, /\.search-popover\s*\{[^}]*width:\s*40px;[^}]*height:\s*40px/s);
  assert.match(css, /\.search-button\s*\{[^}]*width:\s*40px;[^}]*height:\s*40px/s);
});

test('menu works without client hydration and public content keeps readable contrast', async () => {
  const navigation = await read('apps/web/app/SiteNavigation.tsx');
  const css = await read('apps/web/app/wynn-parity.css');

  assert.match(navigation, /type="checkbox"/);
  assert.match(navigation, /id="site-nav-toggle"/);
  assert.match(navigation, /htmlFor="site-nav-toggle"/);
  assert.match(navigation, /site-nav-backdrop/);
  assert.match(css, /font-family:\s*Arial,\s*"Segoe UI",\s*Tahoma,\s*sans-serif/);
  assert.match(css, /\.content h1,\s*\.content h2,\s*\.content h3\s*\{[^}]*text-shadow:\s*none/s);
  assert.match(lastRule(css, String.raw`(?:\.wiki-filters,\s*)?\.catalog-filters`), /background:\s*var\(--nh-paper-light\)/s);
  assert.match(lastRule(css, String.raw`\.news-aside`), /background:\s*#3a3328/s);
});

test('sidebar is an opaque viewport layer, not a fixed child of a transformed toggle', async () => {
  const legacyCss = await read('apps/web/app/styles.css');
  const css = await read('apps/web/app/wynn-parity.css');

  assert.match(legacyCss, /\.site-nav-menu\s*\{[^}]*top:\s*calc\(50%\s*-\s*19px\);[^}]*transform:\s*none/s);
  assert.match(css, /\.site-nav-layer\s*\{[^}]*position:\s*fixed;[^}]*inset:\s*0;[^}]*z-index:\s*1000/s);
  assert.match(css, /\.site-nav-backdrop\s*\{[^}]*position:\s*absolute;[^}]*inset:\s*0;/s);
  assert.match(css, /\.site-nav-drawer\s*\{[^}]*position:\s*absolute;[^}]*inset:\s*0 auto 0 0;/s);
  assert.match(lastRule(css, String.raw`\.site-nav-drawer`), /background:\s*var\(--nh-paper\)/s);
  assert.match(css, /\.site-nav-backdrop\s*\{[^}]*cursor:\s*pointer/s);
});

test('sidebar footer remains in scroll flow instead of covering the last destination', async () => {
  const css = await read('apps/web/app/wynn-parity.css');
  const drawer = lastRule(css, String.raw`\.site-nav-drawer`);
  const footer = lastRule(css, String.raw`\.site-nav-footer`);

  assert.match(drawer, /display:\s*flex/);
  assert.match(drawer, /min-height:\s*100dvh/);
  assert.match(footer, /position:\s*static/);
  assert.match(footer, /margin:\s*18px\s+0\s+0/);
});

test('centered server address stays visible on narrow screens without toolbar collisions', async () => {
  const css = await read('apps/web/app/wynn-parity.css');
  const baseCss = await read('apps/web/app/styles.css');

  assert.match(css, /@media\s*\(max-width:\s*640px\)[\s\S]*?\.server-pill\s*\{[^}]*display:\s*inline-flex/s);
  assert.match(css, /@media\s*\(max-width:\s*600px\)[\s\S]*?\.signin-button\s*\{[^}]*display:\s*none/s);
  assert.match(css, /@media\s*\(max-width:\s*360px\)[\s\S]*?\.home-button\s*\{[^}]*display:\s*none/s);
  assert.match(baseCss, /\.server-pill\s*\{[^}]*left:\s*50%;[^}]*transform:\s*translate\(-50%,\s*-50%\)/s);
});

test('desktop cart utility shares the toolbar row and does not wrap below navigation', async () => {
  const commerceCss = await read('apps/web/app/commerce/store.css');
  assert.match(commerceCss, /\.cart-status-link\s*\{[^}]*grid-column:\s*5;/s);
});

test('footer becomes a centered stack on tablet and mobile widths', async () => {
  const css = await read('apps/web/app/wynn-parity.css');
  const tabletRules = [...css.matchAll(/@media\s*\(max-width:\s*900px\)\s*\{([\s\S]*?)\n\}/g)]
    .map(match => match[1])
    .find(rules => rules.includes('.site-footer') && /\.site-footer\s*\{[^}]*grid-template-columns:\s*1fr;/.test(rules));

  assert.ok(tabletRules, 'expected a tablet breakpoint containing the footer rules');
  assert.match(tabletRules, /\.site-footer\s*\{[^}]*grid-template-columns:\s*1fr;/s);
  assert.match(tabletRules, /\.footer-brand,\s*\.footer-meta\s*\{[^}]*justify-items:\s*center/s);
  assert.match(tabletRules, /\.footer-meta\s*\{[^}]*grid-column:\s*auto/s);
});

test('Wynncraft screen structure uses separate page compositions, not one graphite card palette', async () => {
  const css = await read('apps/web/app/wynn-parity.css');

  assert.match(css, /\.hero\.hero-home\s*\{[^}]*min-height:\s*clamp/s);
  assert.match(css, /\.wiki-layout,\s*\.catalog-layout,\s*\.news-layout\s*\{[^}]*display:\s*grid/s);
  assert.match(css, /\.hero-home \.hero-landscape-image\s*\{[^}]*images\.unsplash\.com/s);
  assert.match(css, /\.news-card\s*\{[^}]*grid-template-columns/s);
});

test('Wiki library only shows categories with published articles and has a quieter footer contract', async () => {
  const listing = await read('apps/web/app/wiki/page.tsx');
  const layout = await read('apps/web/app/layout.tsx');

  assert.match(listing, /categories\.filter\(category => category\.articleCount > 0\)/);
  assert.doesNotMatch(listing, /categories\.map\(x/);
  assert.match(layout, /Khám phá/);
  assert.match(layout, /Trang chủ/);
});

test('Wiki bookmarks remain browser-local and revalidate every saved article as public', async () => {
  const navigation = await read('apps/web/app/SiteNavigation.tsx');
  const footer = await read('apps/web/app/layout.tsx');
  const route = await read('apps/web/app/bookmarks/page.tsx');
  const list = await read('apps/web/app/bookmarks/BookmarkList.tsx');
  const article = await read('apps/web/app/wiki/[slug]/BookmarkButton.tsx');
  const css = await read('apps/web/app/wynn-parity.css');

  assert.match(navigation, /href:\s*'\/bookmarks',\s*label:\s*'Bài đã lưu'/);
  assert.match(footer, /href="\/bookmarks">Bài đã lưu<\/Link>/);
  assert.match(route, /robots:\s*\{index:false,follow:false\}/);
  assert.match(list, /loadPublishedWikiArticles\(slugs\)/);
  assert.match(list, /Tìm trong bài đã lưu/);
  assert.match(list, /filterBookmarkedArticles\(articles,query\)/);
  assert.match(article, /aria-pressed=\{saved\}/);
  assert.match(article, /Lưu bài/);
  assert.match(css, /\.wiki-bookmark-button,\s*\.bookmark-primary-link\s*\{[^}]*min-height:\s*44px/s);
  assert.match(css, /\.bookmark-search input\s*\{[^}]*min-height:\s*44px/s);
  assert.match(css, /\.bookmarks-page :is\(a,button\):focus-visible/);
});

test('recent Wiki history is local, bounded and only tracks server-confirmed public articles', async () => {
  const navigation = await read('apps/web/app/SiteNavigation.tsx');
  const article = await read('apps/web/app/wiki/[slug]/page.tsx');
  const tracker = await read('apps/web/app/wiki/[slug]/RecentArticleTracker.tsx');
  const route = await read('apps/web/app/recent/page.tsx');
  const list = await read('apps/web/app/recent/RecentWikiList.tsx');
  const css = await read('apps/web/app/wynn-parity.css');

  assert.match(navigation, /href:\s*'\/recent',\s*label:\s*'Đọc gần đây'/);
  assert.match(article, /<RecentArticleTracker slug=\{article\.slug\}\/?>/);
  assert.match(tracker, /if\(ready&&!storageError\)record\(slug\)/);
  assert.match(route, /robots:\s*\{index:false,follow:false\}/);
  assert.match(list, /loadPublishedWikiArticles\(slugs\)/);
  assert.match(list, /window\.confirm\(/);
  assert.match(css, /\.recent-results-heading\s*\{/);
  assert.match(css, /\.recent-clear-button\s*\{\s*flex:\s*0\s+0\s+auto/s);
});

test('Admin connection gate times out and explains how to recover', async () => {
  const admin = await read('apps/web/app/admin/page.tsx');

  assert.match(admin, /AbortController/);
  assert.match(admin, /AbortError/);
  assert.match(admin, /Thử lại/);
  assert.match(admin, /const\s*\[\s*ready\s*,\s*setReady\s*\]\s*=\s*useState\(true\)/);
  assert.doesNotMatch(admin, /if\(!ready\)return <section className="content"><h1>Nova CMS<\/h1><p>Đang kết nối Backend\.\.\.<\/p><\/section>/);
});
