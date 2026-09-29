import Link from 'next/link';
import {commerceApi} from '@/lib/commerce-api';
import CommerceStore from './CommerceStore';

export default async function Commerce(){
 try{
  const result=await commerceApi.list();
  return <section className="content shop-page">
   <p className="eyebrow">NOVA HAVEN · CỬA HÀNG</p><h1>Gian hàng phiêu lưu</h1>
   <p className="shop-lede">Khám phá các gói và vật phẩm do đội ngũ Nova Haven biên tập.</p>
   <aside className="shop-notice"><strong>Cửa Hàng Vật Phẩm & Gói Hỗ Trợ</strong><span>Khám phá các vật phẩm, trang bị và gói hỗ trợ để đồng hành cùng máy chủ Nova Haven.</span></aside>
   <div className="shop-toolbar"><span>{result.total} sản phẩm đã xuất bản</span><Link className="button button-outline" href="/cart">Xem giỏ hàng</Link></div>
   {result.items.length?<CommerceStore items={result.items}/>:<div className="notice">Chưa có sản phẩm được xuất bản. Quản trị viên có thể tạo sản phẩm trong Nova CMS.</div>}
  </section>;
 }catch{return <section className="content shop-page"><p className="eyebrow">NOVA HAVEN · CỬA HÀNG</p><h1>Gian hàng phiêu lưu</h1><div className="notice">Hệ thống cửa hàng đang cập nhật dữ liệu từ máy chủ. Vui lòng tải lại trang sau ít phút.</div></section>;}
}
