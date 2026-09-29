import assert from 'node:assert/strict';
import test from 'node:test';

const webOrigin = process.env.NOVA_HOME_E2E_ORIGIN;
const apiOrigin = process.env.NOVA_HOME_E2E_API_ORIGIN;
const integrationOptions = {skip: !webOrigin || !apiOrigin};

async function getJson(origin, path) {
  const response = await fetch(new URL(path, origin));
  assert.equal(response.status, 200, `${path} should be available`);
  return response.json();
}

async function readHomepage() {
  const response = await fetch(new URL('/', webOrigin));
  assert.equal(response.status, 200, 'homepage should be available');
  return response.text();
}

test('homepage shows links to the current published Wiki articles', integrationOptions, async () => {
  const [page, categories, html] = await Promise.all([
    getJson(apiOrigin, '/api/v1/wiki/articles?page=1&pageSize=20'),
    getJson(apiOrigin, '/api/v1/wiki/categories'),
    readHomepage(),
  ]);

  assert.ok(page.items.length > 0, 'fixture API should have published Wiki content');
  const visibleArticles = page.items.slice(0, 3);
  for (const article of visibleArticles) {
    assert.ok(html.includes(article.title), `homepage should show ${article.title}`);
    assert.ok(html.includes(`href="/wiki/${article.slug}"`), `homepage should link to ${article.slug}`);
  }
  const renderedCategoryLabels = [...html.matchAll(/class="home-card-meta"><span>([^<]+)<\/span>/g)].map(match => match[1]);
  const firstCategory = categories.find(category => category.slug === visibleArticles[0].category);
  assert.ok(firstCategory, 'published article category should resolve to its public category');
  assert.equal(renderedCategoryLabels[0], firstCategory.name, 'homepage metadata should use the Vietnamese category name, not its API slug');
});

test('homepage shows published Knowledge destinations from the World Atlas', integrationOptions, async () => {
  const [page, html] = await Promise.all([
    getJson(apiOrigin, '/api/v1/knowledge?page=1&pageSize=20'),
    readHomepage(),
  ]);

  assert.ok(page.items.length > 0, 'fixture API should have published Knowledge content');
  for (const entry of page.items.slice(0, 3)) {
    assert.ok(html.includes(entry.name), `homepage should show ${entry.name}`);
    assert.ok(html.includes(`href="/knowledge/${entry.kind}/${entry.slug}"`), `homepage should link to ${entry.slug}`);
  }
});

test('homepage features current published News instead of placeholder copy', integrationOptions, async () => {
  const [page, html] = await Promise.all([
    getJson(apiOrigin, '/api/v1/news?page=1&pageSize=20'),
    readHomepage(),
  ]);

  assert.ok(page.items.length > 0, 'fixture API should have published News content');
  assert.ok(html.includes(page.items[0].title), 'homepage should show the newest published News title');
  assert.ok(html.includes(`href="/news/${page.items[0].slug}"`), 'homepage should link to the newest News item');
});

test('homepage directory reports Catalog and Community publication state honestly', integrationOptions, async () => {
  const [catalog, community, html] = await Promise.all([
    getJson(apiOrigin, '/api/v1/catalog/items?page=1&pageSize=20'),
    getJson(apiOrigin, '/api/v1/community?page=1&pageSize=20'),
    readHomepage(),
  ]);

  assert.ok(html.includes('href="/catalog"'), 'homepage should provide a Catalog destination');
  assert.ok(html.includes('href="/community"'), 'homepage should provide a Community destination');
  if (catalog.total === 0 || community.total === 0) {
    assert.ok(html.includes('Chưa có nội dung công khai'), 'an empty module should not be presented as populated');
  }
  if (catalog.total > 0) assert.ok(html.includes(`${catalog.total} mục đã xuất bản`));
  if (community.total > 0) assert.ok(html.includes(`${community.total} mục đã xuất bản`));
});
