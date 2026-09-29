import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync } from 'node:fs';
import contract from '../../contracts/openapi/wiki-v1.json' with { type: 'json' };

const conflictFilter = readFileSync(new URL('../../backend/NovaHaven.Api/Filters/PersistenceConflictExceptionFilter.cs', import.meta.url), 'utf8');
const categoryService = readFileSync(new URL('../../backend/NovaHaven.Application/Features/Wiki/Services/WikiCategoryService.cs', import.meta.url), 'utf8');
const articleService = readFileSync(new URL('../../backend/NovaHaven.Application/Features/Wiki/Services/WikiArticleService.cs', import.meta.url), 'utf8');
const articleController = readFileSync(new URL('../../backend/NovaHaven.Api/Controllers/AdminWikiArticlesController.cs', import.meta.url), 'utf8');
const unitOfWork = readFileSync(new URL('../../backend/NovaHaven.Infrastructure/Persistence/UnitOfWork/EfUnitOfWork.cs', import.meta.url), 'utf8');
const guard = () => readFileSync(new URL('../../backend/NovaHaven.Api/Infrastructure/SqlServerConflict.cs', import.meta.url), 'utf8');
for (const [name, start, end, reads] of [
  ['article create', 'public async Task<ApplicationResult<WikiArticleWriteResult>> CreateAsync(', 'public Task<ApplicationResult<WikiArticleWriteResult>> UpdateAsync(', /IsActiveCategoryAsync|AreActiveTagsAsync|AreExistingMediaAsync/],
  ['article edit', 'public Task<ApplicationResult<WikiArticleWriteResult>> UpdateAsync(', 'public Task<ApplicationResult<WikiArticlePublishedResult>> PublishAsync(', /FindForUpdateAsync|IsActiveCategoryAsync|AreActiveTagsAsync|AreExistingMediaAsync/],
  ['article publish', 'public Task<ApplicationResult<WikiArticlePublishedResult>> PublishAsync(', 'public Task<ApplicationResult<bool>> UnpublishAsync(', /FindForUpdateAsync|GetDraftTagIdsAsync|GetDraftMediaIdsAsync/],
  ['article restore', 'public Task<ApplicationResult<WikiArticleWriteResult>> RestoreAsync(', 'private async Task<ApplicationResult<bool>> UnpublishInTransactionAsync(', /FindForUpdateAsync|FindRevisionAsync|GetRevisionTagIdsAsync|GetRevisionMediaIdsAsync/]
]) {
  test(`${name} guards classification read and write with serializable Application transaction`, () => {
    const begin = articleService.indexOf(start);
    const stop = articleService.indexOf(end, begin + start.length);
    assert.ok(begin !== -1 && stop !== -1, `missing ${name} use case`);
    const body = articleService.slice(begin, stop);
    assert.match(body, /ExecuteInTransactionAsync/);
    assert.match(body, reads);
    assert.match(body, /TransactionIsolation\.Serializable/);
    assert.match(body, /unitOfWork\.SaveChangesAsync/);
    assert.match(body, /result => result\.IsSuccess/);
    assert.match(unitOfWork, /if \(shouldCommit\(result\)\)[\s\S]*CommitAsync/);
    assert.match(unitOfWork, /RollbackAsync/);
  });
}

for (const [name, start, end, reads] of [
  ['category edit', 'public Task<ApplicationResult<WikiCategoryAdminResult>> UpdateAsync(', 'public Task<ApplicationResult<bool>> DeleteAsync(', /FindForUpdateAsync|HasArticleReferencesAsync|HasRevisionReferencesAsync/],
  ['category delete', 'public Task<ApplicationResult<bool>> DeleteAsync(', 'private static WikiCategoryAdminResult ToResult(', /FindForUpdateAsync|HasArticleReferencesAsync|HasRevisionReferencesAsync/]
]) {
  test(`${name} guards classification reads and writes with the Application unit of work`, () => {
    const begin = categoryService.indexOf(start);
    const stop = categoryService.indexOf(end, begin + start.length);
    assert.ok(begin !== -1 && stop !== -1, `missing ${name} use case`);
    const body = categoryService.slice(begin, stop);
    assert.match(body, /ExecuteInTransactionAsync/);
    assert.match(body, reads);
    assert.match(body, /TransactionIsolation\.Serializable/);
    assert.match(body, /unitOfWork\.SaveChangesAsync/);
    assert.match(body, /result => result\.IsSuccess/);
    assert.match(unitOfWork, /if \(shouldCommit\(result\)\)[\s\S]*CommitAsync/);
    assert.match(unitOfWork, /RollbackAsync/);
  });
}

test('Admin mutation filter returns 409 only for recognized SQL Server 1205 errors', () => {
  assert.match(conflictFilter, /SqlServerConflict\.IsDeadlock\(exception\)/);
  assert.match(conflictFilter, /StatusCodes\.Status409Conflict/);
  assert.match(guard(), /SqlException\s*\{\s*Number:\s*1205\s*\}/);
  assert.match(guard(), /\.InnerException/);
});

test('article edit and restore never send a new ETag before commit succeeds', () => {
  for (const method of ['Update', 'Restore']) {
    const begin = articleController.indexOf(`public async Task<IActionResult> ${method}(`);
    const stop = articleController.indexOf('\n    [Http', begin + 1);
    const body = articleController.slice(begin, stop === -1 ? articleController.length : stop);
    const awaitedUseCase = body.indexOf('await wikiArticleService.');
    const successCheck = body.indexOf('if (!result.IsSuccess)');
    const header = body.indexOf('Response.Headers.ETag = etag');
    assert.ok(awaitedUseCase !== -1 && successCheck > awaitedUseCase && header > successCheck,
      `${method}: ETag can only be emitted after the transactional service succeeds`);
  }
});

test('real SQL smoke includes concurrent category deactivation and article creation', () => {
  const smoke = readFileSync(new URL('../../scripts/wiki-smoke.mjs', import.meta.url), 'utf8');
  assert.match(smoke, /Promise\.all\(/);
  assert.match(smoke, /Concurrent category deactivation and article creation/);
  assert.match(smoke, /cannot both succeed/);
});

test('all transactional mutations document SQL deadlock HTTP 409 in OpenAPI', () => {
  for (const [path,method] of [
    ['/api/v1/admin/wiki/categories/{id}','patch'],
    ['/api/v1/admin/wiki/categories/{id}','delete'],
    ['/api/v1/admin/wiki/articles','post'],
    ['/api/v1/admin/wiki/articles/{id}','patch'],
    ['/api/v1/admin/wiki/articles/{id}/publish','post'],
    ['/api/v1/admin/wiki/articles/{id}/revisions/{revisionId}/restore','post']
  ]) {
    const operation = contract.paths[path]?.[method];
    assert.ok(operation?.responses?.['409'], `${method.toUpperCase()} ${path} must document concurrency conflict`);
  }
});
