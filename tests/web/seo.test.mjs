import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import test from 'node:test';

const root = new URL('../../', import.meta.url);
const read = async path => readFile(new URL(path, root), 'utf8').catch(() => '');

test('public SEO routes allow published content and exclude Admin', async () => {
  const robots = await read('apps/web/app/robots.ts');
  const sitemap = await read('apps/web/app/sitemap.ts');
  assert.match(robots, /disallow:\s*['"]\/admin['"]/i);
  assert.match(robots, /sitemap/i);
  for (const path of ['/api/v1/wiki/articles', '/api/v1/knowledge/', '/api/v1/community/', '/api/v1/catalog/items', '/api/v1/news']) {
    assert.match(sitemap, new RegExp(path.replaceAll('/', '\\/')));
  }
  assert.doesNotMatch(sitemap, /admin\//i);
});

test('root metadata defines a site origin and canonical social defaults', async () => {
  const layout = await read('apps/web/app/layout.tsx');
  assert.match(layout, /metadataBase/);
  assert.match(layout, /alternates/);
  assert.match(layout, /openGraph/);
});
