import assert from 'node:assert/strict';
import test from 'node:test';
import {readFileSync} from 'node:fs';
import contract from '../../contracts/openapi/wiki-v1.json' with {type:'json'};
import {validateCategory} from '../../apps/web/src/lib/wiki-category.ts';

const valid = {name:'Getting Started',slug:'getting-started',displayOrder:10,isActive:true};

test('category editor accepts valid input and rejects invalid name, slug, order',()=>{
 assert.deepEqual(validateCategory(valid),{});
 const invalid=validateCategory({...valid,name:' ',slug:'Bad--Slug',displayOrder:-1});
 assert.deepEqual(Object.keys(invalid).sort(),['displayOrder','name','slug']);
 assert.ok(validateCategory({...valid,displayOrder:10001}).displayOrder);
 assert.ok(validateCategory({...valid,isActive:'yes'}).isActive);
});

test('category PATCH and DELETE require Admin cookie and If-Match',()=>{
 const path=contract.paths['/api/v1/admin/wiki/categories/{id}'];
 assert.ok(path,'missing category detail endpoint');
 for(const method of ['patch','delete']){
  const operation=path[method];
  assert.ok(operation,`missing ${method}`);
  assert.deepEqual(operation.security,[{adminCookie:[]}]);
  assert.ok(operation.parameters?.some(p=>p.in==='header'&&p.name==='If-Match'&&p.required),`${method} needs If-Match`);
  assert.ok(operation.responses['412']&&operation.responses['428'],`${method} concurrency error contract`);
 }
});

test('category mutations are declared inside Admin authorization and CSRF endpoint group',()=>{
 const controller=readFileSync(new URL('../../backend/NovaHaven.Api/Controllers/AdminWikiCategoriesController.cs',import.meta.url),'utf8');
 const service=readFileSync(new URL('../../backend/NovaHaven.Application/Features/Wiki/Services/WikiCategoryService.cs',import.meta.url),'utf8');
 assert.match(controller,/\[Authorize\(Policy = "AdminOnly"\)\]/);
 assert.match(controller,/\[ValidateAntiForgeryToken\]/);
 assert.match(controller,/\[HttpPatch\("categories\/\{id:guid\}"\)\]/);
 assert.match(controller,/\[HttpDelete\("categories\/\{id:guid\}"\)\]/);
 assert.match(service,/WikiCategoryPolicy\.CheckUpdate/);
 assert.match(service,/WikiCategoryPolicy\.CheckDelete/);
});

test('Admin category controls require a version token and confirmation before deletion',()=>{
 const component=readFileSync(new URL('../../apps/web/app/admin/CategoryManager.tsx',import.meta.url),'utf8');
 assert.match(component,/validateCategory\(/);
 assert.match(component,/window\.confirm\(/);
 assert.match(component,/If-Match|etag/);
 assert.match(component,/DELETE/);
 assert.match(component,/isActive/);
});

test('category deletion catches only foreign-key races instead of hiding unrelated database errors',()=>{
 const filter=readFileSync(new URL('../../backend/NovaHaven.Api/Filters/PersistenceConflictExceptionFilter.cs',import.meta.url),'utf8');
 const postgres=readFileSync(new URL('../../backend/NovaHaven.Api/Infrastructure/PostgreSqlConflict.cs',import.meta.url),'utf8');
 const service=readFileSync(new URL('../../backend/NovaHaven.Application/Features/Wiki/Services/WikiCategoryService.cs',import.meta.url),'utf8');
 assert.match(filter,/DbUpdateException databaseException when PostgreSqlConflict\.IsForeignKeyViolation\(databaseException\)/);
 assert.match(postgres,/PostgresErrorCodes\.ForeignKeyViolation/);
 assert.match(filter,/if \(statusCode == 0\) return/);
 assert.match(service,/WikiCategoryPolicy\.CheckDelete/);
 assert.match(service,/TransactionIsolation\.Serializable/);
});
