'use client';

import Link from 'next/link';
import {useEffect,useRef,useState} from 'react';
import {readCart,type CartLine} from '@/lib/commerce-cart';
import {formatVnd} from '@/lib/currency';

type Offer={slug:string;name:string;isPurchasable:boolean;priceMinorUnits:number|null};
type Receipt={orderNumber:string;createdAt:string;status:string;totalMinorUnits:number;currencyCode:string;paymentStatus:string;realCharge:boolean;fulfilment:string;idempotentReplay:boolean;message:string;items:{name:string;quantity:number;lineTotalMinorUnits:number}[]};
const CART_KEY='nova-haven-cart-v1';
const ATTEMPT_KEY='nova-haven-checkout-attempt-v1';

function fingerprint(cart:CartLine[]){return cart.slice().sort((a,b)=>a.slug.localeCompare(b.slug)).map(row=>`${row.slug}:${row.quantity}`).join('|');}

async function responseMessage(response:Response){
 try{
  const data=await response.json() as {detail?:string;title?:string;errors?:Record<string,string[]>};
  const validation=Object.values(data.errors??{}).flat().join(' ');
  return data.detail??data.title??(validation||`Yêu cầu thất bại (${response.status}).`);
 }catch{return `Yêu cầu thất bại (${response.status}).`;}
}

export default function CheckoutClient(){
 const [cart,setCart]=useState<CartLine[]>([]);
 const [offers,setOffers]=useState<Offer[]>([]);
 const [loading,setLoading]=useState(true);
 const [busy,setBusy]=useState(false);
 const [error,setError]=useState('');
 const [receipt,setReceipt]=useState<Receipt|null>(null);
 const pendingAttempt=useRef<{fingerprint:string;key:string}|null>(null);
 const errorRef=useRef<HTMLParagraphElement|null>(null);

 useEffect(()=>{
  setCart(readCart(localStorage.getItem(CART_KEY)));
  fetch('/api/v1/commerce/offers?page=1&pageSize=50',{cache:'no-store'}).then(async response=>{
   if(!response.ok)throw new Error('Không tải được giá tham khảo hiện tại.');
   const data=await response.json() as {items:Offer[]};
   setOffers(data.items);
  }).catch(reason=>setError((reason as Error).message)).finally(()=>setLoading(false));
 },[]);

 useEffect(()=>{if(error)errorRef.current?.focus();},[error]);

 const items=cart.map(line=>({line,offer:offers.find(item=>item.slug===line.slug)}));
 const total=items.reduce((sum,{line,offer})=>sum+(offer?.isPurchasable&&offer.priceMinorUnits?line.quantity*offer.priceMinorUnits:0),0);
 const unavailable=items.some(({offer})=>!offer||!offer.isPurchasable||!offer.priceMinorUnits);

 async function submit(event:React.FormEvent){
  event.preventDefault();
  if(busy||!cart.length||unavailable)return;
  setBusy(true);
  setError('');
  try{
   const cartFingerprint=fingerprint(cart);
   let idempotencyKey:string;
   if(pendingAttempt.current?.fingerprint===cartFingerprint)idempotencyKey=pendingAttempt.current.key;
   else try{
    const saved=JSON.parse(sessionStorage.getItem(ATTEMPT_KEY)??'null') as {fingerprint?:string;key?:string}|null;
    idempotencyKey=saved?.fingerprint===cartFingerprint&&saved.key?saved.key:crypto.randomUUID();
    pendingAttempt.current={fingerprint:cartFingerprint,key:idempotencyKey};
    if(saved?.fingerprint!==cartFingerprint||!saved?.key)sessionStorage.setItem(ATTEMPT_KEY,JSON.stringify(pendingAttempt.current));
   }catch{
    idempotencyKey=crypto.randomUUID();
    pendingAttempt.current={fingerprint:cartFingerprint,key:idempotencyKey};
   }

   const response=await fetch('/api/v1/commerce/orders',{
    method:'POST',
    credentials:'same-origin',
    headers:{'Content-Type':'application/json','Idempotency-Key':idempotencyKey},
    body:JSON.stringify({items:cart.map(({slug,quantity})=>({slug,quantity}))}),
   });
   if(!response.ok)throw new Error(await responseMessage(response));
   const completed=await response.json() as Receipt;
   if(completed.realCharge||completed.fulfilment!=='none'||completed.paymentStatus!=='simulated'){
    throw new Error('Backend không trả về biên nhận mô phỏng an toàn; giỏ hàng vẫn được giữ lại.');
   }
   setReceipt(completed);
   localStorage.removeItem(CART_KEY);
   sessionStorage.removeItem(ATTEMPT_KEY);
   window.dispatchEvent(new Event('nova-haven-cart-change'));
  }catch(reason){
   setError((reason as Error).message||'Không thể tạo đơn mô phỏng. Hãy thử lại.');
  }finally{setBusy(false);}
 }

 const demoNotice=<aside className="shop-notice" aria-label="Giới hạn đơn hàng mô phỏng">
  <strong>Đây chỉ là quy trình mô phỏng local</strong>
  <span>Không thu tiền thật, không cần cung cấp thông tin cá nhân và không giao vật phẩm hoặc quyền lợi trong game.</span>
 </aside>;

 if(receipt){
  return <section className="content shop-page checkout-page">
   <p className="eyebrow">NOVA HAVEN · ĐƠN HÀNG DEMO</p>
   <div className="receipt-card">
    <span className="receipt-mark" aria-hidden="true">✓</span>
    <h1>Đã tạo đơn mô phỏng</h1>
    <p>Đơn tham chiếu <strong>#{receipt.orderNumber}</strong> đã được lưu trong cơ sở dữ liệu local. Đây không phải giao dịch mua bán thật.</p>
    {demoNotice}
    <dl>
     <div><dt>Mã tham chiếu</dt><dd>{receipt.orderNumber}</dd></div>
     <div><dt>Thời điểm tạo</dt><dd><time dateTime={receipt.createdAt}>{new Date(receipt.createdAt).toLocaleString('vi-VN',{timeZone:'UTC'})} UTC</time></dd></div>
     <div><dt>Giá trị tham khảo</dt><dd>{formatVnd(receipt.totalMinorUnits)}</dd></div>
     <div><dt>Thu tiền</dt><dd>{receipt.realCharge?'Có':'Không thu tiền thật'}</dd></div>
     <div><dt>Giao vật phẩm trong game</dt><dd>{receipt.fulfilment==='none'?'Không giao':'Không áp dụng'}</dd></div>
    </dl>
    <p>{receipt.message}</p>
    <h2>Danh sách trong đơn mô phỏng</h2>
    <ul>{receipt.items.map((item,index)=><li key={`${item.name}-${index}`}>
     <span>{item.name} × {item.quantity}</span>
     <strong>{formatVnd(item.lineTotalMinorUnits)}</strong>
    </li>)}</ul>
    <div className="receipt-actions">
     <Link className="shop-primary-link" href="/commerce">Quay lại gian hàng demo</Link>
     <Link className="shop-secondary-link" href="/">Về trang chủ</Link>
     <Link className="shop-secondary-link" href="/community">Mở trang cộng đồng</Link>
    </div>
   </div>
  </section>;
 }

 return <section className="content shop-page checkout-page">
  <p className="eyebrow">NOVA HAVEN · XÁC NHẬN GIỎ</p>
  <h1>Tạo đơn mô phỏng</h1>
  <p className="shop-lede">Kiểm tra các mục và tạo bản ghi thử nghiệm local; đây không phải bước thanh toán.</p>
  {demoNotice}
  {error&&<p ref={errorRef} tabIndex={-1} className="shop-error" role="alert">{error}</p>}

  {loading?<p className="notice" role="status">Đang tải giá tham khảo…</p>:!cart.length?<div className="cart-empty"><h2>Giỏ hàng đang trống</h2><Link className="shop-primary-link" href="/commerce">Xem gian hàng demo</Link></div>:<>
   <form onSubmit={submit} className="checkout-main-grid" aria-busy={busy}>
    <div className="checkout-left-col">
     <section className="checkout-section" aria-labelledby="demo-items-title">
      <h2 id="demo-items-title">Các mục trong đơn</h2>
      <div className="checkout-lines">
       {items.map(({line,offer})=><div className="checkout-line" key={line.slug}>
        <span>{offer?.name??line.slug} <small>× {line.quantity}</small></span>
        <strong>{offer?.isPurchasable&&offer.priceMinorUnits?formatVnd(offer.priceMinorUnits*line.quantity):'Không khả dụng'}</strong>
       </div>)}
      </div>
      {unavailable&&<p className="shop-error" role="alert">Có mục đã ngừng bán hoặc chưa có giá. Hãy quay lại giỏ để cập nhật.</p>}
     </section>
    </div>
    <aside className="checkout-summary-card">
     <h2>Tóm tắt đơn demo</h2>
     <div className="checkout-bill">
      <div className="bill-row"><span>Giá trị tham khảo</span><strong>{formatVnd(total)}</strong></div>
      <div className="bill-row"><span>Thu tiền</span><strong>Không</strong></div>
      <div className="bill-row"><span>Giao vật phẩm</span><strong>Không</strong></div>
      <div className="bill-row bill-total"><span>Tổng tham khảo</span><strong className="bill-total-amount">{formatVnd(total)}</strong></div>
     </div>
     <div className="checkout-actions-panel">
      <button className="shop-primary-link btn-block" type="submit" disabled={busy||unavailable}>
       {busy?'Đang tạo đơn mô phỏng…':'Tạo đơn mô phỏng'}
      </button>
      <Link className="shop-secondary-link" href="/cart">← Quay lại giỏ hàng</Link>
     </div>
    </aside>
   </form>
  </>}
 </section>;
}
