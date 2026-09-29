'use client';

import Link from 'next/link';
import {useEffect, useState, type FormEvent} from 'react';

type CurrentUser={id:string;email:string;isAdmin:boolean};
type ApiProblem={detail?:string;title?:string;errors?:Record<string,string[]>};

async function csrfToken(){
  const response=await fetch('/api/v1/auth/csrf',{credentials:'same-origin',cache:'no-store'});
  if(!response.ok)throw new Error('Không lấy được mã bảo vệ phiên. Hãy tải lại trang.');
  return (await response.json() as {token:string}).token;
}
async function request(path:string,body:unknown){
  const token=await csrfToken();
  const response=await fetch(path,{method:'POST',credentials:'same-origin',cache:'no-store',headers:{'Content-Type':'application/json','X-CSRF-TOKEN':token},body:JSON.stringify(body)});
  if(response.ok)return response;
  const problem=await response.json().catch(()=>null) as ApiProblem|null;
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

  useEffect(()=>{
    let alive=true;
    void fetch('/api/v1/auth/me',{credentials:'same-origin',cache:'no-store'}).then(async response=>{
      if(response.ok&&alive)setUser(await response.json() as CurrentUser);
    }).catch(()=>{});
    return ()=>{alive=false;};
  },[]);

  async function submit(event:FormEvent<HTMLFormElement>){
    event.preventDefault();setBusy(true);setError('');setMessage('');
    try{
      if(mode==='register'){
        const response=await request('/api/v1/auth/register',{email,password});
        setUser(await response.json() as CurrentUser);
        setPassword('');
        setMessage('Tài khoản đã sẵn sàng. Bạn đang đăng nhập vào Nova Haven.');
      }else{
        await request('/api/v1/auth/login',{email,password});
        const response=await fetch('/api/v1/auth/me',{credentials:'same-origin',cache:'no-store'});
        if(!response.ok)throw new Error('Đăng nhập thành công nhưng chưa đọc được phiên. Hãy tải lại trang.');
        setUser(await response.json() as CurrentUser);
        setPassword('');setMessage('Đăng nhập thành công.');
      }
    }catch(reason){setError(reason instanceof Error?reason.message:'Có lỗi xảy ra. Vui lòng thử lại.');}
    finally{setBusy(false);}
  }

  async function logout(){
    setBusy(true);setError('');setMessage('');
    try{await request('/api/v1/auth/logout',{});setUser(null);setMessage('Bạn đã đăng xuất.');}
    catch(reason){setError(reason instanceof Error?reason.message:'Không thể đăng xuất.');}
    finally{setBusy(false);}
  }

  if(user)return <div className="account-card account-signed-in">
    <p className="eyebrow">ĐÃ ĐĂNG NHẬP</p><h2>{user.email}</h2>
    <p>{user.isAdmin?'Tài khoản quản trị viên':'Tài khoản người chơi'}{user.isAdmin?' · có quyền Nova CMS':''}</p>
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
      {mode==='register'&&<p className="account-hint">Tối thiểu 12 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt. Tạo xong là có thể sử dụng ngay.</p>}
      <button className="account-primary" type="submit" disabled={busy}>{busy?'Đang xử lý…':mode==='register'?'Tạo tài khoản':'Đăng nhập'}</button>
    </form>
    {message&&<p className="account-message" role="status">{message}</p>}{error&&<p className="account-error" role="alert">{error}</p>}
  </div>;
}
