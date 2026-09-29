'use client';

import Image from 'next/image';
import {useEffect,useState} from 'react';
import {editorialPreviewAssets,fallbackPreviewFor,type EditorialPreview} from '@/lib/article-previews';

export default function ArticlePreview({image}:{image:EditorialPreview}){
 const[failedSources,setFailedSources]=useState<string[]>([]);
 useEffect(()=>setFailedSources([]),[image.src]);
 const current=[image,fallbackPreviewFor(image),...editorialPreviewAssets]
  .find(candidate=>!failedSources.includes(candidate.src));

 return <figure className="editorial-preview">
  <div className="editorial-preview-frame">
   {current?<Image src={current.src} alt={current.alt} fill sizes="(max-width: 760px) 100vw, (max-width: 1100px) 50vw, 420px" unoptimized onError={()=>setFailedSources(values=>values.includes(current.src)?values:[...values,current.src])}/>:<span className="editorial-preview-unavailable" role="img" aria-label={image.alt}>Ảnh xem trước tạm thời không khả dụng</span>}
  </div>
  <figcaption>
   {current?.sourceUrl?<a href={current.sourceUrl} target="_blank" rel="noreferrer">Ảnh: {current.credit}</a>:<span>{current?.credit??image.credit}</span>}
   {current?.licenseUrl&&<a href={current.licenseUrl} target="_blank" rel="noreferrer">Giấy phép</a>}
  </figcaption>
 </figure>;
}
