import type {Metadata} from 'next';
import NotificationsClient from './NotificationsClient';

export const metadata:Metadata={title:'Thông báo'};

export default function NotificationsPage(){
  return <section className="account-page page-band">
    <p className="eyebrow">BẢNG TIN CỦA BẠN</p>
    <h1>Thông báo</h1>
    <p>Tin mới gửi tới tài khoản Nova Haven của bạn.</p>
    <NotificationsClient/>
  </section>;
}
