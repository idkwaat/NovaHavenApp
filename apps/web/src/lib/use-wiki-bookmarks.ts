'use client';

import {useEffect,useState} from 'react';
import {readBookmarkSlugs,toggleBookmark,wikiBookmarkStorageKey} from './wiki-bookmarks';

const changeEvent='nova-haven:wiki-bookmarks-changed';

export function useWikiBookmarks(){
  const [slugs,setSlugs]=useState<string[]>([]);
  const [ready,setReady]=useState(false);
  const [storageError,setStorageError]=useState(false);

  useEffect(()=>{
    const refresh=()=>{
      try{
        setSlugs(readBookmarkSlugs(window.localStorage));
        setStorageError(false);
      }catch{
        setStorageError(true);
      }finally{
        setReady(true);
      }
    };
    const onStorage=(event:StorageEvent)=>{
      if(event.key===null||event.key===wikiBookmarkStorageKey)refresh();
    };
    window.addEventListener('storage',onStorage);
    window.addEventListener(changeEvent,refresh);
    refresh();
    return()=>{
      window.removeEventListener('storage',onStorage);
      window.removeEventListener(changeEvent,refresh);
    };
  },[]);

  function toggle(slug:string){
    try{
      const next=toggleBookmark(window.localStorage,slug);
      setSlugs(next);
      setStorageError(false);
      window.dispatchEvent(new Event(changeEvent));
    }catch{
      setStorageError(true);
    }
  }

  return {slugs,ready,storageError,toggle};
}
