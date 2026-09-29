/**
 * Non-mutating cross-module smoke test for the local API (only a rejected empty-cart POST).
 * Requires a running API with the latest SQL Server migrations and a disposable
 * development Admin. The Wiki mutation lifecycle is covered by wiki-smoke.mjs.
 * Run with: NOVA_API_URL=http://127.0.0.1:5080 NOVA_ADMIN_EMAIL=... NOVA_ADMIN_PASSWORD=... npm run smoke:all
 */
import assert from 'node:assert/strict';
import {randomUUID} from 'node:crypto';

const base = process.env.NOVA_API_URL;
const email = process.env.NOVA_ADMIN_EMAIL;
const password = process.env.NOVA_ADMIN_PASSWORD;
if (!base || !email || !password) throw Error('NOVA_API_URL, NOVA_ADMIN_EMAIL and NOVA_ADMIN_PASSWORD are required.');
if (!process.env.NOVA_ALLOW_REMOTE_SMOKE && !/^https?:\/\/(localhost|127\.0\.0\.1)(:\d+)?\/?$/.test(base))
  throw Error('Smoke tests are local-only unless NOVA_ALLOW_REMOTE_SMOKE is explicitly set.');

const cookies = new Map();
let csrf = '';
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
      ...(options.body ? {'Content-Type': 'application/json'} : {}),
      ...(method !== 'GET' ? {'X-CSRF-TOKEN': csrf} : {}),
      ...(options.headers ?? {})
    },
    body: options.body ? JSON.stringify(options.body) : undefined
  });
  captureCookies(response);
  return response;
}
async function expect(response, status, label) {
  if (response.status !== status) throw Error(`${label}: expected HTTP ${status}, received ${response.status}: ${(await response.text()).slice(0, 300)}`);
  return response;
}
async function json(path, status, label) {
  return (await expect(await request(path), status, label)).json();
}
function itemsOf(value, label) {
  const items = Array.isArray(value) ? value : value?.items;
  assert.ok(Array.isArray(items), `${label} must return an array or paged items array`);
  return items;
}
async function refreshCsrf() {
  csrf = (await json('/api/v1/auth/csrf', 200, 'CSRF')).token;
  assert.ok(csrf);
}

await expect(await request('/health'), 200, 'Health');

// Anonymous published-only/public status contracts.
const knowledge = itemsOf(await json('/api/v1/knowledge?pageSize=1', 200, 'Knowledge public'), 'Knowledge');
const community = itemsOf(await json('/api/v1/community?pageSize=1', 200, 'Community public'), 'Community');
const integrations = itemsOf(await json('/api/v1/integrations/status', 200, 'Integration status'), 'Integration status');
const rewards = itemsOf(await json('/api/v1/rewards?pageSize=1', 200, 'Rewards public'), 'Rewards');
const commerce = itemsOf(await json('/api/v1/commerce/offers?pageSize=1', 200, 'Commerce public'), 'Commerce');
assert.ok(integrations.length >= 4, 'Expected the four local integration capabilities.');
for (const item of rewards) assert.equal(item.externalAcknowledgementRequired, true);
for (const item of commerce) assert.equal(item.definitionOnly, true);

await refreshCsrf();
await expect(await request('/api/v1/auth/login', {method: 'POST', body: {email, password}}), 204, 'Admin login');
await refreshCsrf();

// Admin reads prove authorization and the local editorial stores are wired.
for (const [path, label] of [
  ['/api/v1/admin/knowledge/npc', 'Knowledge admin'],
  ['/api/v1/admin/community/event', 'Community admin'],
  ['/api/v1/admin/integrations/capabilities', 'Integration admin'],
  ['/api/v1/admin/rewards', 'Rewards admin'],
  ['/api/v1/admin/commerce/offers', 'Commerce admin'],
  ['/api/v1/admin/commerce/orders?page=1&pageSize=1', 'Commerce order history']
]) await expect(await request(path), 200, label);
const ordersBefore = await json('/api/v1/admin/commerce/orders?page=1&pageSize=1', 200, 'Order count before invalid cart');
await expect(await request('/api/v1/commerce/orders', {
  method: 'POST', body: {items: []}, headers: {'Idempotency-Key': randomUUID()}
}), 400, 'Reject empty local demo cart');
const ordersAfter = await json('/api/v1/admin/commerce/orders?page=1&pageSize=1', 200, 'Order count after invalid cart');
assert.equal(ordersAfter.total, ordersBefore.total, 'An invalid cart must not create a persisted order.');
const diagnostics = await json('/api/v1/admin/operations/diagnostics', 200, 'Operations diagnostics');
assert.equal(diagnostics.database.canConnect, true);
assert.equal(diagnostics.database.pendingMigrations, 0);
assert.ok(Number.isInteger(diagnostics.database.appliedMigrations) && diagnostics.database.appliedMigrations >= 10);
assert.ok(Array.isArray(diagnostics.integrations));

// Only a rejected empty cart is submitted; the smoke never creates a demo order.
console.log(`ALL-SYSTEMS SMOKE PASS: health / knowledge(${knowledge.length}) / community(${community.length}) / integrations(${integrations.length}) / rewards(${rewards.length}) / commerce(${commerce.length}) / diagnostics / admin reads / empty-cart rejected without an order`);
