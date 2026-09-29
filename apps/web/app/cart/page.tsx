import type {Metadata} from 'next';
import CartClient from './CartClient';
export const metadata:Metadata={title:'Giỏ hàng'};
export default function CartPage(){return <CartClient/>;}
