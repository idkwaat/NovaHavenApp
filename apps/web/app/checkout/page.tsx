import type {Metadata} from 'next';
import CheckoutClient from './CheckoutClient';
export const metadata:Metadata={title:'Thanh toán thử'};
export default function CheckoutPage(){return <CheckoutClient/>;}
