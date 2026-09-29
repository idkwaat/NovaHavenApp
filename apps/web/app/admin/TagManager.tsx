'use client';

import {useState} from 'react';
import {validateTag,type TagInput} from '@/lib/wiki-tag';

export type AdminTag=TagInput & {id:string;etag:string};
type Props={
  tags:AdminTag[];
  mutate:(path:string,method:'POST'|'PATCH'|'DELETE',body?:object,etag?:string)=>Promise<Response>;
  refresh:()=>Promise<void>;
  onStatus:(message:string)=>void;
  errorFor:(response:Response)=>Promise<string>;
  disabled:boolean;
};
const blank:TagInput={name:'',slug:'',isActive:true};

export default function TagManager({tags,mutate,refresh,onStatus,errorFor,disabled}:Props){
  const [newTag,setNewTag]=useState<TagInput>(blank);
  const [selected,setSelected]=useState('');
  const [editing,setEditing]=useState<TagInput>(blank);
  const [etag,setEtag]=useState('');
  const [busy,setBusy]=useState(false);
  const locked=disabled||busy;
  function select(id:string){
    const tag=tags.find(x=>x.id===id);
    setSelected(tag?.id??'');setEtag(tag?.etag??'');
    setEditing(tag?{name:tag.name,slug:tag.slug,isActive:tag.isActive}:blank);
  }
  async function run(action:()=>Promise<void>){
    setBusy(true);
    try{await action();}catch(e){onStatus((e as Error).message);}finally{setBusy(false);}
  }
  async function create(event:React.FormEvent){
    event.preventDefault();
    const errors=validateTag(newTag);
    if(Object.keys(errors).length){onStatus(Object.values(errors).join(' · '));return;}
    await run(async()=>{
      const response=await mutate('/api/v1/admin/wiki/tags','POST',{name:newTag.name,slug:newTag.slug});
      if(!response.ok)throw Error(await errorFor(response));
      setNewTag(blank);await refresh();onStatus('Đã tạo tag Wiki.');
    });
  }
  async function update(event:React.FormEvent){
    event.preventDefault();
    if(!selected||!etag){onStatus('Thiếu ETag của tag, hãy tải lại danh sách.');return;}
    const errors=validateTag(editing);
    if(Object.keys(errors).length){onStatus(Object.values(errors).join(' · '));return;}
    await run(async()=>{
      const response=await mutate(`/api/v1/admin/wiki/tags/${selected}`,'PATCH',editing,etag);
      if(!response.ok)throw Error(await errorFor(response));
      select('');await refresh();onStatus('Đã cập nhật tag.');
    });
  }
  async function remove(){
    if(!selected||!etag)return;
    if(!window.confirm('Xóa tag này? Không thể xóa tag đang được bản nháp hoặc revision sử dụng.'))return;
    await run(async()=>{
      const response=await mutate(`/api/v1/admin/wiki/tags/${selected}`,'DELETE',undefined,etag);
      if(!response.ok)throw Error(await errorFor(response));
      select('');await refresh();onStatus('Đã xóa tag không còn được sử dụng.');
    });
  }
  return <section className="category-manager" aria-labelledby="tags-heading">
    <h2 id="tags-heading">Quản lý Tag Wiki</h2>
    <p>Tag được sử dụng trong draft hoặc revision không thể vô hiệu hóa/xóa. Tag có lịch sử xuất bản cũng không thể đổi tên hoặc slug.</p>
    <div className="category-management-grid">
      <form className="form" onSubmit={create}>
        <h3>Thêm Tag</h3>
        <label>Tên<input required maxLength={80} value={newTag.name} onChange={e=>setNewTag(t=>({...t,name:e.target.value}))}/></label>
        <label>Slug<input required maxLength={80} value={newTag.slug} onChange={e=>setNewTag(t=>({...t,slug:e.target.value}))}/></label>
        <button type="submit" disabled={locked}>Tạo Tag</button>
      </form>
      <form className="form" onSubmit={update}>
        <h3>Sửa hoặc xóa Tag</h3>
        <label>Tag<select value={selected} onChange={e=>select(e.target.value)}>
          <option value="">Chọn tag</option>
          {tags.map(tag=><option key={tag.id} value={tag.id}>{tag.name}{tag.isActive?'':' (đã ẩn)'}</option>)}
        </select></label>
        {selected&&<>
          <label>Tên<input required maxLength={80} value={editing.name} onChange={e=>setEditing(t=>({...t,name:e.target.value}))}/></label>
          <label>Slug<input required maxLength={80} value={editing.slug} onChange={e=>setEditing(t=>({...t,slug:e.target.value}))}/></label>
          <label className="check-label"><input type="checkbox" checked={editing.isActive} onChange={e=>setEditing(t=>({...t,isActive:e.target.checked}))}/>Đang hoạt động</label>
          <div className="form-row"><button type="submit" disabled={locked||!etag}>Lưu Tag</button>
          <button type="button" className="danger" disabled={locked||!etag} onClick={remove}>Xóa Tag</button></div>
        </>}
      </form>
    </div>
  </section>;
}
