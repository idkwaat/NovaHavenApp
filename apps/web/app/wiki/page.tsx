import type {Metadata} from 'next';
import Link from 'next/link';
import {wikiApi} from '@/lib/wiki-api';
import {normalizeSearch,wikiCategoryLabel,wikiListHref} from '@/lib/wiki-model';
import ArticlePreview from '../ArticlePreview';
import {articlePreviewFor} from '@/lib/article-previews';

export const metadata:Metadata={title:'Wiki',description:'Tra cứu kiến thức và hướng dẫn Nova Haven.'};
type Props={searchParams:Promise<{q?:string;category?:string;tag?:string;page?:string}>};

export default async function Wiki({searchParams}:Props){
  const params=await searchParams;
  const q=normalizeSearch(params.q??'');
  const category=(params.category??'').slice(0,80);
  const tag=(params.tag??'').slice(0,80);
  const page=Math.max(1,Math.min(100000,Number.parseInt(params.page??'1',10)||1));
  const href=(overrides:{q?:string;category?:string;tag?:string;page?:number})=>
    wikiListHref({q,category,tag,...overrides});
  try{
    const [categories,tags,articles]=await Promise.all([
      wikiApi.categories(),wikiApi.tags(),wikiApi.articles(q,category,page,tag)
    ]);
    const visibleCategories=categories.filter(category => category.articleCount > 0);
    return <section className="content wiki-page">
      <p className="eyebrow">NOVA HAVEN KNOWLEDGE BASE</p>
      <h1>Thư viện Wiki</h1>
      <p>Khám phá kiến thức đã xuất bản. Thay đổi trong bản nháp không xuất hiện ở đây.</p>
      <div className="wiki-layout">
        <aside className="wiki-filters" aria-label="Lọc thư viện Wiki">
          <form action="/wiki" className="search-bar">
            <label htmlFor="wiki-search">TÌM TRONG WIKI</label>
            <input id="wiki-search" name="q" aria-label="Tìm Wiki" placeholder="Tên bài viết hoặc chủ đề" defaultValue={q} maxLength={100}/>
            {category&&<input type="hidden" name="category" value={category}/>}
            {tag&&<input type="hidden" name="tag" value={tag}/>}
            <button type="submit">Tìm kiếm</button>
          </form>
          <h2>Danh mục</h2>
          <div className="category-list">
            <Link className={!category?'selected':''} href={href({category:'',page:1})}>Tất cả bài viết</Link>
            {visibleCategories.map(x=><Link className={category===x.slug?'selected':''} key={x.id}
              href={href({category:x.slug,page:1})}>{x.name} <span>{x.articleCount}</span></Link>)}
          </div>
          <h2>Chủ đề</h2>
          <div className="tag-list">
            <Link className={!tag?'selected':''} href={href({tag:'',page:1})}>Tất cả chủ đề</Link>
            {tags.filter(x=>x.articleCount>0).map(x=><Link className={tag===x.slug?'selected':''} key={x.id}
              href={href({tag:x.slug,page:1})}>#{x.name} <span>{x.articleCount}</span></Link>)}
          </div>
        </aside>
        <div className="wiki-results">
          <div className="results-heading"><div><span className="eyebrow">BÀI VIẾT ĐÃ XUẤT BẢN</span><h2>Kết quả</h2></div><span>{articles.total} bài</span></div>
          {articles.items.length?<div className="article-grid">{articles.items.map(item=>{
            const preview=articlePreviewFor({collection:'wiki',topic:item.category,slug:item.slug,title:item.title,imageUrl:item.previewImageUrl,imageAlt:item.previewImageAlt});
            return <article className="article-card editorial-card" key={item.id}>
              <ArticlePreview image={preview}/>
              <div className="editorial-card-copy">
                <small>{wikiCategoryLabel(item.category,categories)} · Phiên bản {item.revision}</small>
                <h2><Link href={`/wiki/${encodeURIComponent(item.slug)}`}>{item.title}</Link></h2>
                <p>{item.summary||'Đọc nội dung chi tiết →'}</p>
                <small className="editorial-card-tags">{item.tags.map(value=>`#${value}`).join(' · ')}</small>
                <Link className="editorial-read-link" href={`/wiki/${encodeURIComponent(item.slug)}`}>Đọc bài →</Link>
              </div>
            </article>;
          })}</div>:<div className="notice">Không có bài viết đã xuất bản phù hợp.</div>}
          <div className="pager">
            {page>1&&<Link href={href({page:page-1})}>← Trang trước</Link>}
            <span>Trang {page} · {articles.total} bài</span>
            {page*articles.pageSize<articles.total&&<Link href={href({page:page+1})}>Trang sau →</Link>}
          </div>
        </div>
      </div>
    </section>;
  }catch{
    return <section className="content wiki-page"><p className="eyebrow">NOVA HAVEN · CẨM NANG THẾ GIỚI</p><h1>Thư viện Wiki</h1>
      <div className="public-recovery" role="alert"><div><h2>Wiki đang tạm thời không truy cập được</h2><p>Dịch vụ chưa trả về dữ liệu. Thử lại sau ít phút; bộ lọc hiện tại sẽ được giữ.</p></div><div className="public-recovery-actions"><Link href={wikiListHref({q,category,tag,page})}>Thử lại</Link><Link href="/community">Khám phá cộng đồng</Link></div></div></section>;
  }
}
