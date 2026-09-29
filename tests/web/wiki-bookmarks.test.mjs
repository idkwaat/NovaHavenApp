import assert from 'node:assert/strict';
import test from 'node:test';
import {filterBookmarkedArticles,loadPublishedWikiArticles,readBookmarkSlugs,toggleBookmark} from '../../apps/web/src/lib/wiki-bookmarks.ts';

class MemoryStorage {
  values = new Map();
  getItem(key) { return this.values.get(key) ?? null; }
  setItem(key,value) { this.values.set(key,value); }
}

test('bookmark storage tolerates corruption, removes invalid slugs and duplicates',()=>{
  const storage=new MemoryStorage();
  storage.setItem('nova_haven_wiki_bookmarks_v1','not-json');
  assert.deepEqual(readBookmarkSlugs(storage),[]);
  storage.setItem('nova_haven_wiki_bookmarks_v1',JSON.stringify(['welcome-guide','../admin','welcome-guide','UPPER','map-of-stars']));
  assert.deepEqual(readBookmarkSlugs(storage),['welcome-guide','map-of-stars']);
});

test('toggling a bookmark adds newest first and removes an existing entry',()=>{
  const storage=new MemoryStorage();
  storage.setItem('nova_haven_wiki_bookmarks_v1',JSON.stringify(['old-guide']));
  assert.deepEqual(toggleBookmark(storage,'map-of-stars'),['map-of-stars','old-guide']);
  assert.deepEqual(toggleBookmark(storage,'old-guide'),['map-of-stars']);
  assert.throws(()=>toggleBookmark(storage,'../admin'),/slug/i);
});

test('published bookmark loading uses public same-origin URLs and omits unpublished articles',async()=>{
  const requests=[];
  const fetcher=async(url,options)=>{
    requests.push({url,options});
    const slug=decodeURIComponent(url.split('/').at(-1));
    if(slug==='retired-guide')return {ok:false,status:404};
    return {ok:true,status:200,json:async()=>({id:slug,slug,title:'Hướng dẫn tiếng Việt',summary:'Nội dung đã xuất bản',category:'guide',tags:[],publishedAt:'2026-01-01T00:00:00Z',revision:2,markdown:'# Hướng dẫn',related:[]})};
  };
  const result=await loadPublishedWikiArticles(['map-of-stars','retired-guide'],fetcher);
  assert.deepEqual(result.articles.map(article=>article.slug),['map-of-stars']);
  assert.equal(result.unavailableCount,1);
  assert.equal(requests[0].url,'/api/v1/wiki/articles/map-of-stars');
  assert.equal(requests[0].options.cache,'no-store');
  assert.equal(requests[0].options.credentials,'same-origin');
});

test('published bookmark loading propagates transient API failures for retry',async()=>{
  await assert.rejects(loadPublishedWikiArticles(['map-of-stars'],async()=>({ok:false,status:503})),/503/);
});

test('bookmark loader bounds concurrent public requests',async()=>{
  let active=0;
  let peak=0;
  const fetcher=async(url)=>{
    active++;
    peak=Math.max(peak,active);
    await new Promise(resolve=>setTimeout(resolve,2));
    active--;
    const slug=url.split('/').at(-1);
    return {ok:true,status:200,json:async()=>({id:slug,slug,title:slug,summary:'',category:'guide',tags:[],publishedAt:'2026-01-01T00:00:00Z',revision:1,markdown:'',related:[]})};
  };
  const slugs=Array.from({length:13},(_,index)=>`guide-${index}`);
  const result=await loadPublishedWikiArticles(slugs,fetcher,3);
  assert.equal(result.articles.length,13);
  assert.equal(peak,3);
});

test('saved article search ignores case and Vietnamese diacritics across title, summary and tags',()=>{
  const articles=[
    {slug:'guide',title:'Hướng dẫn chiến đấu',summary:'Bí quyết cho người mới',tags:['kiem-si'],category:'guide'},
    {slug:'atlas',title:'Bản đồ Thung lũng Sao',summary:'Địa danh khởi hành',tags:['truyen-thuyet'],category:'world'},
  ];
  assert.deepEqual(filterBookmarkedArticles(articles,'HUONG dan'),[articles[0]]);
  assert.deepEqual(filterBookmarkedArticles(articles,'TRUYỀN THUYẾT'),[articles[1]]);
  assert.deepEqual(filterBookmarkedArticles(articles,'   '),articles);
  assert.deepEqual(filterBookmarkedArticles(articles,'không tồn tại'),[]);
});
