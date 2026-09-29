import Link from 'next/link';
import ArticlePreview from '../ArticlePreview';
import {articlePreviewFor,extractMarkdownPreview} from '@/lib/article-previews';
import PageNavigation, {parsePublicPage} from '../PageNavigation';

type NewsSummary={id:string;slug:string;title:string;summary:string;markdown?:string;publishedAt:string};
type NewsPageResponse={items:NewsSummary[];page:number;pageSize:number;total:number};
const apiOrigin=process.env.NOVA_API_ORIGIN??'http://localhost:5080';

async function loadNews(page:number):Promise<NewsPageResponse>{
 const response=await fetch(new URL(`/api/v1/news?page=${page}&pageSize=20`,apiOrigin),{cache:'no-store'});
 if(!response.ok)throw Error(`News API ${response.status}`);
 return response.json() as Promise<NewsPageResponse>;
}

export default async function News({searchParams}:{searchParams:Promise<{page?:string}>}){
 try{
  const page=parsePublicPage((await searchParams).page);
  const result=await loadNews(page);
  return <section className="content news-page">
   <header className="page-heading"><p className="eyebrow">NOVA HAVEN · BẢN TIN</p><h1>Tin tức &amp; cập nhật</h1><p>Những bài viết đã được xuất bản trên Nova Haven.</p></header>
   <div className="news-layout">
    <section className="news-feed" aria-labelledby="news-feed-title">
     <div className="results-heading"><h2 id="news-feed-title">Tin mới nhất</h2><span>{result.total} bài</span></div>
     {result.items.length?<div className="news-list">{result.items.map(item=>{
       const markdownImage=extractMarkdownPreview(item.markdown??'');
       const preview=articlePreviewFor({collection:'news',slug:item.slug,title:item.title,imageUrl:markdownImage?.src,imageAlt:markdownImage?.alt});
       return <article className="news-card editorial-news-card" key={item.id}>
        <ArticlePreview image={preview}/>
        <div className="news-copy">
         <small>{new Date(item.publishedAt).toLocaleDateString('vi-VN',{timeZone:'UTC'})}</small>
         <h2><Link href={`/news/${item.slug}`}>{item.title}</Link></h2>
         <p>{item.summary}</p>
         <Link className="news-read-link" href={`/news/${item.slug}`}>Đọc tiếp <span aria-hidden="true">→</span></Link>
        </div>
       </article>;
      })}</div>:<div className="notice">Chưa có tin đã xuất bản.</div>}
     <PageNavigation page={result.page} pageSize={result.pageSize} total={result.total} href="/news"/>
    </section>
    <aside className="news-aside" aria-label="Khám phá Nova Haven">
     <p className="eyebrow">KHÁM PHÁ THÊM</p><h2>Điều gì đang chờ bạn?</h2>
     <p>Tra cứu nội dung thế giới hoặc danh mục vật phẩm đã xuất bản.</p>
     <nav><Link href="/wiki">Thư viện Wiki <span>→</span></Link><Link href="/catalog">Vật phẩm <span>→</span></Link><Link href="/community">Cộng đồng <span>→</span></Link></nav>
     <small>Chỉ hiển thị nội dung công khai đã xuất bản.</small>
    </aside>
   </div>
  </section>;
 }catch{return <section className="content news-page"><header className="page-heading"><p className="eyebrow">NOVA HAVEN · BẢN TIN</p><h1>Tin tức &amp; cập nhật</h1></header><div className="public-recovery" role="alert"><div><h2>Chưa tải được bản tin</h2><p>Dịch vụ dữ liệu đang tạm gián đoạn. Hãy thử tải lại sau.</p></div><div className="public-recovery-actions"><Link href="/news">Thử lại</Link><Link href="/wiki">Mở thư viện Wiki</Link></div></div></section>;}
}
