import assert from 'node:assert/strict';
import test from 'node:test';
import {readFileSync} from 'node:fs';
import {wikiApi} from '../../apps/web/src/lib/wiki-api.ts';
import {validateArticle, wikiListHref} from '../../apps/web/src/lib/wiki-model.ts';
import contract from '../../contracts/openapi/wiki-v1.json' with {type:'json'};

const guid='58c19a0d-3a3e-4b10-acf7-6b94c4555d4f';
const valid={title:'Fishing',slug:'fishing-guide',summary:'Guide',markdown:'# Guide',categoryId:guid,tagIds:[guid]};

test('tag selection allows zero or at most ten distinct UUIDs',()=>{
  assert.deepEqual(validateArticle({...valid,tagIds:[]}),{});
  assert.equal(validateArticle({...valid,tagIds:[guid,guid]}).tagIds?.length>0,true);
  assert.equal(validateArticle({...valid,tagIds:['not-uuid']}).tagIds?.length>0,true);
  const ids=Array.from({length:11},(_,i)=>`58c19a0d-3a3e-4b10-acf7-${(i+1).toString(16).padStart(12,'0')}`);
  assert.ok(validateArticle({...valid,tagIds:ids}).tagIds);
});

test('Wiki navigation preserves search, category and tag on pagination',()=>{
  const href=wikiListHref({q:'my quest',category:'professions',tag:'rare-fish',page:2});
  const url=new URL(href,'https://nova.example');
  assert.equal(url.pathname,'/wiki');
  assert.equal(url.searchParams.get('q'),'my quest');
  assert.equal(url.searchParams.get('category'),'professions');
  assert.equal(url.searchParams.get('tag'),'rare-fish');
  assert.equal(url.searchParams.get('page'),'2');
});

test('public API client sends tag filter to published articles endpoint only',async()=>{
 const original=globalThis.fetch;
 try{
  let requested;
  globalThis.fetch=async(url)=>{requested=new URL(url);return {ok:true,json:async()=>({items:[],page:1,pageSize:20,total:0})};};
  await wikiApi.articles('','',1,'rare-fish');
  assert.equal(requested.pathname,'/api/v1/wiki/articles');
  assert.equal(requested.searchParams.get('tag'),'rare-fish');
 }finally{globalThis.fetch=original;}
});

test('tag API contract declares protected mutations and public filter',()=>{
 const admin='/api/v1/admin/wiki/tags';
 const item='/api/v1/admin/wiki/tags/{id}';
 assert.ok(contract.paths[admin]?.get);
 assert.deepEqual(contract.paths[admin]?.post?.security,[{adminCookie:[]}]);
 for(const verb of ['patch','delete']){
  const op=contract.paths[item]?.[verb];
  assert.deepEqual(op?.security,[{adminCookie:[]}]);
  assert.ok(op?.parameters?.some(p=>p.name==='If-Match'&&p.required));
 }
 assert.ok(contract.paths['/api/v1/wiki/tags']?.get);
 assert.ok(contract.paths['/api/v1/wiki/articles'].get.parameters.some(p=>p.name==='tag'));
});

test('tag snapshot is written on publish and public filter excludes draft membership',()=>{
 const repository=readFileSync(new URL('../../backend/NovaHaven.Infrastructure/Persistence/Repositories/Wiki/EfWikiArticleRepository.cs',import.meta.url),'utf8');
 const publicCode=readFileSync(new URL('../../backend/NovaHaven.Api/Endpoints/PublicWikiEndpoints.cs',import.meta.url),'utf8');
 assert.match(repository,/dbContext\.RevisionTags\.AddRange/);
 assert.match(publicCode,/db\.RevisionTags\.Any/);
 assert.doesNotMatch(publicCode,/db\.DraftTags/);
});

test('tag manager validation rejects malformed tag names/slugs and allows active toggle',async()=>{
  const {validateTag}=await import('../../apps/web/src/lib/wiki-tag.ts');
  assert.deepEqual(validateTag({name:'Fishing',slug:'fishing',isActive:true}),{});
  assert.ok(validateTag({name:'',slug:'fishing',isActive:true}).name);
  assert.ok(validateTag({name:'Fishing',slug:'Fishing',isActive:true}).slug);
  assert.ok(validateTag({name:'Fishing',slug:'fishing',isActive:'yes'}).isActive);
});

test('Next Wiki UI exposes published tag navigation and admin tag multi-selection',()=>{
 const listing=readFileSync(new URL('../../apps/web/app/wiki/page.tsx',import.meta.url),'utf8');
 const detail=readFileSync(new URL('../../apps/web/app/wiki/[slug]/page.tsx',import.meta.url),'utf8');
 const editor=readFileSync(new URL('../../apps/web/app/admin/page.tsx',import.meta.url),'utf8');
 assert.match(listing,/wikiApi\.tags\(/);
 assert.match(listing,/wikiListHref\(/);
 assert.match(detail,/article\.tags/);
 assert.match(editor,/TagManager/);
 assert.match(editor,/tagIds/);
});

test('tag join tables enforce unique membership and restrictive foreign keys',()=>{
 const model=readFileSync(new URL('../../backend/NovaHaven.Infrastructure/Data/NovaDbContext.cs',import.meta.url),'utf8');
 for(const entity of ['WikiDraftTag','WikiRevisionTag']){
  const block=model.split(`builder.Entity<${entity}>`)[1]?.split('builder.Entity<')[0]??'';
  assert.match(block,/HasKey\(x => new \{ x\./);
  assert.equal((block.match(/DeleteBehavior\.Restrict/g)??[]).length,2);
 }
});

test('TagUpdate OpenAPI schema accepts active flag without allOf/additionalProperties contradiction',()=>{
 const schema=contract.components.schemas.TagUpdate;
 assert.equal(schema.additionalProperties,false);
 assert.ok(schema.required.includes('isActive'));
 assert.equal(schema.properties.isActive.type,'boolean');
});

test('live SQL smoke script has tag visibility and historical reference assertions',()=>{
 const smoke=readFileSync(new URL('../../scripts/wiki-smoke.mjs',import.meta.url),'utf8');
 assert.match(smoke,/Tag draft must not leak into public catalog/);
 assert.match(smoke,/Public tag remains unchanged after draft edit/);
 assert.match(smoke,/Historical tag must not be deleted/);
 assert.match(smoke,/Restored tag remains private until publish/);
});

test('public tag catalog omits tags used only in private drafts or old revisions',()=>{
 const source=readFileSync(new URL('../../backend/NovaHaven.Api/Endpoints/PublicWikiEndpoints.cs',import.meta.url),'utf8');
 const catalog=source.split('group.MapGet("/tags"')[1]?.split('group.MapGet("/categories"')[0]??'';
 assert.match(catalog,/article\.State == ArticleState\.Published/);
 assert.match(catalog,/article\.PublishedRevisionId/);
 assert.match(catalog,/byId\.GetValueOrDefault\(x\.Id\) > 0/);
});
