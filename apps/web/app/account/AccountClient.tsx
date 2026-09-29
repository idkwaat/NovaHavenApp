'use client';

import Link from 'next/link';
import {useEffect, useState, type FormEvent} from 'react';

type CurrentUser={id:string;email:string;emailConfirmed:boolean;isAdmin:boolean};
type LocalMail={confirmationUrl:string;createdAtUtc:string};

async function csrfToken(){
  const response=await fetch('/api/v1/auth/csrf',{credentials:'same-origin',cache:'no-store'});
  if(!response.ok)throw new Error('Không lấy được mã bảo vệ phiên. Hãy tải lại trang.');
  return (await response.json() as {token:string}).token;
}
async function request(path:string,body:unknown,method='POST'){
  const token=await csrfToken();
  const response=await fetch(path,{method,credentials:'same-origin',cache:'no-store',headers:{'Content-Type':'application/json','X-CSRF-TOKEN':token},body:JSON.stringify(body)});
  if(response.ok)return response;
  const problem=await response.json().catch(()=>null) as {detail?:string;title?:string;errors?:Record<string,string[]>}|null;
  const validation=problem?.errors?Object.values(problem.errors).flat()[0]:undefined;
  throw new Error(validation??problem?.detail??problem?.title??`Yêu cầu thất bại (HTTP ${response.status}).`);
}

export default function AccountClient(){
  const [mode,setMode]=useState<'login'|'register'>('login');
  const [email,setEmail]=useState('');
  const [password,setPassword]=useState('');
  const [busy,setBusy]=useState(false);
  const [message,setMessage]=useState('');
  const [error,setError]=useState('');
  const [user,setUser]=useState<CurrentUser|null>(null);
  const [mail,setMail]=useState<LocalMail|null>(null);
  const [mailLoading,setMailLoading]=useState(false);
  const [confirming,setConfirming]=useState(false);

  useEffect(()=>{
    let alive=true;
    void fetch('/api/v1/auth/me',{credentials:'same-origin',cache:'no-store'}).then(async response=>{
      if(response.ok&&alive)setUser(await response.json() as CurrentUser);
    }).catch(()=>{});
    const params=new URLSearchParams(window.location.search);
    const token=params.get('token');
    const targetEmail=params.get('email');
    if(token&&targetEmail){
      setEmail(targetEmail);setConfirming(true);
      void request('/api/v1/auth/confirm-email',{email:targetEmail,token}).then(()=>setMessage('Email đã được xác nhận. Bạn có thể đăng nhập.')).catch(reason=>setError(reason instanceof Error?reason.message:'Không thể xác nhận email.')).finally(()=>setConfirming(false));
      window.history.replaceState({},'',window.location.pathname);
    }
    return ()=>{alive=false;};
  },[]);

  async function submit(event:FormEvent<HTMLFormElement>){
    event.preventDefault();setBusy(true);setError('');setMessage('');
    try{
      if(mode==='register'){
        await request('/api/v1/auth/register',{email,password});
        setMessage('Nếu địa chỉ này hợp lệ, liên kết xác nhận đã được gửi. Hãy xác nhận email trước khi đăng nhập.');
      }else{
        await request('/api/v1/auth/login',{email,password});
        const response=await fetch('/api/v1/auth/me',{credentials:'same-origin',cache:'no-store'});
        if(response.ok)setUser(await response.json() as CurrentUser);
        setPassword('');setMessage('Đăng nhập thành công.');
      }
    }catch(reason){setError(reason instanceof Error?reason.message:'Có lỗi xảy ra. Vui lòng thử lại.');}
    finally{setBusy(false);}
  }

  async function resend(){
    setBusy(true);setError('');setMessage('');
    try{await request('/api/v1/auth/resend-confirmation',{email});setMessage('Nếu email thuộc tài khoản chưa xác nhận, liên kết mới đã được gửi.');}
    catch(reason){setError(reason instanceof Error?reason.message:'Không gửi được email xác nhận.');}
    finally{setBusy(false);}
  }
  async function loadLocalMail(){
    setMailLoading(true);setError('');
    try{
      const response=await fetch(`/api/v1/dev/mailbox?email=${encodeURIComponent(email)}`,{cache:'no-store'});
      if(!response.ok)throw new Error('Hộp thư local chỉ có trong môi trường Development.');
      const entries=await response.json() as LocalMail[];
      setMail(entries[0]??null);
      if(!entries.length)setMessage('Chưa có thư local cho địa chỉ này. Hãy đăng ký hoặc gửi lại liên kết.');
    }catch(reason){setError(reason instanceof Error?reason.message:'Không đọc được hộp thư local.');}
    finally{setMailLoading(false);}
  }
  async function logout(){
    setBusy(true);setError('');
    try{await request('/api/v1/auth/logout',{});setUser(null);setMessage('Bạn đã đăng xuất.');}
    catch(reason){setError(reason instanceof Error?reason.message:'Không thể đăng xuất.');}
    finally{setBusy(false);}
  }

  if(confirming)return <div className="account-card" role="status">Đang xác nhận email…</div>;
  if(user)return <div className="account-card account-signed-in">
    <p className="eyebrow">ĐÃ ĐĂNG NHẬP</p><h2>{user.email}</h2>
    <p>Email {user.emailConfirmed?'đã xác nhận':'chưa xác nhận'}{user.isAdmin?' · Quản trị viên':''}</p>
    <div className="account-actions"><Link className="account-primary" href="/notifications">Mở thông báo</Link>{user.isAdmin&&<Link className="account-secondary" href="/admin">Vào Nova CMS</Link>}<button className="account-secondary" type="button" onClick={logout} disabled={busy}>Đăng xuất</button></div>
    {message&&<p className="account-message" role="status">{message}</p>}{error&&<p className="account-error" role="alert">{error}</p>}
  </div>;

  return <div className="account-card">
    <div className="account-tabs" role="tablist" aria-label="Tác vụ tài khoản">
      <button type="button" role="tab" aria-selected={mode==='login'} onClick={()=>{setMode('login');setError('');setMessage('');}}>Đăng nhập</button>
      <button type="button" role="tab" aria-selected={mode==='register'} onClick={()=>{setMode('register');setError('');setMessage('');}}>Tạo tài khoản</button>
    </div>
    <form className="account-form" onSubmit={submit}>
      <label htmlFor="account-email">Email</label>
      <input id="account-email" name="email" type="email" autoComplete="email" required maxLength={256} value={email} onChange={event=>setEmail(event.target.value)}/>
      <label htmlFor="account-password">Mật khẩu</label>
      <input id="account-password" name="password" type="password" autoComplete={mode==='register'?'new-password':'current-password'} required minLength={mode==='register'?12:1} maxLength={128} value={password} onChange={event=>setPassword(event.target.value)}/>
      {mode==='register'&&<p className="account-hint">Ít nhất 12 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt. Cần xác nhận email trước lần đăng nhập đầu tiên.</p>}
      <button className="account-primary" type="submit" disabled={busy}>{busy?'Đang xử lý…':mode==='register'?'Tạo tài khoản và gửi email':'Đăng nhập'}</button>
    </form>
    {mode==='login'&&<div className="account-resend"><span>Chưa nhận được thư xác nhận?</span><button className="account-text-button" type="button" onClick={resend} disabled={busy||!email}>Gửi lại liên kết</button></div>}
    {mode==='register'&&<button className="account-text-button" type="button" onClick={loadLocalMail} disabled={mailLoading||!email}>{mailLoading?'Đang mở hộp thư…':'Mở hộp thư xác nhận local'}</button>}
    {mail&&<p className="account-message" role="status">Thư mới nhất lúc {new Date(mail.createdAtUtc).toLocaleString('vi-VN')}: <a href={mail.confirmationUrl}>Xác nhận email</a></p>}
    {message&&<p className="account-message" role="status">{message}</p>}{error&&<p className="account-error" role="alert">{error}</p>}
  </div>;
}
