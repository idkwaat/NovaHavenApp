'use client';

import {useEffect,useState} from 'react';

type Revision={id:string;number:number;title:string;publishedAt:string;publishedBy:string};
type Audit={id:string;action:string;entityType:string;entityId:string;detailsJson:string;occurredAt:string;actorUserId:string};
type Props={
 articleId:string;
 etag:string|null;
 mutate:(path:string,method:'POST'|'PATCH'|'DELETE',body?:object,precondition?:string)=>Promise<Response>;
 onStatus:(message:string)=>void;
 onRestored:()=>Promise<void>;
 disabled:boolean;
};

export default function RevisionHistory({articleId,etag,mutate,onStatus,onRestored,disabled}:Props){
 const[revisions,setRevisions]=useState<Revision[]>([]);
 const[audit,setAudit]=useState<Audit[]>([]);
 const[busy,setBusy]=useState(false);
 useEffect(()=>{
  let active=true;
  (async()=>{
   const [revisionResponse,auditResponse]=await Promise.all([
    fetch(`/api/v1/admin/wiki/articles/${articleId}/revisions`,{cache:'no-store'}),
    fetch(`/api/v1/admin/audit?entityType=WikiArticle&entityId=${encodeURIComponent(articleId)}`,{cache:'no-store'})
   ]);
   if(!active)return;
   if(revisionResponse.ok)setRevisions((await revisionResponse.json() as Revision[]));
   if(auditResponse.ok){const result=await auditResponse.json() as {items:Audit[]};setAudit(result.items);}
  })().catch(()=>{if(active)onStatus('Không thể tải lịch sử revision hoặc audit.');});
  return()=>{active=false;};
 },[articleId,onStatus]);
 async function restore(revision:Revision){
  if(!etag){onStatus('Thiếu ETag bài viết. Hãy tải lại trước khi khôi phục.');return;}
  if(!window.confirm(`Khôi phục revision ${revision.number} thành bản nháp? Bản đang công khai không đổi.`))return;
  setBusy(true);
  try{
   const response=await mutate(`/api/v1/admin/wiki/articles/${articleId}/revisions/${revision.id}/restore`,'POST',undefined,etag);
   if(!response.ok){const data=await response.json().catch(()=>null) as {title?:string}|null;throw Error(data?.title??`API trả về ${response.status}`);}
   onStatus(`Đã khôi phục revision ${revision.number} thành bản nháp.`);
   await onRestored();
  }catch(error){onStatus((error as Error).message);}finally{setBusy(false);}
 }
 return <section className="revision-panel" aria-labelledby="revision-heading">
  <div className="revision-columns">
   <div><h2 id="revision-heading">Lịch sử revision</h2>
    {revisions.length===0?<p className="notice">Chưa có revision đã xuất bản.</p>:<ol className="revision-list">
     {revisions.map(revision=><li key={revision.id}><div><strong>Revision {revision.number}</strong><span>{revision.title}</span><small>{new Date(revision.publishedAt).toLocaleString('vi-VN',{timeZone:'UTC'})} UTC</small></div><button type="button" disabled={disabled||busy} onClick={()=>restore(revision)}>Khôi phục thành nháp</button></li>)}
    </ol>}
   </div>
   <div><h2>Audit history</h2>
    {audit.length===0?<p className="notice">Chưa có audit event.</p>:<ul className="audit-list">{audit.map(event=><li key={event.id}><strong>{event.action}</strong><small>{new Date(event.occurredAt).toLocaleString('vi-VN',{timeZone:'UTC'})} UTC</small></li>)}</ul>}
   </div>
  </div>
 </section>;
}
