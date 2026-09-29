import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';

const contract = JSON.parse(await readFile(new URL('../../contracts/openapi/wiki-v1.json', import.meta.url), 'utf8'));

test('the shared API contract describes player account and confirmation flows', () => {
  for (const route of [
    '/api/v1/auth/register',
    '/api/v1/auth/confirm-email',
    '/api/v1/auth/resend-confirmation',
    '/api/v1/auth/me',
  ]) assert.ok(contract.paths[route], `missing ${route}`);
  assert.deepEqual(contract.paths['/api/v1/auth/me'].get.security, [{userCookie: []}]);
  assert.deepEqual(contract.paths['/api/v1/auth/logout'].post.security, [{userCookie: []}]);
});

test('the shared API contract scopes notification inbox and push subscriptions to signed-in users', () => {
  for (const route of [
    '/api/v1/notifications',
    '/api/v1/notifications/{id}/read',
    '/api/v1/notifications/read-all',
    '/api/v1/notifications/push/config',
    '/api/v1/notifications/push/subscriptions',
    '/api/v1/admin/notifications',
  ]) assert.ok(contract.paths[route], `missing ${route}`);

  assert.deepEqual(contract.paths['/api/v1/notifications'].get.security, [{userCookie: []}]);
  assert.deepEqual(contract.paths['/api/v1/admin/notifications'].post.security, [{adminCookie: []}]);
});
