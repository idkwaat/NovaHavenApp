'use client';

import {useEffect,useState} from 'react';

type Status='unavailable'|'configured'|'healthy'|'stale';
type Capability={id:string;capabilityKey:string;displayName:string;owner:string;status:Status;lastCheckedAt:string|null;lastSuccessAt:string|null;safeMessage:string;etag:string};
type Mutation=(path:string,method:'POST'|'PATCH'|'DELETE',body?:object,precondition?:string)=>Promise<Response>;
type Props={mutate:Mutation;errorFor:(response:Response)=>Promise<string>;onStatus:(value:string)=>void;disabled:boolean};

const labels:Record<Status,string>={unavailable:'Unavailable',configured:'Configured',healthy:'Healthy',stale:'Stale'};

export default function IntegrationManager({mutate,errorFor,onStatus,disabled}:Props){
 const [items,setItems]=useState<Capability[]>([]);
 const [selected,setSelected]=useState<Capability|null>(null);
 const [status,setStatus]=useState<Status>('unavailable');
 const [safeMessage,setSafeMessage]=useState('');
 const [busy,setBusy]=useState(false);

 async function refresh(){
  const response=await fetch('/api/v1/admin/integrations/capabilities',{credentials:'same-origin',cache:'no-store'});
  if(!response.ok)throw Error(await errorFor(response));
  const next=await response.json() as Capability[];
  setItems(next);
  if(selected){const current=next.find(item=>item.id===selected.id);if(current){setSelected(current);setStatus(current.status);setSafeMessage(current.safeMessage);}}
 }
 useEffect(()=>{let active=true;(async()=>{try{await refresh();}catch(error){if(active)onStatus((error as Error).message);}})();return()=>{active=false;};},[]);
 function choose(item:Capability){setSelected(item);setStatus(item.status);setSafeMessage(item.safeMessage);}
 async function save(event:React.FormEvent){
  event.preventDefault();
  if(!selected)return;
  setBusy(true);
  try{
   const response=await mutate(`/api/v1/admin/integrations/capabilities/${selected.capabilityKey}`,'PATCH',{status,safeMessage},selected.etag);
   if(!response.ok)throw Error(await errorFor(response));
   await refresh();
   onStatus(`Đã cập nhật trạng thái ${selected.displayName}.`);
  }catch(error){onStatus((error as Error).message);}finally{setBusy(false);}
 }
 return <section className="category-manager" aria-labelledby="integration-heading">
  <h2 id="integration-heading">Integration Foundation</h2>
  <p>Trạng thái capability local. Đây không phải kết nối thật tới Minecraft hoặc payment provider; secret không được lưu trong database.</p>
  <div className="tile-grid">{items.map(item=><button type="button" className="article-card" key={item.id} onClick={()=>choose(item)}><strong>{item.displayName}</strong><small>{item.capabilityKey} · {labels[item.status]}</small></button>)}</div>
  <div className="form-row"><button type="button" onClick={()=>refresh().catch(error=>onStatus((error as Error).message))} disabled={disabled||busy}>Làm mới trạng thái</button></div>
  {selected&&<form className="form" onSubmit={save}>
   <h3>{selected.displayName}</h3>
   <label>Trạng thái<select value={status} onChange={event=>setStatus(event.target.value as Status)} disabled={disabled||busy}><option value="unavailable">Unavailable</option><option value="configured">Configured</option><option value="healthy">Healthy</option><option value="stale">Stale</option></select></label>
   <label>Thông báo an toàn<textarea maxLength={500} value={safeMessage} onChange={event=>setSafeMessage(event.target.value)} disabled={disabled||busy}/></label>
   <p className="muted">Capability: {selected.capabilityKey} · Owner: {selected.owner}. ETag được dùng để chống ghi đè.</p>
   <button type="submit" disabled={disabled||busy}>Lưu trạng thái</button>
  </form>}
 </section>;
}
