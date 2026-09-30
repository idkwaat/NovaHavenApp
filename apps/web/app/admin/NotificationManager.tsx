'use client';

import {useState, type FormEvent} from 'react';

type Mutate=(path:string,method:'POST'|'PATCH'|'DELETE',body?:object,precondition?:string)=>Promise<Response>;
type Props={mutate:Mutate;errorFor:(response:Response)=>Promise<string>;onStatus:(message:string)=>void;disabled:boolean};

export default function NotificationManager({mutate,errorFor,onStatus,disabled}:Props){
  const [title,setTitle]=useState('');
  const [body,setBody]=useState('');
  const [href,setHref]=useState('');
  const [sending,setSending]=useState(false);
  async function send(event:FormEvent<HTMLFormElement>){
    event.preventDefault();
    if(!window.confirm('Gửi thông báo vào hộp thư tất cả tài khoản người chơi? Thao tác này không thể thu hồi.'))return;
    setSending(true);
    try{
      const response=await mutate('/api/v1/admin/notifications','POST',{title,body,href:href.trim()||null});
      if(!response.ok)throw new Error(await errorFor(response));
      const result=await response.json() as {recipientCount:number;pushAttemptCount:number;pushSentCount:number};
      setTitle('');setBody('');setHref('');
      onStatus(`Đã lưu thông báo cho ${result.recipientCount} tài khoản; Web Push đã gửi ${result.pushSentCount}/${result.pushAttemptCount} thiết bị.`);
    }catch(error){onStatus(error instanceof Error?error.message:'Không gửi được thông báo.');}
    finally{setSending(false);}
  }
  return <section className="admin-section">
    <div className="admin-section-header"><div><h2>Thông báo người chơi</h2><p>Gửi vào hộp thư và thử Web Push tới các thiết bị đã đăng ký.</p></div></div>
    <form className="admin-form-grid" onSubmit={send}>
      <label>Tiêu đề<input required maxLength={120} value={title} onChange={event=>setTitle(event.target.value)}/></label>
      <label>Nội dung<textarea required maxLength={500} rows={4} value={body} onChange={event=>setBody(event.target.value)}/></label>
      <label>Đường dẫn nội bộ (tuỳ chọn)<input type="text" maxLength={200} placeholder="/news" value={href} onChange={event=>setHref(event.target.value)}/></label>
      <p>Người nhận: tài khoản người chơi đã đăng ký. Thông báo trong hộp thư vẫn lưu nếu thiết bị chưa bật Web Push.</p>
      <button type="submit" disabled={disabled||sending}>{sending?'Đang gửi…':'Gửi thông báo'}</button>
    </form>
  </section>;
}
