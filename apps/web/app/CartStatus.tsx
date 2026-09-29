'use client';

import Link from 'next/link';
import {useEffect,useState} from 'react';
import {cartCount,readCart} from '@/lib/commerce-cart';

const CART_KEY='nova-haven-cart-v1';
export default function CartStatus(){
 const [count,setCount]=useState(0);
 useEffect(()=>{
  const refresh=()=>setCount(cartCount(readCart(localStorage.getItem(CART_KEY))));
  refresh();window.addEventListener('storage',refresh);window.addEventListener('nova-haven-cart-change',refresh);
  return()=>{window.removeEventListener('storage',refresh);window.removeEventListener('nova-haven-cart-change',refresh);};
 },[]);
 return <Link className="cart-status-link" href="/cart" aria-label={`Giỏ hàng, ${count} món`} title="Giỏ hàng">Giỏ <span aria-hidden="true">{count}</span></Link>;
}
