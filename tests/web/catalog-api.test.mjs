import assert from 'node:assert/strict';
import test from 'node:test';
import {catalogApi} from '../../apps/web/src/lib/catalog-api.ts';

test('public Catalog client requests published item pages and filters', async()=>{
 const original=globalThis.fetch;
 const calls=[];
 try{
  globalThis.fetch=async(url,options)=>{calls.push({url:String(url),options});return {ok:true,json:async()=>({items:[],page:1,pageSize:20,total:0})};};
  const result=await catalogApi.items(' sword ','weapon');
  assert.deepEqual(result.items,[]);
  const url=new URL(calls[0].url);
  assert.equal(url.pathname,'/api/v1/catalog/items');
  assert.equal(url.searchParams.get('q'),' sword ');
  assert.equal(url.searchParams.get('kind'),'weapon');
  assert.equal(calls[0].options.cache,'no-store');
 }finally{globalThis.fetch=original;}
});

test('public Catalog client rejects an unpublished item response',async()=>{
 const original=globalThis.fetch;
 try{
  globalThis.fetch=async()=>({ok:false,status:404});
  await assert.rejects(catalogApi.item('private-draft'),/Catalog API 404/);
 }finally{globalThis.fetch=original;}
});

test('public Catalog contract includes published recipe snapshots',async()=>{
 const original=globalThis.fetch;
 try{
  globalThis.fetch=async()=>({ok:true,json:async()=>({slug:'forge',name:'Forge',summary:'Recipe',revision:1,markdown:'# Forge',ingredients:[{itemId:'i1',itemSlug:'ingot',itemName:'Ingot',quantity:2}],outputs:[{itemId:'i2',itemSlug:'sword',itemName:'Sword',quantity:1}]})});
  const {catalogApi}=await import('../../apps/web/src/lib/catalog-api.ts');
  const recipe=await catalogApi.recipe('forge');
  assert.equal(recipe.ingredients[0].itemName,'Ingot');
  assert.equal(recipe.outputs[0].quantity,1);
 }finally{globalThis.fetch=original;}
});
