/**
 * Idempotent local demo content for Nova Haven.
 *
 * Run only against a disposable local API:
 * NOVA_API_URL=http://127.0.0.1:5080 \
 * NOVA_ADMIN_EMAIL=admin@local.test \
 * NOVA_ADMIN_PASSWORD=... npm run seed:demo
 */
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';

const base = process.env.NOVA_API_URL;
const email = process.env.NOVA_ADMIN_EMAIL;
const password = process.env.NOVA_ADMIN_PASSWORD;
if (!base || !email || !password) throw Error('NOVA_API_URL, NOVA_ADMIN_EMAIL and NOVA_ADMIN_PASSWORD are required.');
if (!/^https?:\/\/(localhost|127\.0\.0\.1)(:\d+)?\/?$/.test(base))
  throw Error('Không chạy demo seed trên API từ xa. Chỉ localhost/127.0.0.1 được phép.');

const cookies = new Map();
let csrf = '';
const created = [];
const wikiContent = JSON.parse(await readFile(new URL('./demo-wiki-content.json', import.meta.url), 'utf8'));
const newsContent = JSON.parse(await readFile(new URL('./demo-news-content.json', import.meta.url), 'utf8'));
const knowledgeContent = JSON.parse(await readFile(new URL('./demo-knowledge-content.json', import.meta.url), 'utf8'));
const catalogContent = JSON.parse(await readFile(new URL('./demo-catalog-content.json', import.meta.url), 'utf8'));
const communityContent = JSON.parse(await readFile(new URL('./demo-community-content.json', import.meta.url), 'utf8'));

function captureCookies(response) {
  for (const entry of response.headers.getSetCookie?.() ?? []) {
    const first = entry.split(';', 1)[0];
    const separator = first.indexOf('=');
    if (separator > 0) cookies.set(first.slice(0, separator), first.slice(separator + 1));
  }
}

async function request(path, options = {}) {
  const method = options.method ?? 'GET';
  const response = await fetch(new URL(path, base), {
    method,
    redirect: 'manual',
    headers: {
      Accept: 'application/json',
      Cookie: [...cookies.entries()].map(([key, value]) => `${key}=${value}`).join('; '),
      ...(method !== 'GET' ? {'X-CSRF-TOKEN': csrf} : {}),
      ...(options.body ? {'Content-Type': 'application/json'} : {}),
      ...(options.etag ? {'If-Match': options.etag} : {})
    },
    body: options.body ? JSON.stringify(options.body) : undefined
  });
  captureCookies(response);
  return response;
}

async function responseJson(response, label) {
  const text = await response.text();
  if (!text) return null;
  try { return JSON.parse(text); }
  catch { throw Error(`${label}: API returned invalid JSON.`); }
}

async function expect(response, status, label) {
  if (response.status !== status) {
    const text = await response.text();
    throw Error(`${label}: expected HTTP ${status}, received ${response.status}: ${text.slice(0, 400)}`);
  }
  return response;
}

async function json(path, status, label) {
  return responseJson(await expect(await request(path), status, label), label);
}

async function refreshCsrf() {
  csrf = (await json('/api/v1/auth/csrf', 200, 'CSRF')).token;
  assert.ok(csrf, 'CSRF token must be returned');
}

function listOf(value) {
  return Array.isArray(value) ? value : value?.items ?? [];
}

function isPublished(state) {
  return state === 1 || String(state).toLowerCase() === 'published';
}

async function ensureSimple(path, body, label) {
  const existing = listOf(await json(path, 200, `${label} list`)).find(item => item.slug === body.slug);
  if (existing) return existing.id;
  const response = await expect(await request(path, {method: 'POST', body}), 201, `Create ${label}`);
  const value = await responseJson(response, `Create ${label}`);
  created.push(label);
  return value.id;
}

async function ensurePublished({listPath, detailPath, createPath, publishPath, body, label}) {
  const existing = listOf(await json(listPath, 200, `${label} list`)).find(item => item.slug === body.slug);
  const id = existing?.id ?? (await responseJson(
    await expect(await request(createPath, {method: 'POST', body}), 201, `Create ${label}`),
    `Create ${label}`
  )).id;
  if (!existing) created.push(label);

  const detailResponse = await expect(await request(`${detailPath}/${id}`), 200, `${label} detail`);
  const detail = await responseJson(detailResponse, `${label} detail`);
  if (!isPublished(detail.state)) {
    const etag = detailResponse.headers.get('etag') ?? detail.etag;
    assert.ok(etag, `${label} must expose an ETag before publish`);
    await expect(await request(`${publishPath}/${id}/publish`, {method: 'POST', etag}), 200, `Publish ${label}`);
    created.push(`${label} published`);
  }
  return id;
}

await refreshCsrf();
await expect(await request('/api/v1/auth/login', {method: 'POST', body: {email, password}}), 204, 'Admin login');
await refreshCsrf();

const startCategory = await ensureSimple('/api/v1/admin/wiki/categories', {name: 'Khởi hành', slug: 'khoi-hanh', displayOrder: 1}, 'category Khởi hành');
const classesCategory = await ensureSimple('/api/v1/admin/wiki/categories', {name: 'Lớp nhân vật', slug: 'lop-nhan-vat', displayOrder: 2}, 'category Lớp nhân vật');
const worldCategory = await ensureSimple('/api/v1/admin/wiki/categories', {name: 'Vùng đất', slug: 'vung-dat', displayOrder: 3}, 'category Vùng đất');
const categoryIdsBySlug = {
  'khoi-hanh': startCategory,
  'lop-nhan-vat': classesCategory,
  'vung-dat': worldCategory
};
const guideTag = await ensureSimple('/api/v1/admin/wiki/tags', {name: 'Hướng dẫn', slug: 'huong-dan'}, 'tag Hướng dẫn');
const loreTag = await ensureSimple('/api/v1/admin/wiki/tags', {name: 'Truyền thuyết', slug: 'truyen-thuyet'}, 'tag Truyền thuyết');
const discoveryTag = await ensureSimple('/api/v1/admin/wiki/tags', {name: 'Khám phá', slug: 'kham-pha'}, 'tag Khám phá');
const handbookTag = await ensureSimple('/api/v1/admin/wiki/tags', {name: 'Cẩm nang', slug: 'cam-nang'}, 'tag Cẩm nang');
const communityTag = await ensureSimple('/api/v1/admin/wiki/tags', {name: 'Cộng đồng', slug: 'cong-dong'}, 'tag Cộng đồng');
const journalTag = await ensureSimple('/api/v1/admin/wiki/tags', {name: 'Nhật ký', slug: 'nhat-ky'}, 'tag Nhật ký');
const tagIdsBySlug = {
  'kham-pha': discoveryTag,
  'cam-nang': handbookTag,
  'cong-dong': communityTag,
  'nhat-ky': journalTag,
  'huong-dan': guideTag,
  'truyen-thuyet': loreTag
};

await ensurePublished({
  listPath: '/api/v1/admin/wiki/articles', detailPath: '/api/v1/admin/wiki/articles',
  createPath: '/api/v1/admin/wiki/articles', publishPath: '/api/v1/admin/wiki/articles',
  label: 'article Chào mừng đến Nova Haven',
  body: {title: 'Chào mừng đến Nova Haven', slug: 'chao-mung-nova-haven', summary: 'Cẩm nang đầu tiên dành cho những người chơi đặt chân vào vùng đất mới.', categoryId: startCategory, tagIds: [guideTag], markdown: '# Chào mừng đến Nova Haven\n\nHãy bắt đầu bằng cách đọc bản đồ, chọn hướng phiêu lưu và ghi nhớ những địa danh quan trọng. Wiki này là nơi lưu giữ các câu chuyện, hướng dẫn và khám phá của cộng đồng.'}
});
await ensurePublished({
  listPath: '/api/v1/admin/wiki/articles', detailPath: '/api/v1/admin/wiki/articles',
  createPath: '/api/v1/admin/wiki/articles', publishPath: '/api/v1/admin/wiki/articles',
  label: 'article Kiếm sĩ Thung lũng Sao',
  body: {title: 'Kiếm sĩ Thung lũng Sao', slug: 'kiem-si-thung-lung-sao', summary: 'Chân dung kiếm sĩ trong lore Nova Haven; hồ sơ này không xác nhận lớp nhân vật hay kỹ năng trong máy chủ.', categoryId: classesCategory, tagIds: [guideTag, loreTag], markdown: '# Kiếm sĩ Thung lũng Sao\n\nĐây là chân dung nhân vật thuộc lore do Nova Haven biên soạn, dùng để mở đầu nhóm bài về những người lữ hành. Nội dung không xác nhận máy chủ online có lớp Kiếm sĩ, bộ kỹ năng hay lối chơi cụ thể nào.\n\n## Hình dung nhân vật\n\nTrong câu chuyện, người kiếm sĩ kiên nhẫn quan sát trước khi chọn cách giúp đỡ đoàn. Chi tiết này là nét tính cách của nhân vật hư cấu, không phải hướng dẫn chiến đấu.\n\n## Câu hỏi dành cho người khám phá\n\nKhi Wiki nhận được dữ liệu có nguồn đáng tin cậy về các lớp trong máy chủ, người biên tập có thể bổ sung bài hướng dẫn riêng và ghi rõ phiên bản, thời điểm kiểm tra. Cho tới lúc đó, trang này chỉ lưu giữ lore và không nên được dùng để suy ra chỉ số hoặc cơ chế gameplay.\n\n## Trạng thái\n\nNhân vật và mô tả trên là nội dung lore biên soạn, chưa xác nhận tương ứng với lớp nhân vật trong game.'}
});
await ensurePublished({
  listPath: '/api/v1/admin/wiki/articles', detailPath: '/api/v1/admin/wiki/articles',
  createPath: '/api/v1/admin/wiki/articles', publishPath: '/api/v1/admin/wiki/articles',
  label: 'article Bản đồ Thung lũng Sao',
  body: {title: 'Bản đồ Thung lũng Sao', slug: 'ban-do-thung-lung-sao', summary: 'Các địa danh trong lore Nova Haven; chưa phải bản đồ vị trí đã được xác minh trong máy chủ.', categoryId: worldCategory, tagIds: [loreTag], markdown: '# Thung lũng Sao\n\nThung lũng Sao là địa danh trong lore biên soạn cho Nova Haven. Những câu chuyện nhắc tới rừng, bến cảng và các chuyến đi, nhưng chưa xác lập thứ tự địa lý hoặc đường đi giữa chúng.\n\n## Đọc bản đồ như một câu chuyện\n\nCác tên gọi ở đây giúp kết nối những mẩu lore và nhật ký thám hiểm. Chúng không phải chỉ dẫn để tìm địa điểm trong game.\n\n## Trạng thái xác minh\n\nBài viết chưa có tọa độ, ảnh chụp hay nguồn xác nhận địa danh trên máy chủ online. Đây là lore chưa xác nhận với dữ liệu server; không suy ra hướng đi từ mô tả văn học. Bản đồ gameplay chỉ nên được bổ sung khi có bằng chứng và thời điểm kiểm tra.'}
});

for (const article of wikiContent.articles) {
  await ensurePublished({
    listPath: '/api/v1/admin/wiki/articles', detailPath: '/api/v1/admin/wiki/articles',
    createPath: '/api/v1/admin/wiki/articles', publishPath: '/api/v1/admin/wiki/articles',
    label: `article ${article.title}`,
    body: {
      title: article.title,
      slug: article.slug,
      summary: article.summary,
      categoryId: categoryIdsBySlug[article.categorySlug],
      tagIds: article.tagSlugs.map(slug => tagIdsBySlug[slug]),
      markdown: article.markdown
    }
  });
}

for (const post of newsContent.posts) {
  await ensurePublished({
    listPath: '/api/v1/admin/news', detailPath: '/api/v1/admin/news',
    createPath: '/api/v1/admin/news', publishPath: '/api/v1/admin/news',
    label: `news ${post.title}`,
    body: {title: post.title, slug: post.slug, summary: post.summary, markdown: post.markdown}
  });
}

const valley = await ensurePublished({
  listPath: '/api/v1/admin/knowledge/location', detailPath: '/api/v1/admin/knowledge/location',
  createPath: '/api/v1/admin/knowledge/location', publishPath: '/api/v1/admin/knowledge/location',
  label: 'knowledge Thung lũng Sao',
  body: {name: 'Thung lũng Sao', slug: 'thung-lung-sao', summary: 'Vùng đất trong lore Nova Haven nơi các nhà thám hiểm bắt đầu ghi chép hành trình.', markdown: '# Thung lũng Sao\n\nĐịa danh khởi đầu trong lore biên soạn cho Nova Haven. Tọa độ và vị trí trong máy chủ online chưa được xác nhận.', kind: 'location', region: 'Miền Đông', locationType: 'Thung lũng', latitude: null, longitude: null, mapImageUrl: null}
});

const locationIdsBySlug = {'thung-lung-sao': valley};
for (const location of knowledgeContent.locations) {
  locationIdsBySlug[location.slug] = await ensurePublished({
    listPath: '/api/v1/admin/knowledge/location', detailPath: '/api/v1/admin/knowledge/location',
    createPath: '/api/v1/admin/knowledge/location', publishPath: '/api/v1/admin/knowledge/location',
    label: `knowledge location ${location.name}`,
    body: {
      name: location.name,
      slug: location.slug,
      summary: location.summary,
      markdown: location.markdown,
      kind: 'location',
      region: location.region,
      locationType: location.locationType,
      latitude: location.latitude,
      longitude: location.longitude,
      mapImageUrl: null
    }
  });
}

for (const npc of knowledgeContent.npcs) {
  const locationEntryId = locationIdsBySlug[npc.locationSlug];
  assert.ok(locationEntryId, `${npc.slug} references a location that must be seeded first`);
  await ensurePublished({
    listPath: '/api/v1/admin/knowledge/npc', detailPath: '/api/v1/admin/knowledge/npc',
    createPath: '/api/v1/admin/knowledge/npc', publishPath: '/api/v1/admin/knowledge/npc',
    label: `knowledge npc ${npc.name}`,
    body: {
      name: npc.name,
      slug: npc.slug,
      summary: npc.summary,
      markdown: npc.markdown,
      kind: 'npc',
      role: npc.role,
      locationEntryId,
      portraitUrl: null
    }
  });
}

for (const item of catalogContent.items) {
  await ensurePublished({
    listPath: '/api/v1/admin/catalog/items', detailPath: '/api/v1/admin/catalog/items',
    createPath: '/api/v1/admin/catalog/items', publishPath: '/api/v1/admin/catalog/items',
    label: `catalog ${item.name}`,
    body: {
      name: item.name,
      slug: item.slug,
      summary: item.summary,
      markdown: `${item.markdown}\n\nNguồn tham khảo: [${item.sourceTitle}](${item.sourceUrl})`,
      kind: item.kind
    }
  });
}

for (const entry of communityContent.entries) {
  const locationEntryId = entry.locationSlug ? locationIdsBySlug[entry.locationSlug] : null;
  assert.ok(!entry.locationSlug || locationEntryId, `${entry.slug} references a published location that must be seeded first`);
  const startsAt = entry.kind === 'event'
    ? new Date(Date.now() + entry.startsInDays * 24 * 60 * 60 * 1000).toISOString()
    : null;
  const endsAt = startsAt
    ? new Date(new Date(startsAt).getTime() + entry.durationDays * 24 * 60 * 60 * 1000).toISOString()
    : null;

  await ensurePublished({
    listPath: `/api/v1/admin/community/${entry.kind}`,
    detailPath: `/api/v1/admin/community/${entry.kind}`,
    createPath: `/api/v1/admin/community/${entry.kind}`,
    publishPath: `/api/v1/admin/community/${entry.kind}`,
    label: `community ${entry.kind} ${entry.name}`,
    body: {
      name: entry.name,
      slug: entry.slug,
      summary: entry.summary,
      markdown: entry.markdown,
      kind: entry.kind,
      startsAt,
      endsAt,
      locationEntryId,
      capacity: entry.capacity ?? null,
      registrationOpen: entry.registrationOpen ?? false,
      motto: entry.motto ?? '',
      discordUrl: entry.discordUrl ?? '',
      handle: entry.handle ?? '',
      bio: entry.bio ?? '',
      avatarUrl: '',
      ownerDisplayName: entry.ownerDisplayName ?? '',
      galleryMarkdown: entry.galleryMarkdown ?? '',
      leaderboardCategory: entry.leaderboardCategory ?? '',
      seasonEntryId: null,
      rows: entry.rows ?? []
    }
  });
}

console.log(`DEMO SEED PASS: ${created.length ? created.join(', ') : 'all fixtures already existed'}`);
