'use client';

import {useEffect} from 'react';
import {useWikiHistory} from '@/lib/use-wiki-history';

export default function RecentArticleTracker({slug}:{slug:string}){
  const {ready,storageError,record}=useWikiHistory();
  useEffect(()=>{
    if(ready&&!storageError)record(slug);
  },[ready,storageError,record,slug]);
  return null;
}
