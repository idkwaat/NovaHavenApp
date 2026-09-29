import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import test from 'node:test';

const root = new URL('../../', import.meta.url);
const read = path => readFile(new URL(path, root), 'utf8');

test('public listing outages are distinct from empty results and offer a retry path', async () => {
  const [wiki, catalog, news, theme] = await Promise.all([
    read('apps/web/app/wiki/page.tsx'),
    read('apps/web/app/catalog/page.tsx'),
    read('apps/web/app/news/page.tsx'),
    read('apps/web/app/wynn-parity.css'),
  ]);

  for (const page of [wiki, catalog, news]) {
    assert.match(page, /public-recovery/);
    assert.match(page, /role="alert"/);
    assert.match(page, /Thử lại/);
    assert.doesNotMatch(page, /API đang offline hoặc chưa có dữ liệu xuất bản/);
  }
  assert.match(wiki, /wikiListHref\(\{q,category,tag,page\}\)/);
  assert.match(theme, /\.public-recovery\s*\{/);
  assert.match(theme, /\.public-recovery-actions\s+a\s*\{[^}]*min-height:\s*44px/s);
  assert.match(theme, /\.public-recovery-actions\s+a:focus-visible/);
});
