import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import test from 'node:test';

const root = new URL('../../', import.meta.url);
const read = path => readFile(new URL(path, root), 'utf8');
const lastRule = (css, selector) =>
  [...css.matchAll(new RegExp(`(?:^|\\n)\\s*${selector}\\s*\\{([^}]*)\\}`, 'g'))].at(-1)?.[1] ?? '';

test('community category pages keep a single published entry compact and readable', async () => {
  const page = await read('apps/web/app/community/[kind]/page.tsx');
  const theme = await read('apps/web/app/wynn-parity.css');

  assert.match(page, /className="content community-kind-page"/);
  assert.match(theme, /\.community-kind-page \.article-grid\s*\{[^}]*width:\s*min\(100%,\s*900px\)/s);
  assert.match(theme, /\.community-kind-page \.article-card\s*\{[^}]*max-width:\s*420px/s);
  assert.match(lastRule(theme, String.raw`\.community-kind-page \.article-card`), /background:\s*var\(--nh-paper-light\)/s);
  assert.match(theme, /\.community-kind-page\s*>\s*h1\s*\{[^}]*text-align:\s*center/s);
});

test('community directory uses Vietnamese copy, honest availability and a balanced five-group layout', async () => {
  const page = await read('apps/web/app/community/page.tsx');
  const theme = await read('apps/web/app/wynn-parity.css');

  assert.match(page, /CỘNG ĐỒNG · NOVA HAVEN/);
  assert.match(page, /loadCommunityOverview\(\)/);
  assert.match(page, /community-state-partial/);
  assert.match(page, /Tạm thời chưa tải được mục này\./);
  assert.match(page, /Chưa có nội dung cộng đồng được xuất bản/);
  assert.match(page, /community-overview-grid/);
  assert.doesNotMatch(page, /Events, Guild, Player Hub, Housing Showcase/);
  assert.match(theme, /\.community-overview-grid\s*\{[^}]*grid-template-columns:\s*repeat\(6,\s*minmax\(0,\s*1fr\)\)/s);
  assert.match(theme, /\.community-overview-grid \.community-card\s*\{[^}]*grid-column:\s*span 2/s);
  assert.match(theme, /\.community-overview-grid \.community-card:nth-child\(4\)\s*\{[^}]*grid-column:\s*2\s*\/\s*4/s);
  const tabletRules = [...theme.matchAll(/@media\s*\(max-width:\s*900px\)\s*\{([\s\S]*?)\n\}/g)].at(-1)?.[1] ?? '';
  assert.match(tabletRules, /\.community-overview-grid \.community-card:nth-child\(4\)\s*\{[^}]*grid-column:\s*auto/s);
  assert.match(lastRule(theme, String.raw`\.community-overview-grid \.community-card:nth-child\(4\)`), /grid-column:\s*auto/s);
  assert.match(lastRule(theme, String.raw`\.community-overview-grid \.community-card:nth-child\(5\)`), /width:\s*100%[\s\S]*justify-self:\s*stretch/s);
  assert.match(theme, /\.community-card-link\s*\{[^}]*white-space:\s*nowrap/s);
  assert.match(theme, /@media\s*\(max-width:\s*600px\)\s*\{[^}]*\.community-overview-grid\s*\{\s*grid-template-columns:\s*minmax\(0,\s*1fr\)/s);
  assert.match(theme, /\.community-retry-link[^}]*min-height:\s*44px/s);
  assert.match(theme, /@media\s*\(prefers-reduced-motion:\s*reduce\)\s*\{[^}]*\.community-overview-grid/s);
});
