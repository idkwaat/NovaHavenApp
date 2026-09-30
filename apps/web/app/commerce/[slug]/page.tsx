import Link from 'next/link';
import {notFound} from 'next/navigation';
import ReactMarkdown from 'react-markdown';
import rehypeSanitize from 'rehype-sanitize';
import remarkGfm from 'remark-gfm';
import {commerceApi} from '@/lib/commerce-api';
import {formatVnd} from '@/lib/currency';
import {AddToCartButton} from '../CommerceStore';

export default async function CommerceDetail({params}:{params:Promise<{slug:string}>}){
 try{
  const item=await commerceApi.detail((await params).slug);
  return <article className="content shop-page shop-detail-page"><p className="eyebrow">NOVA HAVEN · {item.kind.toUpperCase()}</p><h1>{item.name}</h1><p className="shop-lede">{item.summary}</p>
   <div className="shop-detail-panel"><span>Giá trị tham khảo</span><strong>{item.isPurchasable&&item.priceMinorUnits?formatVnd(item.priceMinorUnits):item.displayPrice}</strong><span>{item.isPurchasable?'Có thể tạo đơn demo':'Chưa mở bán'}</span><AddToCartButton item={item}/></div>
   <div className="shop-notice"><strong>Đơn hàng mô phỏng local</strong><span>Không thu tiền thật, không cần thông tin cá nhân và không giao vật phẩm hoặc quyền lợi trong game.</span></div>
   <div className="markdown"><ReactMarkdown remarkPlugins={[remarkGfm]} rehypePlugins={[rehypeSanitize]} skipHtml>{item.markdown}</ReactMarkdown></div><p><Link className="shop-secondary-link" href="/commerce">← Quay lại cửa hàng</Link></p>
  </article>;
 }catch{return notFound();}
}
