/**
 * Real end-to-end smoke test. Requires a running API with a migrated SQL Server
 * and local test Admin. Never runs in production by default and never prints secrets.
 * Run: NOVA_API_URL=http://127.0.0.1:5080 NOVA_ADMIN_EMAIL=... NOVA_ADMIN_PASSWORD=... npm run smoke:wiki
 */
import assert from 'node:assert/strict';
import {randomUUID} from 'node:crypto';

const base=process.env.NOVA_API_URL;
const email=process.env.NOVA_ADMIN_EMAIL;
const password=process.env.NOVA_ADMIN_PASSWORD;
if(!base||!email||!password)throw Error('NOVA_API_URL, NOVA_ADMIN_EMAIL and NOVA_ADMIN_PASSWORD are required.');
if(!process.env.NOVA_ALLOW_REMOTE_SMOKE && !/^https?:\/\/(localhost|127\.0\.0\.1)(:\d+)?\/?$/.test(base))
  throw Error('Smoke tests mutate the database: only localhost is allowed without NOVA_ALLOW_REMOTE_SMOKE.');

const cookies=new Map();
let csrf='';
function captureCookies(response){
 for(const entry of response.headers.getSetCookie?.()??[]){
  const first=entry.split(';',1)[0];
  const separator=first.indexOf('=');
  if(separator>0)cookies.set(first.slice(0,separator),first.slice(separator+1));
 }
}
async function request(path,options={}){
 const response=await fetch(new URL(path,base),{
  method:options.method??'GET',redirect:'manual',
  headers:{Accept:'application/json',Cookie:[...cookies.entries()].map(([key,value])=>`${key}=${value}`).join('; '),
    ...(options.method&&options.method!=='GET'?{'X-CSRF-TOKEN':csrf}:{}),
    ...(options.body?{'Content-Type':'application/json'}:{}),
    ...(options.etag?{'If-Match':options.etag}:{}),
  },body:options.body?JSON.stringify(options.body):undefined,
 });
 captureCookies(response);
 return response;
}
async function expect(response,status,label){
 if(response.status!==status){
  const data=await response.text();
  throw Error(`${label}: expected HTTP ${status}, received ${response.status}: ${data.slice(0,300)}`);
 }
 return response;
}
async function json(response,status,label){return (await expect(response,status,label)).json();}
async function refreshCsrf(){csrf=(await json(await request('/api/v1/auth/csrf'),200,'CSRF')).token;assert.ok(csrf);}

await refreshCsrf();
await expect(await request('/api/v1/auth/login',{method:'POST',body:{email,password}}),204,'Admin login');
await refreshCsrf(); // CSRF tokens generated for anonymous identity cannot be reused after sign-in.
const suffix=randomUUID().slice(0,8);
const category=await json(await request('/api/v1/admin/wiki/categories',{method:'POST',body:{name:`Smoke ${suffix}`,slug:`smoke-${suffix}`,displayOrder:0}}),201,'Create category');
const categoryPath=`/api/v1/admin/wiki/categories/${category.id}`;
let categoryRead=await expect(await request(categoryPath),200,'Read category');
let categoryEtag=categoryRead.headers.get('etag');assert.ok(categoryEtag);
await expect(await request(categoryPath,{method:'PATCH',body:{name:category.name,slug:category.slug,displayOrder:1,isActive:true}}),428,'Category edit needs If-Match');
await expect(await request(categoryPath,{method:'PATCH',etag:'"stale"',body:{name:category.name,slug:category.slug,displayOrder:1,isActive:true}}),412,'Stale category edit denied');
await expect(await request(categoryPath,{method:'PATCH',etag:categoryEtag,body:{name:category.name,slug:category.slug,displayOrder:1,isActive:true}}),200,'Category reordered');
categoryRead=await expect(await request(categoryPath),200,'Read reordered category');
categoryEtag=categoryRead.headers.get('etag');assert.ok(categoryEtag);
const temporary=await json(await request('/api/v1/admin/wiki/categories',{method:'POST',body:{name:`Unused ${suffix}`,slug:`unused-${suffix}`,displayOrder:2}}),201,'Create unused category');
let unused=await expect(await request(`/api/v1/admin/wiki/categories/${temporary.id}`),200,'Read unused category');
await expect(await request(`/api/v1/admin/wiki/categories/${temporary.id}`,{method:'DELETE',etag:unused.headers.get('etag')}),204,'Delete unused category');
// Concurrent category deactivation and article creation on a disposable category.
// Under SQL Server SERIALIZABLE they cannot both succeed: a new draft must never
// end up referencing a category that was disabled by the concurrent request.
const raceCategory=await json(await request('/api/v1/admin/wiki/categories',{
 method:'POST',body:{name:`Race ${suffix}`,slug:`race-${suffix}`,displayOrder:3}
}),201,'Create isolated race category');
const racePath=`/api/v1/admin/wiki/categories/${raceCategory.id}`;
const raceCategoryRead=await expect(await request(racePath),200,'Read race category');
const raceEtag=raceCategoryRead.headers.get('etag');assert.ok(raceEtag);
const raceDraft={title:'Concurrent draft',slug:`race-article-${suffix}`,summary:'Concurrency test',
 markdown:'# Concurrent draft',categoryId:raceCategory.id};
const [raceCreate,raceDeactivate]=await Promise.all([
 request('/api/v1/admin/wiki/articles',{method:'POST',body:raceDraft}),
 request(racePath,{method:'PATCH',etag:raceEtag,body:{name:raceCategory.name,
  slug:raceCategory.slug,displayOrder:3,isActive:false}})
]);
assert.ok([201,400,409].includes(raceCreate.status),`Unexpected racing create: ${raceCreate.status}`);
assert.ok([200,409,412].includes(raceDeactivate.status),`Unexpected racing deactivate: ${raceDeactivate.status}`);
assert.ok(!(raceCreate.status===201&&raceDeactivate.status===200),
 'Concurrent category deactivation and article creation cannot both succeed');
const finalRaceCategory=await json(await request(racePath),200,'Read final race category');
if(raceCreate.status===201) assert.equal(finalRaceCategory.isActive,true,'Created article requires an active category');
if(raceDeactivate.status===200) assert.equal(finalRaceCategory.isActive,false,'Deactivated category must remain inactive');
// Disposable tag lifecycle and ETag checks.
const unusedTag=await json(await request('/api/v1/admin/wiki/tags',{method:'POST',
 body:{name:`Unused Tag ${suffix}`,slug:`unused-tag-${suffix}`}}),201,'Create disposable tag');
await expect(await request(`/api/v1/admin/wiki/tags/${unusedTag.id}`,{method:'PATCH',
 body:{name:unusedTag.name,slug:unusedTag.slug,isActive:false}}),428,'Tag edit needs If-Match');
await expect(await request(`/api/v1/admin/wiki/tags/${unusedTag.id}`,{method:'PATCH',etag:'"stale"',
 body:{name:unusedTag.name,slug:unusedTag.slug,isActive:false}}),412,'Stale tag edit denied');
await expect(await request(`/api/v1/admin/wiki/tags/${unusedTag.id}`,{method:'DELETE',etag:unusedTag.etag}),204,'Delete unused tag');
const tag=await json(await request('/api/v1/admin/wiki/tags',{method:'POST',
 body:{name:`Fishing Tag ${suffix}`,slug:`fishing-tag-${suffix}`}}),201,'Create article tag');
const tagPath=`/api/v1/admin/wiki/tags/${tag.id}`;
const first={title:'Smoke First Version',slug:`smoke-article-${suffix}`,summary:'Initial published version',markdown:'# Initial draft',categoryId:category.id,tagIds:[tag.id]};
const created=await json(await request('/api/v1/admin/wiki/articles',{method:'POST',body:first}),201,'Create draft');
await expect(await request(categoryPath,{method:'DELETE',etag:categoryEtag}),409,'Referenced draft protects category deletion');
await expect(await request(categoryPath,{method:'PATCH',etag:categoryEtag,body:{name:category.name,slug:category.slug,displayOrder:1,isActive:false}}),409,'Referenced draft prevents deactivation');
const id=created.id;
await expect(await request(`/api/v1/wiki/articles/${first.slug}`),404,'Draft privacy');
let tagCatalog=await json(await request('/api/v1/wiki/tags'),200,'Public tag catalog before publish');
assert.equal(tagCatalog.some(x=>x.slug===tag.slug),false,
 'Tag draft must not leak into public catalog');
let filtered=await json(await request(`/api/v1/wiki/articles?tag=${encodeURIComponent(tag.slug)}`),200,'Prepublish tag filter');
assert.equal(filtered.total,0,'Draft tag must not appear in public filter');
let admin=await expect(await request(`/api/v1/admin/wiki/articles/${id}`),200,'Admin read draft');
let etag=admin.headers.get('etag');assert.ok(etag);
let published=await json(await request(`/api/v1/admin/wiki/articles/${id}/publish`,{method:'POST',etag}),200,'Publish revision 1');
assert.equal(published.revision,1);
await expect(await request(categoryPath,{method:'PATCH',etag:categoryEtag,body:{name:`Renamed ${suffix}`,slug:category.slug,displayOrder:1,isActive:true}}),409,'Immutable revision prevents category rename');
let publicArticle=await json(await request(`/api/v1/wiki/articles/${first.slug}`),200,'Public revision 1');
assert.equal(publicArticle.title,first.title);
assert.deepEqual(publicArticle.tags,[tag.slug],'Published revision snapshots its tags');
tagCatalog=await json(await request('/api/v1/wiki/tags'),200,'Tag catalog after publish');
assert.equal(tagCatalog.find(x=>x.slug===tag.slug)?.articleCount,1,'Current published tag count');
filtered=await json(await request(`/api/v1/wiki/articles?tag=${encodeURIComponent(tag.slug)}`),200,'Published tag filter');
assert.equal(filtered.total,1);
await expect(await request(tagPath,{method:'DELETE',etag:tag.etag}),409,'Historical tag must not be deleted');
await expect(await request(tagPath,{method:'PATCH',etag:tag.etag,
 body:{name:tag.name,slug:tag.slug,isActive:false}}),409,'Published tag cannot deactivate');
admin=await expect(await request(`/api/v1/admin/wiki/articles/${id}`),200,'Reload ETag');
etag=admin.headers.get('etag');assert.ok(etag);
const update={...first,title:'Smoke Second Version',markdown:'# Changed draft',tagIds:[]};
await expect(await request(`/api/v1/admin/wiki/articles/${id}`,{method:'PATCH',etag,body:update}),200,'Edit draft');
publicArticle=await json(await request(`/api/v1/wiki/articles/${first.slug}`),200,'Draft edit does not change public');
assert.equal(publicArticle.title,first.title);
assert.deepEqual(publicArticle.tags,[tag.slug],'Public tag remains unchanged after draft edit');
await expect(await request(`/api/v1/admin/wiki/articles/${id}/publish`,{method:'POST',etag}),412,'Stale ETag denied');
admin=await expect(await request(`/api/v1/admin/wiki/articles/${id}`),200,'Reload after edit');
etag=admin.headers.get('etag');assert.ok(etag);
published=await json(await request(`/api/v1/admin/wiki/articles/${id}/publish`,{method:'POST',etag}),200,'Republish');
assert.equal(published.revision,2);
publicArticle=await json(await request(`/api/v1/wiki/articles/${first.slug}`),200,'Public revision 2');
assert.equal(publicArticle.title,update.title);
assert.deepEqual(publicArticle.tags,[],'Republish removes tag only from current public revision');
tagCatalog=await json(await request('/api/v1/wiki/tags'),200,'Tag catalog after republish');
assert.equal(tagCatalog.some(x=>x.slug===tag.slug),false,'Old historical tag must not appear in public catalog');
filtered=await json(await request(`/api/v1/wiki/articles?tag=${encodeURIComponent(tag.slug)}`),200,'Old tag filter');
assert.equal(filtered.total,0);
const revisions=await json(await request(`/api/v1/admin/wiki/articles/${id}/revisions`),200,'History');
assert.equal(revisions.length,2);
admin=await expect(await request(`/api/v1/admin/wiki/articles/${id}`),200,'Reload before restore');
etag=admin.headers.get('etag');assert.ok(etag);
await expect(await request(`/api/v1/admin/wiki/articles/${id}/revisions/${revisions.find(x=>x.number===1).id}/restore`,{method:'POST',etag}),200,'Restore into draft');
publicArticle=await json(await request(`/api/v1/wiki/articles/${first.slug}`),200,'Restore does not change public');
assert.equal(publicArticle.title,update.title);
assert.deepEqual(publicArticle.tags,[],'Restored tag remains private until publish');
admin=await expect(await request(`/api/v1/admin/wiki/articles/${id}`),200,'Reload restored draft tags');
assert.deepEqual((await admin.json()).tagIds,[tag.id],'Restore copies historical tags to draft');
admin=await expect(await request(`/api/v1/admin/wiki/articles/${id}`),200,'Reload before unpublish');
etag=admin.headers.get('etag');assert.ok(etag);
await expect(await request(`/api/v1/admin/wiki/articles/${id}/unpublish`,{method:'POST',etag}),204,'Unpublish');
await expect(await request(`/api/v1/wiki/articles/${first.slug}`),404,'Unpublished privacy');
console.log('SMOKE PASS: auth / categories / classification race / tags / draft 404 / publish / isolated edits / stale ETag / republish / restore / unpublish 404');
