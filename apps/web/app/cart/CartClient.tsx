'use client';

import Link from 'next/link';
import {useEffect,useState} from 'react';
import {cartCount,readCart,removeCartItem,setCartQuantity,type CartLine} from '@/lib/commerce-cart';
import {formatVnd} from '@/lib/currency';

type Offer={slug:string;name:string;summary:string;kind:string;isPurchasable:boolean;priceMinorUnits:number|null;currencyCode:string};
const CART_KEY='nova-haven-cart-v1';
export default function CartClient(){
 const [cart,setCart]=useState<CartLine[]>([]);const [offers,setOffers]=useState<Offer[]>([]);const [loading,setLoading]=useState(true);const [error,setError]=useState('');
 useEffect(()=>{
  const sync=()=>setCart(readCart(localStorage.getItem(CART_KEY)));
  sync();window.addEventListener('storage',sync);window.addEventListener('nova-haven-cart-change',sync);
  fetch('/api/v1/commerce/offers?page=1&pageSize=50',{cache:'no-store'}).then(async response=>{if(!response.ok)throw new Error('Không tải được bảng giá cửa hàng.');const data=await response.json() as {items:Offer[]};setOffers(data.items);}).catch(reason=>setError((reason as Error).message)).finally(()=>setLoading(false));
  return()=>{window.removeEventListener('storage',sync);window.removeEventListener('nova-haven-cart-change',sync);};
 },[]);
 function save(next:CartLine[]){setCart(next);localStorage.setItem(CART_KEY,JSON.stringify(next));window.dispatchEvent(new Event('nova-haven-cart-change'));}
 const lines=cart.map(line=>({line,offer:offers.find(item=>item.slug===line.slug)}));
 const total=lines.reduce((sum,{line,offer})=>sum+(offer?.isPurchasable&&offer.priceMinorUnits?offer.priceMinorUnits*line.quantity:0),0);
 const hasUnavailable=lines.some(({offer})=>!offer||!offer.isPurchasable||!offer.priceMinorUnits);
 return <section className="content shop-page cart-page">
  <p className="eyebrow">NOVA HAVEN · CỬA HÀNG</p><h1>Giỏ hàng</h1><p className="shop-lede">{cartCount(cart)} món đang nằm trong giỏ của bạn.</p>
  <div className="shop-notice"><strong>Giỏ hàng demo</strong><span>Bước tiếp theo chỉ lưu một đơn mô phỏng local; không thu tiền và không giao vật phẩm hoặc quyền lợi trong game.</span></div>
  {error&&<p className="shop-error" role="alert">{error}</p>}
  {loading?<p className="notice" role="status">Đang kiểm tra sản phẩm và giá mới nhất…</p>:cart.length===0?<div className="cart-empty"><span aria-hidden="true">▣</span><h2>Giỏ còn trống</h2><p>Ghé gian hàng và thêm món bạn muốn khám phá.</p><Link className="shop-primary-link" href="/commerce">Mở cửa hàng</Link></div>:<>
   <div className="cart-lines">{lines.map(({line,offer})=><article className="cart-line" key={line.slug}>
    <div className="cart-item-mark" aria-hidden="true">✦</div><div className="cart-item-main"><p className="shop-card-meta">{offer?.kind??'SẢN PHẨM'}</p><h2>{offer?.name??line.slug}</h2><p>{offer?.summary??'Sản phẩm không còn trong danh mục hiện tại.'}</p>{(!offer?.isPurchasable||!offer.priceMinorUnits)&&<strong className="shop-unavailable">Không khả dụng — hãy xóa khỏi giỏ</strong>}</div>
    <div className="cart-line-side">{offer?.isPurchasable&&offer.priceMinorUnits?<strong>{formatVnd(offer.priceMinorUnits*line.quantity)}</strong>:<strong>—</strong>}<div className="quantity-control" aria-label={`Số lượng ${offer?.name??line.slug}`}><button type="button" aria-label="Giảm số lượng" onClick={()=>save(setCartQuantity(cart,line.slug,line.quantity-1))}>−</button><output>{line.quantity}</output><button type="button" aria-label="Tăng số lượng" onClick={()=>save(setCartQuantity(cart,line.slug,line.quantity+1))} disabled={line.quantity>=99}>+</button></div><button className="remove-cart-item" type="button" onClick={()=>save(removeCartItem(cart,line.slug))}>Xóa</button></div>
   </article>)}</div>
   <aside className="cart-summary"><div><span>Giá trị tham khảo</span><strong>{formatVnd(total)}</strong></div><small>Backend kiểm tra giá khi tạo đơn demo. Không có khoản tiền nào bị thu.</small><Link aria-disabled={hasUnavailable} className={`shop-primary-link${hasUnavailable?' is-disabled':''}`} href={hasUnavailable?'#':'/checkout'} onClick={event=>{if(hasUnavailable)event.preventDefault();}}>Tạo đơn mô phỏng</Link><Link className="shop-secondary-link" href="/commerce">← Tiếp tục xem gian hàng</Link></aside>
  </>}
 </section>;
}
