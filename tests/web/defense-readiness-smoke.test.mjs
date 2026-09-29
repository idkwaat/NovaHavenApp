import assert from 'node:assert/strict';
import test from 'node:test';
import {runDefenseReadinessSmoke} from '../../scripts/defense-readiness-smoke.mjs';

const apiOrigin = 'http://127.0.0.1:5080';
const webOrigin = 'http://localhost:3001';
const summary = {
  id: 'a0000000-0000-4000-8000-000000000001',
  slug: 'valley-map',
  title: 'Bản đồ Thung lũng Sao',
  summary: 'Những địa danh đã xuất bản.',
  revision: 3,
};
const detail = {
  ...summary,
  markdown: '# Bản đồ\n\nNội dung đã xuất bản.',
  category: 'vung-dat',
  tags: ['huong-dan'],
  related: [],
  publishedAt: '2026-09-28T00:00:00Z',
};

function response(body, status = 200) {
  return {
    ok: status >= 200 && status < 300,
    status,
    async json() { return body; },
    async text() { return typeof body === 'string' ? body : JSON.stringify(body); },
  };
}

function fakeFetch({pageRevision = 3, detailRevision = 3} = {}) {
  const requests = [];
  const fetchImpl = async (input, options = {}) => {
    const url = new URL(input);
    requests.push({url, method: options.method ?? 'GET'});
    assert.equal(options.method ?? 'GET', 'GET');
    if (url.pathname === '/health') return response({status: 'ok'});
    if (url.pathname === '/api/v1/wiki/articles') {
      return response({items: [summary], page: 1, pageSize: 20, total: 1});
    }
    if (url.pathname === `/api/v1/wiki/articles/${summary.slug}`) {
      return response({...detail, revision: detailRevision});
    }
    if (url.pathname === '/wiki') {
      return response(`<a href="/wiki/${summary.slug}">${summary.title}</a>`);
    }
    if (url.pathname === `/wiki/${summary.slug}`) {
      return response(`<h1>${summary.title}</h1><p>Bản biên tập <!-- -->${pageRevision}</p><p>${summary.summary}</p><p>Nội dung đã xuất bản.</p>`);
    }
    return response({error: `Unexpected request: ${url}`}, 404);
  };
  return {fetchImpl, requests};
}

test('rejects non-local origins before making any request', async () => {
  const {fetchImpl, requests} = fakeFetch();
  await assert.rejects(
    runDefenseReadinessSmoke({apiOrigin, webOrigin: 'https://example.com', fetchImpl}),
    /loopback/i,
  );
  assert.equal(requests.length, 0);
});

test('matches a published API article to the server-rendered Wiki page', async () => {
  const {fetchImpl, requests} = fakeFetch();
  const result = await runDefenseReadinessSmoke({apiOrigin, webOrigin, fetchImpl});

  assert.equal(result.articleCount, 1);
  assert.deepEqual(result.verifiedSlugs, ['valley-map']);
  assert.ok(requests.length >= 4);
  assert.ok(requests.every(({method}) => method === 'GET'));
});

test('fails when the website is still rendering an older revision', async () => {
  const {fetchImpl} = fakeFetch({pageRevision: 2});
  await assert.rejects(
    runDefenseReadinessSmoke({apiOrigin, webOrigin, fetchImpl}),
    /revision 2.*revision 3/i,
  );
});

test('fails when the public detail and published list disagree', async () => {
  const {fetchImpl} = fakeFetch({detailRevision: 4});
  await assert.rejects(
    runDefenseReadinessSmoke({apiOrigin, webOrigin, fetchImpl}),
    /API detail.*revision/i,
  );
});
