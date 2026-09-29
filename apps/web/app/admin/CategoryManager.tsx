'use client';

import {useState} from 'react';
import {validateCategory, type CategoryInput} from '@/lib/wiki-category';

export type AdminCategory = CategoryInput & {id:string;etag:string};
type Mutation = (path:string, method:'POST'|'PATCH'|'DELETE', body?:object, precondition?:string)=>Promise<Response>;
type Props = {
  categories: AdminCategory[];
  mutate: Mutation;
  refresh: ()=>Promise<void>;
  onStatus: (value:string)=>void;
  errorFor: (response:Response)=>Promise<string>;
  disabled:boolean;
};
const blank:CategoryInput = {name:'',slug:'',displayOrder:0,isActive:true};

export default function CategoryManager({categories,mutate,refresh,onStatus,errorFor,disabled}:Props){
  const [newCategory,setNewCategory]=useState<CategoryInput>(blank);
  const [selected,setSelected]=useState<string|null>(null);
  const [editing,setEditing]=useState<CategoryInput>(blank);
  const [etag,setEtag]=useState<string|null>(null);
  const [busy,setBusy]=useState(false);
  const locked=disabled||busy;

  function selectCategory(id:string){
    const category=categories.find(item=>item.id===id);
    setSelected(category?.id??null);
    setEtag(category?.etag??null);
    setEditing(category?{name:category.name,slug:category.slug,
      displayOrder:category.displayOrder,isActive:category.isActive}:blank);
  }

  async function run(action:()=>Promise<void>){
    setBusy(true);
    try{await action();}
    catch(err){onStatus((err as Error).message);}
    finally{setBusy(false);}
  }

  async function create(event:React.FormEvent){
    event.preventDefault();
    const errors=validateCategory(newCategory);
    if(Object.keys(errors).length){onStatus(Object.values(errors).join(' · '));return;}
    await run(async()=>{
      const response=await mutate('/api/v1/admin/wiki/categories','POST',{
        name:newCategory.name,slug:newCategory.slug,displayOrder:newCategory.displayOrder
      });
      if(!response.ok)throw Error(await errorFor(response));
      setNewCategory(blank);
      await refresh();
      onStatus('Đã tạo danh mục Wiki.');
    });
  }

  async function update(event:React.FormEvent){
    event.preventDefault();
    if(!selected||!etag){onStatus('Thiếu ETag danh mục. Hãy tải lại danh sách.');return;}
    const errors=validateCategory(editing);
    if(Object.keys(errors).length){onStatus(Object.values(errors).join(' · '));return;}
    await run(async()=>{
      const response=await mutate(`/api/v1/admin/wiki/categories/${selected}`,'PATCH',editing,etag);
      if(!response.ok)throw Error(await errorFor(response));
      setSelected(null);setEtag(null);
      await refresh();
      onStatus('Đã cập nhật danh mục.');
    });
  }

  async function remove(){
    if(!selected||!etag){onStatus('Thiếu ETag danh mục. Hãy tải lại danh sách.');return;}
    const category=categories.find(item=>item.id===selected);
    if(!window.confirm(`Xóa danh mục "${category?.name??''}"? Chỉ được xóa khi chưa có bài viết hoặc revision tham chiếu.`))return;
    await run(async()=>{
      const response=await mutate(`/api/v1/admin/wiki/categories/${selected}`,'DELETE',undefined,etag);
      if(!response.ok)throw Error(await errorFor(response));
      setSelected(null);setEtag(null);
      await refresh();
      onStatus('Đã xóa danh mục không còn được sử dụng.');
    });
  }

  return <section aria-labelledby="category-heading" className="category-manager">
    <h2 id="category-heading">Quản lý danh mục Wiki</h2>
    <p>Danh mục đang được bài viết hoặc phiên bản cũ sử dụng không thể vô hiệu hóa hay xóa. Đổi tên hoặc slug đã xuất hiện trong revision cũng bị khóa.</p>
    <div className="category-management-grid">
      <form className="form" onSubmit={create}>
        <h3>Thêm danh mục</h3>
        <label>Tên<input required maxLength={80} value={newCategory.name}
          onChange={e=>setNewCategory(current=>({...current,name:e.target.value}))}/></label>
        <label>Slug<input required maxLength={80} value={newCategory.slug}
          onChange={e=>setNewCategory(current=>({...current,slug:e.target.value}))}/></label>
        <label>Thứ tự<input type="number" min={0} max={10000} required value={newCategory.displayOrder}
          onChange={e=>setNewCategory(current=>({...current,displayOrder:Number(e.target.value)}))}/></label>
        <button disabled={locked} type="submit">Tạo danh mục</button>
      </form>
      <form className="form" onSubmit={update}>
        <h3>Sửa / sắp xếp / ẩn danh mục</h3>
        <label>Danh mục<select value={selected??''} onChange={e=>selectCategory(e.target.value)}>
          <option value="">Chọn danh mục</option>
          {categories.map(category=><option value={category.id} key={category.id}>
            {category.name}{category.isActive?'':' (đã ẩn)'}
          </option>)}
        </select></label>
        {selected&&<>
          <label>Tên<input required maxLength={80} value={editing.name}
            onChange={e=>setEditing(current=>({...current,name:e.target.value}))}/></label>
          <label>Slug<input required maxLength={80} value={editing.slug}
            onChange={e=>setEditing(current=>({...current,slug:e.target.value}))}/></label>
          <label>Thứ tự<input type="number" min={0} max={10000} required value={editing.displayOrder}
            onChange={e=>setEditing(current=>({...current,displayOrder:Number(e.target.value)}))}/></label>
          <label className="check-label"><input type="checkbox" checked={editing.isActive}
            onChange={e=>setEditing(current=>({...current,isActive:e.target.checked}))}/>Đang hoạt động</label>
          <div className="form-row">
            <button type="submit" disabled={locked||!etag}>Lưu thay đổi</button>
            <button type="button" className="danger" disabled={locked||!etag} onClick={remove}>Xóa danh mục</button>
          </div>
        </>}
      </form>
    </div>
  </section>;
}
