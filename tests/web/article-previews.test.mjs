import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import test from 'node:test';
import contract from '../../contracts/openapi/wiki-v1.json' with {type:'json'};
import {
  articlePreviewFor,
  extractMarkdownPreview,
  isAllowedPreviewUrl,
  editorialPreviewAssets,
} from '../../apps/web/src/lib/article-previews.ts';

const root = new URL('../../', import.meta.url);
const read = path => readFile(new URL(path, root), 'utf8');

test('published article previews prefer attached public media and reject unsafe URLs', () => {
  const media = articlePreviewFor({
    collection: 'wiki',
    slug: 'ban-do-thung-lung-sao',
    title: 'Bản đồ Thung lũng Sao',
    imageUrl: '/api/v1/wiki/media/75d110c0-7286-4b3b-bc49-6a8035277af3',
    imageAlt: 'Bản đồ đường mòn quanh thung lũng',
  });

  assert.equal(media.src, '/api/v1/wiki/media/75d110c0-7286-4b3b-bc49-6a8035277af3');
  assert.equal(media.alt, 'Bản đồ đường mòn quanh thung lũng');
  assert.equal(isAllowedPreviewUrl('javascript:alert(1)'), false);
  assert.equal(isAllowedPreviewUrl('//tracking.example/pixel.jpg'), false);

  const fallback = articlePreviewFor({
    collection: 'wiki', slug: 'ban-do-thung-lung-sao', title: 'Bản đồ Thung lũng Sao',
    imageUrl: 'https://tracking.example/pixel.jpg', imageAlt: 'tracking pixel',
  });
  assert.notEqual(fallback.src, 'https://tracking.example/pixel.jpg');
  assert.ok(fallback.credit);
});

test('News can use its first safe Markdown image as a preview', () => {
  const image = extractMarkdownPreview(
    '![Bình minh trên rừng](https://upload.wikimedia.org/wikipedia/commons/3/35/forest.jpg)\n\n## Chuyến đi',
  );
  assert.deepEqual(image, {
    src: 'https://upload.wikimedia.org/wikipedia/commons/3/35/forest.jpg',
    alt: 'Bình minh trên rừng',
  });
  assert.equal(extractMarkdownPreview('![Ảnh xấu](javascript:alert(1))'), null);
});

test('real, credited fallback photography is stable and varied across seeded articles', async () => {
  assert.ok(editorialPreviewAssets.length >= 3);
  for (const image of editorialPreviewAssets) {
    assert.ok(isAllowedPreviewUrl(image.src));
    assert.ok(image.alt.trim());
    assert.ok(image.credit.trim());
    assert.match(image.sourceUrl, /^https:\/\//);
    assert.match(image.licenseUrl, /^https:\/\//);
  }

  const news = JSON.parse(await read('scripts/demo-news-content.json')).posts;
  const previews = news.map(post => articlePreviewFor({collection:'news', slug:post.slug, title:post.title}).src);
  assert.ok(new Set(previews).size > 1, 'different News posts should not all reuse one cover');
  assert.equal(previews[0], articlePreviewFor({collection:'news', slug:news[0].slug, title:news[0].title}).src);
});

test('public list contracts expose published media and News markdown for image previews', () => {
  const wikiPageRef=contract.paths['/api/v1/wiki/articles'].get.responses['200'].content['application/json'].schema.$ref;
  assert.equal(wikiPageRef,'#/components/schemas/WikiArticlePage');
  const wikiSummary=contract.components.schemas.WikiArticleSummary;
  assert.ok(wikiSummary.required.includes('previewImageUrl'));
  assert.ok(wikiSummary.required.includes('previewImageAlt'));
  assert.deepEqual(wikiSummary.properties.previewImageUrl.type,['string','null']);

  const newsPageRef=contract.paths['/api/v1/news'].get.responses['200'].content['application/json'].schema.$ref;
  assert.equal(newsPageRef,'#/components/schemas/NewsPage');
  assert.ok(contract.components.schemas.NewsSummary.required.includes('markdown'));

  const knowledgePageRef=contract.paths['/api/v1/knowledge/{kind}'].get.responses['200'].content['application/json'].schema.$ref;
  assert.equal(knowledgePageRef,'#/components/schemas/KnowledgePage');
  assert.ok(contract.components.schemas.KnowledgeSummary.required.includes('previewImageUrl'));
});
