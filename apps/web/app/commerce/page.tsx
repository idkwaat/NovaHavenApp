import Link from 'next/link';
import {commerceApi} from '@/lib/commerce-api';
import CommerceStore from './CommerceStore';
import PageNavigation, {parsePublicPage} from '../PageNavigation';

export default async function Commerce({searchParams}:{searchParams:Promise<{page?:string}>}){
 try{
  const result=await commerceApi.list(parsePublicPage((await searchParams).page));
  return <section className="content shop-page">
   <p className="eyebrow">NOVA HAVEN · CỬA HÀNG</p><h1>Gian hàng phiêu lưu</h1>
   <p className="shop-lede">Danh mục sản phẩm và quy trình tạo đơn mô phỏng local.</p>
   <aside className="shop-notice"><strong>Gian hàng demo</strong><span>Giá chỉ để kiểm thử quy trình. Không thu tiền thật, không cần thông tin cá nhân và không giao vật phẩm hoặc quyền lợi trong game.</span></aside>
   <div className="shop-toolbar"><span>{result.total} sản phẩm đã xuất bản</span><Link className="button button-outline" href="/cart">Xem giỏ hàng</Link></div>
   {result.items.length?<CommerceStore items={result.items}/>:<div className="notice">Chưa có sản phẩm được xuất bản. Quản trị viên có thể tạo sản phẩm trong Nova CMS.</div>}
   <PageNavigation page={result.page} pageSize={result.pageSize} total={result.total} href="/commerce"/>
  </section>;
 }catch{return <section className="content shop-page"><p className="eyebrow">NOVA HAVEN · CỬA HÀNG</p><h1>Gian hàng phiêu lưu</h1><div className="notice">Hệ thống cửa hàng đang cập nhật dữ liệu từ máy chủ. Vui lòng tải lại trang sau ít phút.</div></section>;}
}
