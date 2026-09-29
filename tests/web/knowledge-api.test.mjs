import assert from 'node:assert/strict';
import test from 'node:test';
import * as knowledgeModule from '../../apps/web/src/lib/knowledge-api.ts';

const {knowledgeApi}=knowledgeModule;

test('Knowledge client requests the selected published kind and decodes relations', async()=>{
 const original=globalThis.fetch;
 try{
  const calls=[];
  globalThis.fetch=async(url,options)=>{calls.push({url:String(url),options});return {ok:true,json:async()=>({items:[{id:'n1',slug:'lyra',name:'Lyra',summary:'Warden',kind:'npc',revision:1,publishedAt:'2026-01-01T00:00:00Z'}],page:1,pageSize:20,total:1})};};
  const page=await knowledgeApi.list('npc','ly');
  assert.equal(page.items[0].kind,'npc');
  const url=new URL(calls[0].url);
  assert.equal(url.pathname,'/api/v1/knowledge/npc');
  assert.equal(url.searchParams.get('q'),'ly');
  assert.equal(calls[0].options.cache,'no-store');
 }finally{globalThis.fetch=original;}
});

test('Knowledge client keeps the public unpublished response as an error', async()=>{
 const original=globalThis.fetch;
 try{
  globalThis.fetch=async()=>({ok:false,status:404});
  await assert.rejects(knowledgeApi.detail('quest','draft-quest'),/Knowledge API 404/);
 }finally{globalThis.fetch=original;}
});

test('Knowledge detail decodes typed metadata and graph links', async()=>{
 const original=globalThis.fetch;
 try{
  globalThis.fetch=async()=>({ok:true,json:async()=>({id:'q1',slug:'quest',name:'Quest',summary:'A quest',kind:'quest',revision:1,publishedAt:'2026-01-01T00:00:00Z',markdown:'# Quest',metadata:{difficulty:3,location:{slug:'harbor',name:'Harbor',kind:'location'},steps:[{position:1,title:'Start',description:'Begin'}]},links:[{linkType:'usesItem',slug:'sword',name:'Sword',type:'catalog',sortOrder:0}]})});
  const detail=await knowledgeApi.detail('quest','quest');
  assert.equal(detail.metadata.difficulty,3);
  assert.equal(detail.metadata.steps[0].title,'Start');
  assert.equal(detail.links[0].type,'catalog');
 }finally{globalThis.fetch=original;}
});

test('Knowledge overview keeps successful groups available when one endpoint fails', async()=>{
 const overview=await knowledgeModule.loadKnowledgeOverview(async kind=>{
  if(kind==='npc')throw new Error('NPC endpoint unavailable');
  const items=kind==='location'?[{id:'l1',slug:'valley',name:'Thung lũng Sao',summary:'Địa danh',kind,revision:1,publishedAt:'2026-09-28T00:00:00Z'}]:[];
  return {items,page:1,pageSize:20,total:items.length};
 });
 assert.equal(overview.status,'partial');
 assert.deepEqual(overview.failedKinds,['npc']);
 assert.equal(overview.publishedTotal,1);
 assert.deepEqual(overview.entries.map(entry=>entry.kind),['quest','location','season']);
});

test('Knowledge overview reports valid empty collections as ready rather than offline', async()=>{
 const overview=await knowledgeModule.loadKnowledgeOverview(async kind=>({
  items:[],page:1,pageSize:20,total:0,
 }));
 assert.equal(overview.status,'ready');
 assert.equal(overview.publishedTotal,0);
 assert.equal(overview.entries.length,4);
 assert.deepEqual(overview.failedKinds,[]);
});

test('Knowledge overview reports an outage only when every group request fails', async()=>{
 const overview=await knowledgeModule.loadKnowledgeOverview(async()=>{
  throw new Error('service unavailable');
 });
 assert.equal(overview.status,'unavailable');
 assert.equal(overview.entries.length,0);
 assert.deepEqual(overview.failedKinds,['npc','quest','location','season']);
});
