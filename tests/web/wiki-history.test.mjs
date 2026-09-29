import assert from 'node:assert/strict';
import test from 'node:test';
import {clearRecentWikiArticles,readRecentWikiSlugs,recordRecentWikiArticle,wikiRecentStorageKey} from '../../apps/web/src/lib/wiki-history.ts';

class MemoryStorage {
  values=new Map();
  getItem(key){return this.values.get(key)??null;}
  setItem(key,value){this.values.set(key,value);}
  removeItem(key){this.values.delete(key);}
}

test('reading history ignores corrupt entries and de-duplicates valid article slugs',()=>{
  const storage=new MemoryStorage();
  storage.setItem(wikiRecentStorageKey,'bad json');
  assert.deepEqual(readRecentWikiSlugs(storage),[]);
  storage.setItem(wikiRecentStorageKey,JSON.stringify(['first-guide','../admin','first-guide','second-guide']));
  assert.deepEqual(readRecentWikiSlugs(storage),['first-guide','second-guide']);
});

test('opening an article moves it to the front and caps history at twenty',()=>{
  const storage=new MemoryStorage();
  const slugs=Array.from({length:22},(_,index)=>`guide-${index}`);
  storage.setItem(wikiRecentStorageKey,JSON.stringify(slugs.slice(0,20)));
  assert.deepEqual(recordRecentWikiArticle(storage,'guide-10'),['guide-10',...slugs.slice(0,10),...slugs.slice(11,20)]);
  const capped=recordRecentWikiArticle(storage,'new-guide');
  assert.equal(capped.length,20);
  assert.equal(capped[0],'new-guide');
  assert.throws(()=>recordRecentWikiArticle(storage,'../admin'),/slug/i);
});

test('clearing recent history removes only its versioned local preference key',()=>{
  const storage=new MemoryStorage();
  storage.setItem(wikiRecentStorageKey,JSON.stringify(['guide-one']));
  storage.setItem('unrelated-preference','keep');
  clearRecentWikiArticles(storage);
  assert.equal(storage.getItem(wikiRecentStorageKey),null);
  assert.equal(storage.getItem('unrelated-preference'),'keep');
});
