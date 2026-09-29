import assert from 'node:assert/strict';
import test from 'node:test';
import * as communityModule from '../../apps/web/src/lib/community-api.ts';
const {communityApi}=communityModule;

test('Community client requests published event pages and decodes metadata',async()=>{
 const original=globalThis.fetch;
 try{
  const calls=[];
  globalThis.fetch=async(url,options)=>{calls.push({url:String(url),options});return {ok:true,json:async()=>({items:[{id:'e1',slug:'festival',name:'Festival',summary:'Gathering',kind:'event',revision:1,publishedAt:'2026-01-01T00:00:00Z'}],page:1,pageSize:20,total:1})};};
  const page=await communityApi.list('event');
  assert.equal(page.items[0].name,'Festival');
  const url=new URL(calls[0].url);
  assert.equal(url.pathname,'/api/v1/community/event');
  assert.equal(calls[0].options.cache,'no-store');
 }finally{globalThis.fetch=original;}
});

test('Community detail preserves editorial leaderboard rows',async()=>{
 const original=globalThis.fetch;
 try{
  globalThis.fetch=async()=>({ok:true,json:async()=>({id:'b1',slug:'cup',name:'Cup',summary:'Standings',kind:'leaderboard',revision:1,publishedAt:'2026-01-01T00:00:00Z',markdown:'# Cup',metadata:{leaderboardCategory:'Fishing',rows:[{rank:1,participantName:'Lyra',score:120.5,note:'Verified'}]}})});
  const detail=await communityApi.detail('leaderboard','cup');
  assert.equal(detail.metadata.rows[0].participantName,'Lyra');
  assert.equal(detail.metadata.rows[0].score,120.5);
 }finally{globalThis.fetch=original;}
});

test('Community overview keeps available categories when one category endpoint fails',async()=>{
 const overview=await communityModule.loadCommunityOverview(async kind=>{
  if(kind==='event')throw new Error('temporary outage');
  return {items:[],page:1,pageSize:20,total:kind==='guild'?2:0};
 });
 assert.equal(overview.status,'partial');
 assert.deepEqual(overview.failedKinds,['event']);
 assert.equal(overview.entries.length,4);
 assert.equal(overview.publishedTotal,2);
});

test('Community overview treats successful empty categories as available data',async()=>{
 const overview=await communityModule.loadCommunityOverview(async()=>({items:[],page:1,pageSize:20,total:0}));
 assert.equal(overview.status,'ready');
 assert.equal(overview.failedKinds.length,0);
 assert.equal(overview.entries.length,5);
 assert.equal(overview.publishedTotal,0);
});

test('Community overview reports unavailable only when every category request fails',async()=>{
 const overview=await communityModule.loadCommunityOverview(async()=>{throw new Error('offline');});
 assert.equal(overview.status,'unavailable');
 assert.deepEqual(overview.failedKinds,['event','guild','player','housing','leaderboard']);
 assert.equal(overview.entries.length,0);
});
