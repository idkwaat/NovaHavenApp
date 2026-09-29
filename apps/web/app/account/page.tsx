import type {Metadata} from 'next';
import AccountClient from './AccountClient';

export const metadata:Metadata={title:'Tài khoản'};

export default function AccountPage(){
  return <section className="account-page page-band">
    <p className="eyebrow">NOVA HAVEN · THÀNH VIÊN</p>
    <h1>Tài khoản phiêu lưu</h1>
    <p>Đăng nhập để theo dõi thông báo và nhận cập nhật mới từ Nova Haven.</p>
    <AccountClient/>
  </section>;
}
