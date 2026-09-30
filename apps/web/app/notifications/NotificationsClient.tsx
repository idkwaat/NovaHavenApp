'use client';

import Link from 'next/link';
import {useCallback, useEffect, useState} from 'react';

type Item={id:string;title:string;body:string;href:string|null;createdAtUtc:string;readAtUtc:string|null};
type Inbox={items:Item[];page:number;pageSize:number;total:number;unreadCount:number};

async function csrfToken(){
  const response=await fetch('/api/v1/auth/csrf',{credentials:'same-origin',cache:'no-store'});
  if(!response.ok)throw new Error('Không lấy được mã bảo vệ phiên.');
  return (await response.json() as {token:string}).token;
}
async function mutate(url:string,method:string,body?:unknown){
  const token=await csrfToken();
  const response=await fetch(url,{method,credentials:'same-origin',cache:'no-store',headers:{'X-CSRF-TOKEN':token,...(body?{'Content-Type':'application/json'}:{})},...(body?{body:JSON.stringify(body)}:{})});
  if(!response.ok)throw new Error(response.status===401?'Hãy đăng nhập để xem thông báo.':`Không thể hoàn tất yêu cầu (HTTP ${response.status}).`);
  return response;
}
function decodeKey(value:string){
  const padded=value.replace(/-/g,'+').replace(/_/g,'/')+'='.repeat((4-value.length%4)%4);
  return Uint8Array.from(atob(padded),character=>character.charCodeAt(0));
}

export default function NotificationsClient(){
  const [inbox,setInbox]=useState<Inbox|null>(null);
  const [page,setPage]=useState(1);
  const [signedIn,setSignedIn]=useState<boolean|null>(null);
  const [error,setError]=useState('');
  const [message,setMessage]=useState('');
  const [busy,setBusy]=useState(false);
  const [pushSupported,setPushSupported]=useState(false);
  const [pushEnabled,setPushEnabled]=useState(false);
  const [pushReady,setPushReady]=useState(false);
  const refresh=useCallback(async(requestedPage=1)=>{
    const response=await fetch(`/api/v1/notifications?page=${requestedPage}&pageSize=20`,{credentials:'same-origin',cache:'no-store'});
    if(response.status===401){setSignedIn(false);setInbox(null);return;}
    if(!response.ok)throw new Error(`Thông báo không tải được (HTTP ${response.status}).`);
    setSignedIn(true);setError('');setPage(requestedPage);setInbox(await response.json() as Inbox);
  },[]);
  useEffect(()=>{
    let alive=true;
    setPushSupported('serviceWorker' in navigator&&'PushManager' in window&&'Notification' in window);
    void refresh().catch(reason=>{if(alive)setError(reason instanceof Error?reason.message:'Không tải được thông báo.');});
    if('serviceWorker' in navigator&&'PushManager' in window){
      void navigator.serviceWorker.register('/sw.js').then(async registration=>{
        const subscription=await registration.pushManager.getSubscription();
        if(alive){setPushEnabled(subscription!==null);setPushReady(true);}
      }).catch(()=>{if(alive)setPushReady(true);});
    }else setPushReady(true);
    return()=>{alive=false;};
  },[refresh]);

  async function run(action:()=>Promise<void>){setBusy(true);setError('');setMessage('');try{await action();}catch(reason){setError(reason instanceof Error?reason.message:'Có lỗi xảy ra.');}finally{setBusy(false);}}
  async function markRead(id:string){
    await mutate(`/api/v1/notifications/${encodeURIComponent(id)}/read`,'PUT');
    setInbox(current=>current?{...current,items:current.items.map(item=>item.id===id?{...item,readAtUtc:new Date().toISOString()}:item),unreadCount:Math.max(0,current.unreadCount-1)}:current);
  }
  async function markAll(){await mutate('/api/v1/notifications/read-all','POST');await refresh(page);}
  async function enablePush(){
    if(!pushSupported)throw new Error('Trình duyệt này không hỗ trợ Web Push.');
    const permission=await Notification.requestPermission();
    if(permission!=='granted')throw new Error('Bạn chưa cấp quyền gửi thông báo cho trình duyệt.');
    const configResponse=await fetch('/api/v1/notifications/push/config',{credentials:'same-origin',cache:'no-store'});
    if(!configResponse.ok)throw new Error('Không tải được cấu hình Web Push.');
    const {publicKey}=await configResponse.json() as {publicKey:string|null};
    if(!publicKey)throw new Error('Web Push chưa được bật trên API local.');
    const registration=await navigator.serviceWorker.register('/sw.js');
    const subscription=await registration.pushManager.subscribe({userVisibleOnly:true,applicationServerKey:decodeKey(publicKey)});
    const json=subscription.toJSON();
    if(!json.endpoint||!json.keys?.p256dh||!json.keys.auth)throw new Error('Trình duyệt trả về subscription không hợp lệ.');
    await mutate('/api/v1/notifications/push/subscriptions','PUT',{endpoint:json.endpoint,keys:{p256dh:json.keys.p256dh,auth:json.keys.auth}});
    setPushEnabled(true);setMessage('Đã bật thông báo trên trình duyệt này.');
  }
  async function disablePush(){
    const registration=await navigator.serviceWorker.ready;
    const subscription=await registration.pushManager.getSubscription();
    if(subscription){await mutate('/api/v1/notifications/push/subscriptions','DELETE',{endpoint:subscription.endpoint});await subscription.unsubscribe();}
    setPushEnabled(false);setMessage('Đã tắt thông báo trên trình duyệt này.');
  }

  if(signedIn===null)return <div className="account-card" role="status">{error?<><p className="account-error" role="alert">{error}</p><button className="account-secondary" type="button" onClick={()=>void refresh().catch(reason=>setError(reason instanceof Error?reason.message:'Không tải được thông báo.'))}>Thử kết nối lại</button></>:'Đang kiểm tra phiên đăng nhập…'}</div>;
  if(!signedIn)return <div className="account-card"><h2>Cần đăng nhập</h2><p>Hộp thư và Web Push được gắn với tài khoản của bạn.</p><Link className="account-primary" href="/account">Đăng nhập hoặc tạo tài khoản</Link>{error&&<p className="account-error" role="alert">{error}</p>}</div>;
  return <div className="account-card notification-card">
    <div className="notification-toolbar"><p>{inbox?.unreadCount??0} tin chưa đọc · {inbox?.total??0} thông báo</p><div>
      <button className="account-secondary" type="button" disabled={busy||!inbox?.unreadCount} onClick={()=>void run(markAll)}>Đánh dấu đã đọc</button>
      {pushSupported&&<button className="account-secondary" type="button" disabled={busy||!pushReady} onClick={()=>void run(pushEnabled?disablePush:enablePush)}>{pushEnabled?'Tắt Web Push':'Bật Web Push'}</button>}
    </div></div>
    {pushSupported&&<p className="account-hint">Web Push cần quyền trình duyệt và khóa VAPID được API cấu hình. Thông báo trong hộp thư vẫn dùng được nếu chưa bật push.</p>}
    {message&&<p className="account-message" role="status">{message}</p>}{error&&<p className="account-error" role="alert">{error}</p>}
    {!inbox?.items.length?<div className="notification-empty"><h2>{inbox?.total ? 'Không có thông báo ở trang này' : 'Chưa có thông báo'}</h2><p>Tin từ Nova Haven sẽ xuất hiện tại đây.</p></div>:<ul className="notification-list">{inbox.items.map(item=><li className={item.readAtUtc?'notification-read':'notification-unread'} key={item.id}>
      <div><h2>{item.title}</h2><p>{item.body}</p><time dateTime={item.createdAtUtc}>{new Date(item.createdAtUtc).toLocaleString('vi-VN')}</time></div>
      <div className="notification-actions">{item.href&&<Link className="account-secondary" href={item.href} onClick={()=>{if(!item.readAtUtc)void run(()=>markRead(item.id));}}>Mở</Link>}{!item.readAtUtc&&<button type="button" className="account-text-button" disabled={busy} onClick={()=>void run(()=>markRead(item.id))}>Đánh dấu đã đọc</button>}</div>
    </li>)}</ul>}
    {inbox&&inbox.total>inbox.pageSize&&<nav className="notification-pager" aria-label="Phân trang thông báo">
      <button className="account-secondary" type="button" disabled={busy||page<=1} onClick={()=>void run(()=>refresh(page-1))}>← Trang trước</button>
      <span aria-live="polite">Trang {page} / {Math.ceil(inbox.total/inbox.pageSize)}</span>
      <button className="account-secondary" type="button" disabled={busy||page>=Math.ceil(inbox.total/inbox.pageSize)} onClick={()=>void run(()=>refresh(page+1))}>Trang tiếp →</button>
    </nav>}
  </div>;
}
