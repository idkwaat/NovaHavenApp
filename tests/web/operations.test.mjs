import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import test from 'node:test';

const controller = await readFile(new URL('../../backend/NovaHaven.Api/Controllers/OperationsController.cs', import.meta.url), 'utf8');
const repository = await readFile(new URL('../../backend/NovaHaven.Infrastructure/Persistence/Repositories/Operations/EfOperationsReadRepository.cs', import.meta.url), 'utf8');
const service = await readFile(new URL('../../backend/NovaHaven.Application/Features/Operations/Services/OperationsService.cs', import.meta.url), 'utf8');
const openapi = JSON.parse(await readFile(new URL('../../contracts/openapi/wiki-v1.json', import.meta.url), 'utf8'));
const page = await readFile(new URL('../../apps/web/app/admin/OperationsManager.tsx', import.meta.url), 'utf8').catch(() => '');
const smoke = await readFile(new URL('../../scripts/all-systems-smoke.mjs', import.meta.url), 'utf8').catch(() => '');

test('operations diagnostics remains Admin-only and read-only', () => {
  assert.match(controller, /\[Route\("api\/v1\/admin\/operations"\)\]/);
  assert.match(controller, /\[Authorize\(Policy = "AdminOnly"\)\]/);
  assert.match(controller, /\[HttpGet\("diagnostics"\)\]/);
  assert.match(controller, /operationsService\.GetDiagnosticsAsync/);
  assert.doesNotMatch(controller, /NovaDbContext|SaveChangesAsync/);
  assert.match(repository, /GetAppliedMigrationsAsync/);
  assert.match(repository, /GetPendingMigrationsAsync/);
  assert.match(repository, /CanConnectAsync/);
  assert.doesNotMatch(repository, /SaveChangesAsync|EnsureKnownAsync/);
  assert.match(service, /repository\.GetDiagnosticsAsync/);
});

test('operations diagnostics is in the API contract and Admin UI', () => {
  assert.ok(openapi.paths['/api/v1/admin/operations/diagnostics']);
  assert.equal(openapi.paths['/api/v1/admin/operations/diagnostics'].get.security[0].adminCookie.length, 0);
  assert.match(page, /operations\/diagnostics/);
  assert.match(page, /pendingMigrations/);
  assert.match(page, /appliedMigrations/);
  assert.match(smoke, /admin\/operations\/diagnostics/);
});
