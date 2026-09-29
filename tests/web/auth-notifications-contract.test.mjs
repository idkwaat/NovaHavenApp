import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';

const contract = JSON.parse(await readFile(new URL('../../contracts/openapi/wiki-v1.json', import.meta.url), 'utf8'));
const [accountClient, accountController, mobileAccountApi] = await Promise.all([
  readFile(new URL('../../apps/web/app/account/AccountClient.tsx', import.meta.url), 'utf8'),
  readFile(new URL('../../backend/NovaHaven.Api/Controllers/AuthController.cs', import.meta.url), 'utf8'),
  readFile(new URL('../../apps/mobile/lib/account_api.dart', import.meta.url), 'utf8'),
]);

test('the shared API contract describes local player account flows without email', () => {
  for (const route of [
    '/api/v1/auth/register',
    '/api/v1/auth/login',
    '/api/v1/auth/me',
    '/api/v1/auth/logout',
  ]) assert.ok(contract.paths[route], `missing ${route}`);
  assert.equal(contract.paths['/api/v1/auth/register'].post.responses['201'].description.includes('no email'), true);
  assert.equal(Object.hasOwn(contract.paths, '/api/v1/auth/confirm-email'), false);
  assert.equal(Object.hasOwn(contract.paths, '/api/v1/auth/resend-confirmation'), false);
  for (const source of [accountClient, accountController, mobileAccountApi]) {
    assert.equal(source.includes('confirm-email'), false);
    assert.equal(source.includes('resend-confirmation'), false);
    assert.equal(source.includes('dev/mailbox'), false);
  }
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
