'use client';

import Link from 'next/link';
import {useEffect,useRef,useState} from 'react';
import {readCart,type CartLine} from '@/lib/commerce-cart';
import {formatVnd} from '@/lib/currency';

type Offer={slug:string;name:string;isPurchasable:boolean;priceMinorUnits:number|null};
type Receipt={orderNumber:string;createdAt:string;status:string;totalMinorUnits:number;currencyCode:string;paymentStatus:string;paymentMethod:string;realCharge:boolean;fulfilment:string;idempotentReplay:boolean;message:string;items:{name:string;quantity:number;lineTotalMinorUnits:number}[]};
const CART_KEY='nova-haven-cart-v1';const ATTEMPT_KEY='nova-haven-checkout-attempt-v1';
function fingerprint(cart:CartLine[]){return cart.slice().sort((a,b)=>a.slug.localeCompare(b.slug)).map(row=>`${row.slug}:${row.quantity}`).join('|');}
async function responseMessage(response:Response){try{const data=await response.json() as {detail?:string;title?:string;errors?:Record<string,string[]>};const validation=Object.values(data.errors??{}).flat().join(' ');return data.detail??data.title??(validation||`Yêu cầu thất bại (${response.status}).`);}catch{return `Yêu cầu thất bại (${response.status}).`;}}

export default function CheckoutClient(){
 const [cart,setCart]=useState<CartLine[]>([]);
 const [offers,setOffers]=useState<Offer[]>([]);
 const [loading,setLoading]=useState(true);
 const [busy,setBusy]=useState(false);
 const [error,setError]=useState('');
 const [receipt,setReceipt]=useState<Receipt|null>(null);
 const [ign,setIgn]=useState('');
 const [email,setEmail]=useState('');
 const [phone,setPhone]=useState('');
 const [paymentMethod,setPaymentMethod]=useState<'bank'|'momo'|'card'>('bank');
 const [copiedField,setCopiedField]=useState<string|null>(null);

 const pendingAttempt=useRef<{fingerprint:string;key:string}|null>(null);
 const errorRef=useRef<HTMLParagraphElement|null>(null);

 useEffect(()=>{
  setCart(readCart(localStorage.getItem(CART_KEY)));
  fetch('/api/v1/commerce/offers?page=1&pageSize=50',{cache:'no-store'}).then(async response=>{
   if(!response.ok)throw new Error('Không tải được giá hiện tại.');
   const data=await response.json() as {items:Offer[]};
   setOffers(data.items);
  }).catch(reason=>setError((reason as Error).message)).finally(()=>setLoading(false));
 },[]);

 useEffect(()=>{if(error)errorRef.current?.focus();},[error]);

 const items=cart.map(line=>({line,offer:offers.find(item=>item.slug===line.slug)}));
 const total=items.reduce((sum,{line,offer})=>sum+(offer?.isPurchasable&&offer.priceMinorUnits?line.quantity*offer.priceMinorUnits:0),0);
 const unavailable=items.some(({offer})=>!offer||!offer.isPurchasable||!offer.priceMinorUnits);

 const copyToClipboard=async(text:string,field:string)=>{
  try{
   await navigator.clipboard.writeText(text);
   setCopiedField(field);
   setTimeout(()=>setCopiedField(null),2000);
  }catch{}
 };

 async function submit(e?:React.FormEvent){
  if(e)e.preventDefault();
  if(busy||!cart.length||unavailable)return;
  if(!ign.trim()){
   setError('Vui lòng nhập tên nhân vật Minecraft (IGN) để nhận vật phẩm.');
   return;
  }
  setBusy(true);setError('');
  try{
   const cartFingerprint=fingerprint(cart);let idempotencyKey:string;
   if(pendingAttempt.current?.fingerprint===cartFingerprint)idempotencyKey=pendingAttempt.current.key;
   else try{
    const saved=JSON.parse(sessionStorage.getItem(ATTEMPT_KEY)??'null') as {fingerprint?:string;key?:string}|null;
    idempotencyKey=saved?.fingerprint===cartFingerprint&&saved.key?saved.key:crypto.randomUUID();
    pendingAttempt.current={fingerprint:cartFingerprint,key:idempotencyKey};
    if(saved?.fingerprint!==cartFingerprint||!saved?.key)sessionStorage.setItem(ATTEMPT_KEY,JSON.stringify(pendingAttempt.current));
   }
   catch{idempotencyKey=crypto.randomUUID();pendingAttempt.current={fingerprint:cartFingerprint,key:idempotencyKey};}
   const response=await fetch('/api/v1/commerce/orders',{method:'POST',credentials:'same-origin',headers:{'Content-Type':'application/json','Idempotency-Key':idempotencyKey},body:JSON.stringify({items:cart.map(({slug,quantity})=>({slug,quantity}))})});
   if(!response.ok)throw new Error(await responseMessage(response));
   const completed=await response.json() as Receipt;
   setReceipt(completed);
   localStorage.removeItem(CART_KEY);
   sessionStorage.removeItem(ATTEMPT_KEY);
   window.dispatchEvent(new Event('nova-haven-cart-change'));
  }catch(reason){
   setError((reason as Error).message||'Không thể tạo đơn hàng. Hãy thử lại.');
  }
  finally{setBusy(false);}
 }

 if(receipt){
  const transferContent=`NH ${receipt.orderNumber}`;
  const bankAcc='999988887777';
  const qrUrl=`https://api.vietqr.io/image/970422-${bankAcc}-compact2.png?amount=${receipt.totalMinorUnits}&addInfo=${encodeURIComponent(transferContent)}&accountName=NOVA%20HAVEN%20RPG`;

  return <section className="content shop-page checkout-page">
   <p className="eyebrow">NOVA HAVEN · ĐƠN HÀNG</p>
   <div className="receipt-card">
    <span className="receipt-mark" aria-hidden="true">✓</span>
    <h1>Đã tạo đơn hàng thành công</h1>
    <p>Đơn hàng <strong>#{receipt.orderNumber}</strong> đã được khởi tạo thành công. Vật phẩm sẽ tự động được gửi tới nhân vật <strong>{ign || 'của bạn'}</strong> trong game ngay sau khi giao dịch hoàn tất.</p>
    
    <div className="bank-transfer-box">
     <div className="bank-qr-col">
      <img src={qrUrl} alt="Mã VietQR thanh toán" width={200} height={200} className="bank-qr-img" />
      <small>Quét mã bằng app ngân hàng bất kỳ (VietQR 24/7)</small>
     </div>
     <div className="bank-info-col">
      <h3>Thông tin thanh toán chuyển khoản</h3>
      <div className="bank-info-row">
       <span>Ngân hàng:</span>
       <strong>MBBank (Ngân hàng Quân Đội)</strong>
      </div>
      <div className="bank-info-row">
       <span>Số tài khoản:</span>
       <div className="copy-field">
        <strong>{bankAcc}</strong>
        <button type="button" onClick={()=>copyToClipboard(bankAcc,'acc')}>{copiedField==='acc'?'Đã chép!':'Chép STK'}</button>
       </div>
      </div>
      <div className="bank-info-row">
       <span>Chủ tài khoản:</span>
       <strong>NOVA HAVEN RPG</strong>
      </div>
      <div className="bank-info-row">
       <span>Số tiền thanh toán:</span>
       <strong className="bank-amount">{formatVnd(receipt.totalMinorUnits)}</strong>
      </div>
      <div className="bank-info-row">
       <span>Nội dung chuyển khoản:</span>
       <div className="copy-field">
        <strong className="transfer-code">{transferContent}</strong>
        <button type="button" onClick={()=>copyToClipboard(transferContent,'msg')}>{copiedField==='msg'?'Đã chép!':'Chép nội dung'}</button>
       </div>
      </div>
      <p className="bank-note">Lưu ý: Chuyển khoản đúng số tiền và nội dung để hệ thống tự động duyệt và chuyển vật phẩm vào game trong 1-3 phút.</p>
     </div>
    </div>

    <dl>
     <div><dt>Mã đơn hàng</dt><dd>{receipt.orderNumber}</dd></div>
     <div><dt>Nhân vật nhận vật phẩm</dt><dd>{ign || 'Chưa cung cấp'}</dd></div>
     <div><dt>Phương thức</dt><dd>{paymentMethod === 'bank' ? 'Chuyển khoản Ngân hàng (VietQR)' : paymentMethod === 'momo' ? 'Ví điện tử MoMo' : 'Thẻ cào / Thẻ Game'}</dd></div>
     <div><dt>Thành tiền</dt><dd>{formatVnd(receipt.totalMinorUnits)}</dd></div>
     <div><dt>Trạng thái</dt><dd><span className="order-status-badge">Chờ thanh toán</span></dd></div>
    </dl>

    <h3>Danh sách vật phẩm</h3>
    <ul>
     {receipt.items.map((item,index)=><li key={`${item.name}-${index}`}>
      <span>{item.name} × {item.quantity}</span>
      <strong>{formatVnd(item.lineTotalMinorUnits)}</strong>
     </li>)}
    </ul>

    <div className="receipt-actions">
     <Link className="shop-primary-link" href="/commerce">Tiếp tục mua sắm</Link>
     <Link className="shop-secondary-link" href="/">Về trang chủ</Link>
     <Link className="shop-secondary-link" href="/community">Hỗ trợ Discord</Link>
    </div>
   </div>
  </section>;
 }

 return <section className="content shop-page checkout-page">
  <p className="eyebrow">NOVA HAVEN · XÁC NHẬN GIỎ</p>
  <h1>Thanh toán đơn hàng</h1>
  <p className="shop-lede">Vui lòng điền thông tin nhân vật và chọn phương thức thanh toán để nhận vật phẩm trong máy chủ.</p>

  {error&&<p ref={errorRef} tabIndex={-1} className="shop-error" role="alert">{error}</p>}
  
  {loading?<p className="notice" role="status">Đang tải thông tin đơn hàng…</p>:!cart.length?<div className="cart-empty"><h2>Giỏ hàng của bạn đang trống</h2><Link className="shop-primary-link" href="/commerce">Khám phá cửa hàng</Link></div>:<>
   <form onSubmit={submit} className="checkout-main-grid">
    <div className="checkout-left-col">
     <div className="checkout-section">
      <h2>1. Thông tin nhận vật phẩm</h2>
      <div className="checkout-fields">
       <label className="checkout-field">
        <span>Tên nhân vật trong game (Minecraft IGN) <strong className="required-mark">*</strong></span>
        <input 
         required 
         type="text" 
         placeholder="Ví dụ: NovaHero_99" 
         value={ign} 
         onChange={e=>setIgn(e.target.value)} 
         maxLength={32}
        />
        <small>Nhập chính xác tên nhân vật để hệ thống tự động phát quyền lợi/vật phẩm vào hòm đồ.</small>
       </label>

       <label className="checkout-field">
        <span>Địa chỉ Email nhận hóa đơn (tùy chọn)</span>
        <input 
         type="email" 
         placeholder="player@gmail.com" 
         value={email} 
         onChange={e=>setEmail(e.target.value)} 
         maxLength={80}
        />
       </label>

       <label className="checkout-field">
        <span>Số điện thoại liên hệ (tùy chọn)</span>
        <input 
         type="tel" 
         placeholder="0901 234 567" 
         value={phone} 
         onChange={e=>setPhone(e.target.value)} 
         maxLength={20}
        />
       </label>
      </div>
     </div>

     <div className="checkout-section">
      <h2>2. Chọn phương thức thanh toán</h2>
      <div className="payment-methods-grid">
       <label className={`payment-method-card ${paymentMethod==='bank'?'is-selected':''}`}>
        <input 
         type="radio" 
         name="payment_method" 
         checked={paymentMethod==='bank'} 
         onChange={()=>setPaymentMethod('bank')} 
        />
        <div className="payment-card-content">
         <div className="payment-card-header">
          <strong>Chuyển khoản Ngân hàng (VietQR / Napas 247)</strong>
          <span className="badge-recommended">Khuyên dùng</span>
         </div>
         <p>Quét mã QR bằng mọi app ngân hàng. Tự động cộng vật phẩm tức thì 24/7 không mất phí.</p>
        </div>
       </label>

       <label className={`payment-method-card ${paymentMethod==='momo'?'is-selected':''}`}>
        <input 
         type="radio" 
         name="payment_method" 
         checked={paymentMethod==='momo'} 
         onChange={()=>setPaymentMethod('momo')} 
        />
        <div className="payment-card-content">
         <div className="payment-card-header">
          <strong>Ví điện tử MoMo</strong>
         </div>
         <p>Chuyển nhanh qua ứng dụng MoMo chỉ với 1 thao tác quét mã.</p>
        </div>
       </label>

       <label className={`payment-method-card ${paymentMethod==='card'?'is-selected':''}`}>
        <input 
         type="radio" 
         name="payment_method" 
         checked={paymentMethod==='card'} 
         onChange={()=>setPaymentMethod('card')} 
        />
        <div className="payment-card-content">
         <div className="payment-card-header">
          <strong>Thẻ Game / Thẻ Cào (Zing, Garena, Viettel, Vina)</strong>
         </div>
         <p>Hỗ trợ nạp qua mã thẻ cào điện thoại hoặc thẻ game tiện lợi.</p>
        </div>
       </label>
      </div>
     </div>
    </div>

    <div className="checkout-right-col">
     <div className="checkout-summary-card">
      <h2>Thông tin đơn hàng</h2>
      <div className="checkout-lines">
       {items.map(({line,offer})=><div className="checkout-line" key={line.slug}>
        <span>{offer?.name??line.slug} <small>× {line.quantity}</small></span>
        <strong>{offer?.isPurchasable&&offer.priceMinorUnits?formatVnd(offer.priceMinorUnits*line.quantity):'Không khả dụng'}</strong>
       </div>)}
      </div>

      {unavailable&&<p className="shop-error" role="alert">Có sản phẩm đã ngừng bán hoặc chưa có giá. Vui lòng quay lại giỏ để cập nhật.</p>}

      <div className="checkout-bill">
       <div className="bill-row">
        <span>Tạm tính</span>
        <strong>{formatVnd(total)}</strong>
       </div>
       <div className="bill-row">
        <span>Phí thanh toán</span>
        <strong className="text-free">Miễn phí (0đ)</strong>
       </div>
       <div className="bill-row bill-total">
        <span>Tổng thanh toán</span>
        <strong className="bill-total-amount">{formatVnd(total)}</strong>
       </div>
      </div>

      <div className="checkout-actions-panel">
       <button className="shop-primary-link btn-block" type="submit" disabled={busy||unavailable}>
        {busy?'Đang xử lý đơn hàng…':'Xác nhận thanh toán'}
       </button>
       <Link className="shop-secondary-link" href="/cart">← Quay lại giỏ hàng</Link>
      </div>
     </div>
    </div>
   </form>
  </>}
 </section>;
}
