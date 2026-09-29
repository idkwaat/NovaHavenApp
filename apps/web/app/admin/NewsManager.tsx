'use client';

import {useCallback,useEffect,useState} from 'react';

type NewsInput={title:string;slug:string;summary:string;markdown:string};
type NewsEntry={id:string;slug:string;title:string;state:number;publishedAt:string|null;updatedAt:string;etag:string};
type Props={
 mutate:(path:string,method:'POST'|'PATCH'|'DELETE',body?:object,precondition?:string)=>Promise<Response>;
 errorFor:(response:Response)=>Promise<string>;
 onStatus:(message:string)=>void;
 disabled:boolean;
};
const blank:NewsInput={title:'',slug:'',summary:'',markdown:''};

export default function NewsManager({mutate,errorFor,onStatus,disabled}:Props){
 const[items,setItems]=useState<NewsEntry[]>([]);const[selected,setSelected]=useState<string|null>(null);const[draft,setDraft]=useState<NewsInput>(blank);const[etag,setEtag]=useState<string|null>(null);const[busy,setBusy]=useState(false);
 const load=useCallback(async()=>{const response=await fetch('/api/v1/admin/news',{cache:'no-store'});if(response.ok)setItems(await response.json() as NewsEntry[]);},[]);
 useEffect(()=>{load().catch(()=>undefined);},[load]);
 async function run(action:()=>Promise<void>){setBusy(true);try{await action();}catch(error){onStatus((error as Error).message);}finally{setBusy(false);}}
 async function select(id:string){if(!id){setSelected(null);setDraft(blank);setEtag(null);return;}await run(async()=>{const response=await fetch(`/api/v1/admin/news/${id}`,{cache:'no-store'});if(!response.ok)throw Error(await errorFor(response));const data=await response.json() as NewsInput;setSelected(id);setDraft(data);setEtag(response.headers.get('ETag'));});}
 async function save(event:React.FormEvent){event.preventDefault();if(!draft.title.trim()||!draft.slug.trim()||!draft.markdown.trim()){onStatus('News cần title, slug và nội dung.');return;}await run(async()=>{const response=selected?await mutate(`/api/v1/admin/news/${selected}`,'PATCH',draft,etag??undefined):await mutate('/api/v1/admin/news','POST',draft);if(!response.ok)throw Error(await errorFor(response));const data=await response.json() as {id:string;etag?:string};const id=selected??data.id;setSelected(id);setEtag(response.headers.get('ETag')??data.etag??null);await load();onStatus('Đã lưu News draft.');});}
 async function lifecycle(action:'publish'|'unpublish'){if(!selected||!etag){onStatus('Hãy chọn News và tải ETag trước khi thay đổi trạng thái.');return;}await run(async()=>{const response=await mutate(`/api/v1/admin/news/${selected}/${action}`,'POST',undefined,etag??undefined);if(!response.ok)throw Error(await errorFor(response));await select(selected);await load();onStatus(action==='publish'?'Đã publish News.':'Đã unpublish News.');});}
 return <section className="category-manager" aria-labelledby="news-admin-heading"><h2 id="news-admin-heading">News / Changelog</h2><p>News có lifecycle riêng, không dùng chung revision Wiki. Public chỉ đọc bài đã publish.</p><div className="category-management-grid"><div><div className="form-row"><button type="button" disabled={disabled||busy} onClick={()=>{setSelected(null);setDraft(blank);setEtag(null);}}>+ News mới</button><button type="button" disabled={disabled||busy} onClick={()=>load()}>Làm mới</button></div><div className="tile-grid">{items.map(item=><button type="button" className="article-card" key={item.id} onClick={()=>select(item.id)}><strong>{item.title}</strong><small>{item.slug} · {['Nháp','Đã publish','Đã unpublish'][item.state]??item.state}</small></button>)}</div></div><form className="form" onSubmit={save}><label>Tiêu đề<input required maxLength={160} value={draft.title} onChange={e=>setDraft(current=>({...current,title:e.target.value}))}/></label><label>Slug<input required maxLength={120} value={draft.slug} onChange={e=>setDraft(current=>({...current,slug:e.target.value}))}/></label><label>Tóm tắt<textarea maxLength={500} value={draft.summary} onChange={e=>setDraft(current=>({...current,summary:e.target.value}))}/></label><label>Markdown<textarea required maxLength={50000} value={draft.markdown} onChange={e=>setDraft(current=>({...current,markdown:e.target.value}))}/></label><div className="form-row"><button type="submit" disabled={disabled||busy}>Lưu News draft</button><button type="button" disabled={disabled||busy||!selected} onClick={()=>lifecycle('publish')}>Publish</button><button type="button" className="danger" disabled={disabled||busy||!selected} onClick={()=>lifecycle('unpublish')}>Unpublish</button></div></form></div></section>;
}
