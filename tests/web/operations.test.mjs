import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import test from 'node:test';

const endpoint = await readFile(new URL('../../backend/NovaHaven.Api/Endpoints/OperationsEndpoints.cs', import.meta.url), 'utf8').catch(() => '');
const openapi = JSON.parse(await readFile(new URL('../../contracts/openapi/wiki-v1.json', import.meta.url), 'utf8'));
const page = await readFile(new URL('../../apps/web/app/admin/OperationsManager.tsx', import.meta.url), 'utf8').catch(() => '');
const smoke = await readFile(new URL('../../scripts/all-systems-smoke.mjs', import.meta.url), 'utf8').catch(() => '');

test('operations diagnostics remains Admin-only and read-only', () => {
  assert.match(endpoint, /\/api\/v1\/admin\/operations\/diagnostics/);
  assert.match(endpoint, /RequireAuthorization\("AdminOnly"\)/);
  assert.match(endpoint, /GetAppliedMigrationsAsync/);
  assert.match(endpoint, /GetPendingMigrationsAsync/);
  assert.doesNotMatch(endpoint, /SaveChangesAsync/);
  assert.doesNotMatch(endpoint, /EnsureKnownAsync/);
});

test('operations diagnostics is in the API contract and Admin UI', () => {
  assert.ok(openapi.paths['/api/v1/admin/operations/diagnostics']);
  assert.equal(openapi.paths['/api/v1/admin/operations/diagnostics'].get.security[0].adminCookie.length, 0);
  assert.match(page, /operations\/diagnostics/);
  assert.match(page, /pendingMigrations/);
  assert.match(page, /appliedMigrations/);
  assert.match(smoke, /admin\/operations\/diagnostics/);
});
