'use client';

import Link from 'next/link';
import {useState} from 'react';
import {cartCount,readCart,setCartQuantity} from '@/lib/commerce-cart';
import {formatVnd} from '@/lib/currency';
import type {CommerceSummary} from '@/lib/commerce-api';

const CART_KEY='nova-haven-cart-v1';
function announceCart(cart:ReturnType<typeof readCart>){localStorage.setItem(CART_KEY,JSON.stringify(cart));window.dispatchEvent(new Event('nova-haven-cart-change'));}
export function AddToCartButton({item}:{item:CommerceSummary}){
 const [message,setMessage]=useState('');
 function add(){
  if(!item.isPurchasable||!item.priceMinorUnits){setMessage('Sản phẩm này chưa mở mua thử.');return;}
  const current=readCart(localStorage.getItem(CART_KEY));
  const updated=setCartQuantity(current,item.slug,(current.find(line=>line.slug===item.slug)?.quantity??0)+1);
  if((current.find(line=>line.slug===item.slug)?.quantity??0)>=99||(!current.some(line=>line.slug===item.slug)&&updated.length===current.length)){setMessage('Giỏ đã đạt giới hạn cho sản phẩm này.');return;}
  announceCart(updated);setMessage(`Đã thêm ${item.name} vào giỏ (${cartCount(updated)} món).`);
 }
 return <div className="shop-add-control"><button className="shop-add-button" type="button" onClick={add} disabled={!item.isPurchasable||!item.priceMinorUnits}>Thêm vào giỏ</button><span className="shop-status" aria-live="polite">{message}</span></div>;
}

export default function CommerceStore({items}:{items:CommerceSummary[]}){
 return <div className="shop-grid">{items.map(item=><article className="shop-card" key={item.id}>
  <div className="shop-card-art" aria-hidden="true"><span>{item.kind==='cosmetic'?'✦':item.kind==='membership'?'♜':item.kind==='bundle'?'▣':'◆'}</span><small>{item.kind}</small></div>
  <div className="shop-card-copy"><p className="shop-card-meta">NOVA HAVEN <span>·</span> {item.kind}</p><h2><Link href={`/commerce/${item.slug}`}>{item.name}</Link></h2><p>{item.summary}</p>
   <div className="shop-card-bottom"><strong>{item.isPurchasable&&item.priceMinorUnits?formatVnd(item.priceMinorUnits):item.isPurchasable?'Giá chưa thiết lập':'Chưa mở bán'}</strong><Link className="shop-detail-link" href={`/commerce/${item.slug}`}>Chi tiết <span aria-hidden="true">→</span></Link></div>
   <AddToCartButton item={item}/>
  </div>
 </article>)}</div>;
}
