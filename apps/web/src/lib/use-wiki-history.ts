'use client';

import {useCallback,useEffect,useState} from 'react';
import {clearRecentWikiArticles,readRecentWikiSlugs,recordRecentWikiArticle,wikiRecentStorageKey} from './wiki-history';

const changeEvent='nova-haven:wiki-reading-history-changed';

export function useWikiHistory(){
  const [slugs,setSlugs]=useState<string[]>([]);
  const [ready,setReady]=useState(false);
  const [storageError,setStorageError]=useState(false);

  useEffect(()=>{
    const refresh=()=>{
      try{
        setSlugs(readRecentWikiSlugs(window.localStorage));
        setStorageError(false);
      }catch{
        setStorageError(true);
      }finally{
        setReady(true);
      }
    };
    const onStorage=(event:StorageEvent)=>{
      if(event.key===null||event.key===wikiRecentStorageKey)refresh();
    };
    window.addEventListener('storage',onStorage);
    window.addEventListener(changeEvent,refresh);
    refresh();
    return()=>{
      window.removeEventListener('storage',onStorage);
      window.removeEventListener(changeEvent,refresh);
    };
  },[]);

  const record=useCallback((slug:string)=>{
    try{
      setSlugs(recordRecentWikiArticle(window.localStorage,slug));
      setStorageError(false);
      window.dispatchEvent(new Event(changeEvent));
    }catch{
      setStorageError(true);
    }
  },[]);

  const clear=useCallback(()=>{
    try{
      clearRecentWikiArticles(window.localStorage);
      setSlugs([]);
      setStorageError(false);
      window.dispatchEvent(new Event(changeEvent));
      return true;
    }catch{
      setStorageError(true);
      return false;
    }
  },[]);

  return {slugs,ready,storageError,record,clear};
}
