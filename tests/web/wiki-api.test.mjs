import assert from 'node:assert/strict';
import test from 'node:test';
import {wikiApi} from '../../apps/web/src/lib/wiki-api.ts';

test('public Wiki client filters by published category via public endpoint only', async()=>{
 const original=globalThis.fetch;
 const calls=[];
 try{
  globalThis.fetch=async(url,options)=>{
   calls.push({url:String(url),options});
   return {ok:true,json:async()=>({items:[],page:1,pageSize:20,total:0})};
  };
  const result=await wikiApi.articles(' fishing ','professions');
  assert.deepEqual(result.items,[]);
  assert.equal(calls.length,1);
  const url=new URL(calls[0].url);
  assert.equal(url.pathname,'/api/v1/wiki/articles');
  assert.equal(url.searchParams.get('category'),'professions');
  assert.equal(url.searchParams.get('q'),' fishing ');
  assert.equal(calls[0].options.cache,'no-store');
 }finally{globalThis.fetch=original;}
});

test('a missing or private article from backend cannot be mistaken for published',async()=>{
 const original=globalThis.fetch;
 try{
  globalThis.fetch=async()=>({ok:false,status:404});
  await assert.rejects(wikiApi.article('private-draft'),/Wiki API 404/);
 }finally{globalThis.fetch=original;}
});

test('public article response preserves published related links',async()=>{
 const original=globalThis.fetch;
 try{
  globalThis.fetch=async()=>({ok:true,json:async()=>({id:'article',slug:'article',title:'Article',summary:'',category:'wiki',tags:[],markdown:'# Article',related:[{id:'related',slug:'related',title:'Related',summary:'',category:'wiki',tags:[],publishedAt:'2026-01-01T00:00:00Z',revision:1}],publishedAt:'2026-01-01T00:00:00Z',revision:1})});
  const item=await wikiApi.article('article');
  assert.equal(item.related[0].slug,'related');
 }finally{globalThis.fetch=original;}
});
