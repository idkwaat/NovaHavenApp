import assert from 'node:assert/strict';
import {spawn} from 'node:child_process';
import {createServer} from 'node:http';
import {readFile} from 'node:fs/promises';
import test from 'node:test';

const root = new URL('../../', import.meta.url);
const read = path => readFile(new URL(path, root), 'utf8').catch(() => '');

test('demo seed is explicit and restricted to a local API', async () => {
  const script = await read('scripts/demo-seed.mjs');
  const packageJson = JSON.parse(await read('package.json'));
  assert.equal(packageJson.scripts['seed:demo'], 'node scripts/demo-seed.mjs');
  assert.match(script, /NOVA_API_URL/);
  assert.match(script, /NOVA_ADMIN_EMAIL/);
  assert.match(script, /NOVA_ADMIN_PASSWORD/);
  assert.match(script, /localhost.*127\.0\.0\.1/s);
  assert.match(script, /Không chạy demo seed trên API từ xa/);
});

test('preview seed helper targets only port 5081 and never echoes the Admin password', async () => {
  const script = await read('scripts/seed-preview-content.ps1');
  assert.ok(script.trim(), 'the current preview should have a one-command local seeding helper');
  assert.match(script, /127\.0\.0\.1:5081/);
  assert.match(script, /Read-Host[^\r\n]*AsSecureString/);
  assert.match(script, /SecureStringToBSTR/);
  assert.match(script, /ZeroFreeBSTR/);
  assert.match(script, /Remove-Item Env:NOVA_ADMIN_PASSWORD/);
  assert.doesNotMatch(script, /NOVA_ADMIN_PASSWORD\s*=\s*['"]\w/);
});

test('curated Vietnamese Wiki seed contains substantial, classified, unique articles', async () => {
  const content = await read('scripts/demo-wiki-content.json');
  assert.ok(content.trim(), 'the curated Wiki content dataset must exist');

  const dataset = JSON.parse(content);
  const categoryCounts = new Map();
  const slugs = new Set();
  const tagSlugs = new Set(dataset.tags.map(tag => tag.slug));

  assert.equal(dataset.articles.length, 12, 'the seed should add twelve substantive Wiki articles');
  assert.equal(dataset.tags.length, 4, 'the seed should classify articles with four focused tags');

  for (const article of dataset.articles) {
    assert.match(article.slug, /^[a-z0-9]+(?:-[a-z0-9]+)*$/);
    assert.ok(!slugs.has(article.slug), `article slug must be unique: ${article.slug}`);
    slugs.add(article.slug);
    assert.ok(article.title.length > 5 && article.title.length <= 120);
    assert.ok(article.summary.length >= 50 && article.summary.length <= 300);
    assert.ok(['khoi-hanh', 'lop-nhan-vat', 'vung-dat'].includes(article.categorySlug));
    assert.ok(article.tagSlugs.length >= 1 && article.tagSlugs.length <= 10);
    assert.ok(article.tagSlugs.every(slug => tagSlugs.has(slug)), `${article.slug} refers to a missing tag`);
    assert.ok(article.markdown.length >= 700, `${article.slug} must contain useful article text`);
    assert.ok((article.markdown.match(/^## /gm) ?? []).length >= 3, `${article.slug} needs a readable section structure`);
    assert.match(article.markdown, /[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ]/i,
      `${article.slug} should retain Vietnamese text`);
    categoryCounts.set(article.categorySlug, (categoryCounts.get(article.categorySlug) ?? 0) + 1);
  }

  assert.deepEqual([...categoryCounts.entries()].sort(), [
    ['khoi-hanh', 4],
    ['lop-nhan-vat', 4],
    ['vung-dat', 4]
  ]);
});

test('News seed contains only substantial posts about existing project capabilities', async () => {
  const content = await read('scripts/demo-news-content.json');
  assert.ok(content.trim(), 'the project News dataset must exist');

  const posts = JSON.parse(content).posts;
  const slugs = new Set();
  assert.equal(posts.length, 4, 'four real project update posts should be available');

  for (const post of posts) {
    assert.match(post.slug, /^[a-z0-9]+(?:-[a-z0-9]+)*$/);
    assert.ok(!slugs.has(post.slug), `news slug must be unique: ${post.slug}`);
    slugs.add(post.slug);
    assert.ok(post.title.length > 5 && post.title.length <= 160);
    assert.ok(post.summary.length >= 50 && post.summary.length <= 500);
    assert.ok(post.markdown.length >= 500, `${post.slug} needs more than a teaser`);
    assert.ok((post.markdown.match(/^## /gm) ?? []).length >= 2, `${post.slug} needs readable sections`);
  }
});

test('Knowledge seed uses linked lore records without inventing world coordinates', async () => {
  const content = await read('scripts/demo-knowledge-content.json');
  assert.ok(content.trim(), 'the authored Knowledge dataset must exist');

  const dataset = JSON.parse(content);
  const locationsBySlug = new Set(dataset.locations.map(location => location.slug));
  const slugs = new Set();
  assert.equal(dataset.locations.length, 4);
  assert.equal(dataset.npcs.length, 4);

  for (const location of dataset.locations) {
    assert.match(location.slug, /^[a-z0-9]+(?:-[a-z0-9]+)*$/);
    assert.ok(!slugs.has(location.slug), `knowledge slug must be unique: ${location.slug}`);
    slugs.add(location.slug);
    assert.equal(location.latitude, null, `${location.slug} must not invent latitude`);
    assert.equal(location.longitude, null, `${location.slug} must not invent longitude`);
    assert.ok(location.markdown.includes('lore biên soạn'), `${location.slug} must identify its editorial status`);
  }

  for (const npc of dataset.npcs) {
    assert.match(npc.slug, /^[a-z0-9]+(?:-[a-z0-9]+)*$/);
    assert.ok(!slugs.has(npc.slug), `knowledge slug must be unique: ${npc.slug}`);
    slugs.add(npc.slug);
    assert.ok(npc.role.trim().length > 0 && npc.role.length <= 120);
    assert.ok(locationsBySlug.has(npc.locationSlug) || npc.locationSlug === 'thung-lung-sao',
      `${npc.slug} must point to an authored or existing location`);
    assert.ok(npc.markdown.includes('nhân vật lore'), `${npc.slug} must identify its editorial status`);
  }
});

test('Catalog and Community seed are substantial, cited and visibly labeled as local demo data', async () => {
  const catalogText = await read('scripts/demo-catalog-content.json');
  const communityText = await read('scripts/demo-community-content.json');
  assert.ok(catalogText.trim(), 'the vanilla-reference Catalog dataset must exist');
  assert.ok(communityText.trim(), 'the local Community demo dataset must exist');

  const catalog = JSON.parse(catalogText).items;
  const community = JSON.parse(communityText).entries;
  assert.equal(catalog.length, 12);
  assert.equal(community.length, 10);
  const catalogKinds = new Map();
  const communityKinds = new Map();
  const slugs = new Set();

  for (const item of catalog) {
    assert.match(item.slug, /^[a-z0-9]+(?:-[a-z0-9]+)*$/);
    assert.ok(!slugs.has(item.slug), `seed slugs must be unique: ${item.slug}`);
    slugs.add(item.slug);
    assert.ok(['item', 'weapon', 'armor', 'material', 'fish', 'crop'].includes(item.kind));
    assert.ok(item.summary.length >= 50 && item.markdown.length >= 250);
    assert.match(item.sourceUrl, /^https:\/\/www\.minecraft\.net\//);
    assert.match(item.markdown, /dữ liệu minh họa local/i);
    assert.match(item.markdown, /không xác nhận.*Nova Haven/i);
    catalogKinds.set(item.kind, (catalogKinds.get(item.kind) ?? 0) + 1);
  }

  assert.deepEqual([...catalogKinds.entries()].sort(), [
    ['armor', 2], ['crop', 2], ['fish', 1], ['item', 2], ['material', 3], ['weapon', 2]
  ]);

  for (const entry of community) {
    assert.match(entry.slug, /^[a-z0-9]+(?:-[a-z0-9]+)*$/);
    assert.ok(!slugs.has(entry.slug), `all local seed slugs must be unique: ${entry.slug}`);
    slugs.add(entry.slug);
    assert.ok(['event', 'guild', 'player', 'housing', 'leaderboard'].includes(entry.kind));
    assert.ok(entry.summary.length >= 50 && entry.markdown.length >= 250);
    assert.match(entry.markdown, /dữ liệu minh họa local/i);
    assert.match(entry.markdown, /không phải.*(máy chủ|Nova Haven|thật)/i);
    if (entry.kind === 'event') {
      assert.ok(Number.isInteger(entry.startsInDays) && entry.startsInDays > 0);
      assert.ok(Number.isInteger(entry.durationDays) && entry.durationDays > 0);
      assert.equal(entry.registrationOpen, false, 'fictional event samples must not accept signups');
    }
    if (entry.kind === 'guild') assert.ok(entry.motto.length > 0 && !entry.discordUrl);
    if (entry.kind === 'player') assert.ok(entry.handle.length >= 2 && entry.bio.length > 100);
    if (entry.kind === 'housing') assert.ok(entry.ownerDisplayName.length > 0 && entry.galleryMarkdown.length > 100);
    if (entry.kind === 'leaderboard') {
      assert.ok(entry.rows.length >= 3);
      assert.ok(entry.rows.every(row => /minh họa|không phải/i.test(row.note)));
    }
    communityKinds.set(entry.kind, (communityKinds.get(entry.kind) ?? 0) + 1);
  }

  assert.deepEqual([...communityKinds.entries()].sort(), [
    ['event', 2], ['guild', 2], ['housing', 2], ['leaderboard', 1], ['player', 3]
  ]);

  const notice = await read('apps/web/src/components/DemoDataNotice.tsx');
  assert.match(notice, /DỮ LIỆU MINH HỌA LOCAL/);
  for (const path of [
    'apps/web/app/catalog/page.tsx', 'apps/web/app/catalog/[slug]/page.tsx',
    'apps/web/app/community/page.tsx', 'apps/web/app/community/[kind]/page.tsx',
    'apps/web/app/community/[kind]/[slug]/page.tsx'
  ]) {
    assert.match(await read(path), /DemoDataNotice/,
      `${path} must include a provenance notice even when opened directly`);
  }
});

test('demo seed publishes project News, linked Knowledge and local Catalog/Community samples', async () => {
  const created = [];
  let nextId = 1;
  const server = createServer(async (request, response) => {
    let bodyText = '';
    for await (const chunk of request) bodyText += chunk;
    const path = new URL(request.url, 'http://127.0.0.1').pathname;

    if (path === '/api/v1/auth/csrf') {
      response.writeHead(200, {'Content-Type': 'application/json'});
      response.end(JSON.stringify({token: 'local-test-csrf'}));
      return;
    }
    if (path === '/api/v1/auth/login') {
      response.writeHead(204);
      response.end();
      return;
    }
    if (request.method === 'GET' && /\/[0-9a-f-]{36}$/.test(path)) {
      response.writeHead(200, {'Content-Type': 'application/json', ETag: '"local-test-etag"'});
      response.end(JSON.stringify({id: path.split('/').at(-1), state: 'Draft', etag: '"local-test-etag"'}));
      return;
    }
    if (request.method === 'GET') {
      response.writeHead(200, {'Content-Type': 'application/json'});
      response.end('[]');
      return;
    }
    if (request.method === 'POST' && path.endsWith('/publish')) {
      response.writeHead(200, {'Content-Type': 'application/json'});
      response.end('{}');
      return;
    }
    if (request.method === 'POST') {
      const record = {path, body: JSON.parse(bodyText || '{}'), id: `00000000-0000-4000-8000-${String(nextId++).padStart(12, '0')}`};
      created.push(record);
      response.writeHead(201, {'Content-Type': 'application/json'});
      response.end(JSON.stringify({id: record.id, etag: '"local-test-etag"'}));
      return;
    }

    response.writeHead(405);
    response.end();
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));

  try {
    const address = server.address();
    const child = spawn(process.execPath, ['scripts/demo-seed.mjs'], {
      cwd: new URL('../../', import.meta.url),
      env: {
        ...process.env,
        NOVA_API_URL: `http://127.0.0.1:${address.port}`,
        NOVA_ADMIN_EMAIL: 'content-test@local.invalid',
        NOVA_ADMIN_PASSWORD: 'not-a-real-user-password'
      },
      stdio: ['ignore', 'pipe', 'pipe']
    });
    let stdout = '';
    let stderr = '';
    child.stdout.setEncoding('utf8').on('data', chunk => { stdout += chunk; });
    child.stderr.setEncoding('utf8').on('data', chunk => { stderr += chunk; });
    const exitCode = await new Promise((resolve, reject) => {
      child.once('error', reject);
      child.once('exit', resolve);
    });
    assert.equal(exitCode, 0, `${stdout}\n${stderr}`);

    const posts = created.filter(record => record.path === '/api/v1/admin/news');
    const articles = created.filter(record => record.path === '/api/v1/admin/wiki/articles');
    const locations = created.filter(record => record.path === '/api/v1/admin/knowledge/location');
    const npcs = created.filter(record => record.path === '/api/v1/admin/knowledge/npc');
    const catalogItems = created.filter(record => record.path === '/api/v1/admin/catalog/items');
    const communityEntries = created.filter(record => /^\/api\/v1\/admin\/community\//.test(record.path) && !record.path.includes('/registrations'));
    assert.equal(posts.length, 4);
    const swordArticle = articles.find(({body}) => body.slug === 'kiem-si-thung-lung-sao');
    const mapArticle = articles.find(({body}) => body.slug === 'ban-do-thung-lung-sao');
    assert.ok(swordArticle.body.markdown.includes('chưa xác nhận'), 'legacy class copy must not claim unverified game mechanics');
    assert.ok(mapArticle.body.markdown.includes('chưa xác nhận'), 'legacy map copy must identify its lore as unverified');
    assert.doesNotMatch(swordArticle.body.markdown, /né đòn|đòn kết liễu/i);
    assert.doesNotMatch(mapArticle.body.markdown, /phía bắc dẫn tới|phía đông là/i);
    assert.equal(locations.length, 5, 'the seed should retain the valley and add four linked locations');
    assert.equal(npcs.length, 4);
    assert.equal(catalogItems.length, 12);
    assert.equal(communityEntries.length, 10);
    assert.ok(catalogItems.every(({body}) => body.markdown.toLocaleLowerCase('vi-VN').includes('dữ liệu minh họa local')));
    assert.ok(communityEntries.every(({body}) => body.markdown.toLocaleLowerCase('vi-VN').includes('dữ liệu minh họa local')));
    assert.ok(communityEntries.filter(({body}) => body.kind === 'event').every(({body}) => body.registrationOpen === false));
    assert.ok(communityEntries.filter(({body}) => body.kind === 'event').every(({body}) => body.locationEntryId));
    assert.ok(communityEntries.filter(({body}) => body.kind === 'housing').every(({body}) => body.locationEntryId));
    assert.ok(locations.every(({body}) => body.latitude === null && body.longitude === null));
    assert.ok(npcs.every(({body}) => locations.some(location => location.id === body.locationEntryId)));
    assert.equal(created.filter(record => /^\/api\/v1\/admin\/(rewards|commerce)/.test(record.path)).length, 0,
      'the seed must not fabricate rewards or commerce records');
  } finally {
    await new Promise(resolve => server.close(resolve));
  }
});
