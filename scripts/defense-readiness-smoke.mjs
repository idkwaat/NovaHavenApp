import {resolve} from 'node:path';
import {pathToFileURL} from 'node:url';

const DEFAULT_API_ORIGIN = 'http://127.0.0.1:5080';
const DEFAULT_WEB_ORIGIN = 'http://127.0.0.1:3001';
const LOOPBACK_HOSTS = new Set(['localhost', '127.0.0.1', '[::1]']);

function localOrigin(value, label) {
  let url;
  try {
    url = new URL(value);
  } catch {
    throw new Error(`${label} must be a valid loopback origin.`);
  }
  if (!['http:', 'https:'].includes(url.protocol)
      || !LOOPBACK_HOSTS.has(url.hostname.toLowerCase())
      || url.username || url.password
      || url.pathname !== '/' || url.search || url.hash) {
    throw new Error(`${label} must be a loopback origin (localhost, 127.0.0.1 or ::1).`);
  }
  return url.origin;
}

function escapeHtmlText(value) {
  return String(value)
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;');
}

async function get(fetchImpl, origin, path, asText = false) {
  const url = new URL(path, origin);
  const response = await fetchImpl(url, {
    method: 'GET',
    headers: {Accept: asText ? 'text/html' : 'application/json'},
    signal: AbortSignal.timeout(10000),
  });
  if (!response.ok) {
    throw new Error(`GET ${url.pathname}: HTTP ${response.status}`);
  }
  return asText ? response.text() : response.json();
}

/**
 * Verifies published Wiki read parity without sending any mutation requests.
 * Both origins must be loopback URLs so a typo cannot target a remote service.
 */
export async function runDefenseReadinessSmoke({
  apiOrigin = DEFAULT_API_ORIGIN,
  webOrigin = DEFAULT_WEB_ORIGIN,
  fetchImpl = fetch,
} = {}) {
  // Validate both targets before making even the first request.
  const api = localOrigin(apiOrigin, 'API origin');
  const web = localOrigin(webOrigin, 'Web origin');

  const health = await get(fetchImpl, api, '/health');
  if (health.status !== 'ok') throw new Error('API health did not report status=ok.');

  const listing = await get(fetchImpl, api, '/api/v1/wiki/articles?page=1&pageSize=20');
  if (!Array.isArray(listing.items) || listing.items.length === 0) {
    throw new Error('Published Wiki API list is empty or malformed; seed local demo data first.');
  }

  const indexHtml = await get(fetchImpl, web, '/wiki', true);
  const verifiedSlugs = [];
  for (const item of listing.items) {
    if (!item.slug || !item.title || !Number.isInteger(item.revision)) {
      throw new Error('Published Wiki list item is missing slug, title or integer revision.');
    }

    const detail = await get(fetchImpl, api,
      `/api/v1/wiki/articles/${encodeURIComponent(item.slug)}`);
    for (const field of ['slug', 'title', 'summary', 'revision']) {
      if (detail[field] !== item[field]) {
        throw new Error(`API detail for ${item.slug} disagrees with the published list at ${field}.`);
      }
    }
    if (typeof detail.markdown !== 'string' || detail.markdown.length === 0) {
      throw new Error(`API detail for ${item.slug} has no published body.`);
    }

    const href = `/wiki/${encodeURIComponent(item.slug)}`;
    if (!indexHtml.includes(`href="${href}"`)
        || !indexHtml.includes(escapeHtmlText(item.title))) {
      throw new Error(`Web Wiki listing is missing the published article ${item.slug}.`);
    }

    const pageHtml = await get(fetchImpl, web, href, true);
    const expectedHeading = `<h1>${escapeHtmlText(item.title)}</h1>`;
    if (!pageHtml.includes(expectedHeading)) {
      throw new Error(`Web detail for ${item.slug} does not render the API title.`);
    }
    const renderedRevision = pageHtml.match(/(?:\bRevision|Bản\s+biên\s+tập)\s*(?:<!--.*?-->\s*)*(\d+)\b/i);
    if (!renderedRevision || Number(renderedRevision[1]) !== item.revision) {
      throw new Error(
        `Web detail for ${item.slug} renders revision ${renderedRevision?.[1] ?? 'missing'}; API list renders revision ${item.revision}.`,
      );
    }
    if (!pageHtml.includes(escapeHtmlText(item.summary))) {
      throw new Error(`Web detail for ${item.slug} does not render the published summary.`);
    }
    verifiedSlugs.push(item.slug);
  }

  return {articleCount: verifiedSlugs.length, verifiedSlugs};
}

if (process.argv[1]
    && pathToFileURL(resolve(process.argv[1])).href === import.meta.url) {
  runDefenseReadinessSmoke({
    apiOrigin: process.env.NOVA_API_URL ?? DEFAULT_API_ORIGIN,
    webOrigin: process.env.NOVA_WEB_URL ?? DEFAULT_WEB_ORIGIN,
  }).then(({articleCount, verifiedSlugs}) => {
    console.log(`DEFENSE READ-ONLY SMOKE PASS: ${articleCount} article(s) match API and web: ${verifiedSlugs.join(', ')}`);
  }).catch(error => {
    console.error(`DEFENSE READ-ONLY SMOKE FAIL: ${error.message}`);
    process.exitCode = 1;
  });
}
