import assert from 'node:assert/strict';
import test from 'node:test';
import contract from '../../contracts/openapi/wiki-v1.json' with {type:'json'};

test('all declared Admin write endpoints require cookie authentication',()=>{
 for(const [path,methods] of Object.entries(contract.paths)){
  if(!path.startsWith('/api/v1/admin/'))continue;
  for(const [verb,operation] of Object.entries(methods)){
   if(!['post','patch','put','delete'].includes(verb))continue;
   assert.deepEqual(operation.security,[{adminCookie:[]}],`${verb.toUpperCase()} ${path}`);
  }
 }
});
test('all declared edit/publish/restore mutations require If-Match',()=>{
 for(const [path,methods] of Object.entries(contract.paths)){
  if(!path.includes('/articles/{id}'))continue;
  for(const [verb,operation] of Object.entries(methods)){
   if(!['patch','post'].includes(verb))continue;
   assert.ok(operation.parameters?.some(x=>x.name==='If-Match'&&x.required),`${verb.toUpperCase()} ${path}`);
  }
 }
});
