import Link from 'next/link';
import ArticlePreview from './ArticlePreview';
import SiteIcon from './SiteIcon';
import {articlePreviewFor, extractMarkdownPreview} from '@/lib/article-previews';
import {catalogApi, type CatalogSummary} from '@/lib/catalog-api';
import {communityApi, communityKindLabels, type CommunitySummary} from '@/lib/community-api';
import {commerceApi} from '@/lib/commerce-api';
import {knowledgeApi, knowledgeKindLabels, type KnowledgeSummary} from '@/lib/knowledge-api';
import {wikiApi, type WikiArticleSummary} from '@/lib/wiki-api';
import {discordInviteUrl, minecraftServerAddress} from '@/lib/public-site-config';

type HomeNewsSummary={id:string;slug:string;title:string;summary:string;markdown:string;publishedAt:string};
type HomePage<T>={items:T[];page:number;pageSize:number;total:number};
const apiOrigin=process.env.NOVA_API_ORIGIN??'http://localhost:5080';

export default async function Home(){
 const newsRequest=fetch(new URL('/api/v1/news?page=1&pageSize=20',apiOrigin),{
  cache:'no-store',headers:{Accept:'application/json'},
 }).then(async response=>{
  if(!response.ok)throw new Error(`News API ${response.status}`);
  return response.json() as Promise<HomePage<HomeNewsSummary>>;
 });

 const [categoriesResult,wikiResult,newsResult,knowledgeResult,catalogResult,communityResult,commerceResult]=await Promise.allSettled([
  wikiApi.categories(),
  wikiApi.articles(),
  newsRequest,
  knowledgeApi.list(),
  catalogApi.items(),
  communityApi.list(),
  commerceApi.list(),
 ]);

 const categories=categoriesResult.status==='fulfilled'?categoriesResult.value:[];
 const visibleCategories=categories.filter(category=>category.articleCount>0);
 const wikiPage=wikiResult.status==='fulfilled'?wikiResult.value:null;
 const wikiItems=wikiPage?.items.slice(0,3)??[];
 const newsPage=newsResult.status==='fulfilled'?newsResult.value:null;
 const newsItems=newsPage?.items.slice(0,2)??[];
 const knowledgePage=knowledgeResult.status==='fulfilled'?knowledgeResult.value:null;
 const knowledgeItems=knowledgePage?.items.slice(0,3)??[];
 const catalogPage=catalogResult.status==='fulfilled'?catalogResult.value:null;
 const communityPage=communityResult.status==='fulfilled'?communityResult.value:null;
 const commercePage=commerceResult.status==='fulfilled'?commerceResult.value:null;

 const destinations=[
  {href:'/wiki',number:'01',title:'Thư viện Wiki',description:'Cẩm nang và bài viết đã xuất bản.',total:wikiPage?.total??null},
  {href:'/knowledge',number:'02',title:'Bản đồ thế giới',description:'Địa danh, nhân vật và sổ tay phiêu lưu.',total:knowledgePage?.total??null},
  {href:'/catalog',number:'03',title:'Vật phẩm',description:'Danh mục nội dung được biên tập.',total:catalogPage?.total??null},
  {href:'/community',number:'04',title:'Cộng đồng',description:'Sự kiện, bang hội và hoạt động cộng đồng.',total:communityPage?.total??null},
  {href:'/news',number:'05',title:'Bản tin',description:'Tin tức và cập nhật dự án.',total:newsPage?.total??null},
  {href:'/commerce',number:'06',title:'Cửa hàng demo',description:'Danh mục và quy trình tạo đơn mô phỏng.',total:commercePage?.total??null},
 ];

 return <>
  <section className="hero hero-home" aria-labelledby="home-title">
   <div className="hero-landscape-image" aria-hidden="true"/><div className="hero-image-shade" aria-hidden="true"/>
   <div className="hero-copy">
    <div className="hero-signboard"><p className="hero-brand">NOVA HAVEN</p></div>
    <h1 id="home-title">Thế giới Minecraft nhập vai</h1>
    <p className="hero-tagline">KHÁM PHÁ · CHIẾN ĐẤU · PHIÊU LƯU</p>
    <p className="hero-server">{minecraftServerAddress ? <>Địa chỉ máy chủ: <strong>{minecraftServerAddress}</strong> <span>· Chưa có trạng thái kết nối trực tiếp</span></> : <>Bản xem trước local <span>· Địa chỉ máy chủ Minecraft chưa được cấu hình</span></>}</p>
    <div className="actions">
      <Link className="button button-green" href="/wiki">ĐỌC CẨM NANG</Link>
      <Link className="button button-blue" href="/map">BẢN ĐỒ THẾ GIỚI</Link>
    </div>
   </div>
  </section>

  <div className="home-telemetry-band" aria-label="Số lượng nội dung đã xuất bản">
   <div className="content home-telemetry-grid">
    <div className="telemetry-item">
     <span className="telemetry-number">{wikiPage?.total ?? '—'}</span>
     <span className="telemetry-label">Bài Wiki công khai</span>
    </div>
    <div className="telemetry-item">
     <span className="telemetry-number">{knowledgePage?.total ?? '—'}</span>
     <span className="telemetry-label">Hồ sơ Atlas</span>
    </div>
    <div className="telemetry-item">
     <span className="telemetry-number">{catalogPage?.total ?? '—'}</span>
     <span className="telemetry-label">Mục danh mục</span>
    </div>
    <div className="telemetry-item">
     <span className="telemetry-number">{communityPage?.total ?? '—'}</span>
     <span className="telemetry-label">Bài cộng đồng</span>
    </div>
   </div>
  </div>

  <section className="home-intro-band" aria-labelledby="home-explore-title">
   <div className="content home-intro">
    <div><p className="eyebrow">KHÁM PHÁ VÙNG ĐẤT</p><h2 id="home-explore-title">Bắt đầu từ những điều cần biết.</h2><p className="section-lede">Tra cứu cẩm nang, địa danh và hướng dẫn do đội ngũ Nova Haven biên tập.</p></div>
    <Link className="text-link" href="/wiki">Mở thư viện Wiki <span aria-hidden="true">→</span></Link>
   </div>
  </section>

  <section className="content home-categories" aria-label="Danh mục Wiki">
   {visibleCategories.length?<div className="tile-grid">{visibleCategories.map((category,index)=><Link className="tile" key={category.id} href={`/wiki?category=${encodeURIComponent(category.slug)}`}><span className="tile-number">{String(index+1).padStart(2,'0')}</span><span className="tile-icon" aria-hidden="true">✧</span><strong>{category.name}</strong><small>{category.articleCount} bài viết <span aria-hidden="true">↗</span></small></Link>)}</div>:<div className="notice">{categoriesResult.status==='rejected'?'Chưa tải được danh mục Wiki. Hãy mở thư viện để thử lại.':'Các khu vực Wiki sẽ xuất hiện khi có bài viết được xuất bản.'}</div>}
  </section>

  <section className="home-editorial-section home-features-section" aria-labelledby="home-features-title" data-home-section="features">
   <div className="home-section-inner">
    <header className="home-section-heading">
     <div>
      <p className="eyebrow">CÔNG CỤ KHÁM PHÁ</p>
      <h2 id="home-features-title">Những gì bạn có thể làm trên Nova Haven</h2>
      <p>Tra cứu nội dung đã xuất bản, xem bản đồ địa hình local và theo dõi cập nhật. Những tính năng phụ thuộc máy chủ sẽ chỉ được giới thiệu khi có tích hợp được xác nhận.</p>
     </div>
    </header>
    <div className="home-feature-grid">
     <div className="home-feature-card">
      <div className="feature-card-header">
       <span className="feature-icon" aria-hidden="true"><SiteIcon name="book" /></span>
       <span className="feature-badge">Nội dung xuất bản</span>
      </div>
      <h3>Wiki và cẩm nang</h3>
      <p>Đọc các bài viết đã được quản trị viên xuất bản. Bản nháp không xuất hiện trong thư viện công khai.</p>
      <div className="feature-card-footer">
       <Link className="feature-card-link" href="/wiki">Mở thư viện Wiki <span aria-hidden="true">→</span></Link>
      </div>
     </div>

     <div className="home-feature-card">
      <div className="feature-card-header">
       <span className="feature-icon" aria-hidden="true"><SiteIcon name="map" /></span>
       <span className="feature-badge">Bản xem trước</span>
      </div>
      <h3>Atlas địa hình</h3>
      <p>Xem ảnh địa hình của world đã nhập. Đây là dữ liệu local, chưa theo dõi vị trí người chơi hay trạng thái máy chủ trực tiếp.</p>
      <div className="feature-card-footer">
       <Link className="feature-card-link" href="/map">Mở bản đồ <span aria-hidden="true">→</span></Link>
      </div>
     </div>

     <div className="home-feature-card">
      <div className="feature-card-header">
       <span className="feature-icon" aria-hidden="true"><SiteIcon name="news" /></span>
       <span className="feature-badge">Bản tin</span>
      </div>
      <h3>Tin tức dự án</h3>
      <p>Theo dõi thông báo và bài viết mới do đội ngũ quản trị đăng tải trên website.</p>
      <div className="feature-card-footer">
       <Link className="feature-card-link" href="/news">Mở bản tin <span aria-hidden="true">→</span></Link>
      </div>
     </div>

     <div className="home-feature-card">
      <div className="feature-card-header">
       <span className="feature-icon" aria-hidden="true"><SiteIcon name="community" /></span>
       <span className="feature-badge">Cộng đồng</span>
      </div>
      <h3>Không gian cộng đồng</h3>
      <p>Xem bang hội, sự kiện và nội dung cộng đồng đã được xuất bản; website không hiển thị trạng thái người chơi trực tiếp.</p>
      <div className="feature-card-footer">
       <Link className="feature-card-link" href="/community">Mở cộng đồng <span aria-hidden="true">→</span></Link>
      </div>
     </div>
    </div>
   </div>
  </section>

  <section className="home-editorial-section home-join-guide-section" aria-labelledby="home-guide-title" data-home-section="guide">
   <div className="home-section-inner">
    <header className="home-section-heading">
     <div>
      <p className="eyebrow">BẮT ĐẦU TỪ ĐÂU</p>
      <h2 id="home-guide-title">Khám phá Nova Haven trong 3 bước</h2>
      <p>Các bước dưới đây dùng được ngay trên bản web local; chưa yêu cầu tài khoản hoặc máy chủ Minecraft đang chạy.</p>
     </div>
    </header>
    <div className="home-guide-grid">
     <div className="home-guide-step">
      <span className="step-number">01</span>
      <div className="step-content">
       <h3>Đọc cẩm nang</h3>
       <p>Bắt đầu với các bài Wiki đã xuất bản để hiểu nội dung đang có.</p>
       <Link className="step-action-link" href="/wiki">Mở Wiki <span aria-hidden="true">→</span></Link>
      </div>
     </div>

     <div className="home-guide-step">
      <span className="step-number">02</span>
      <div className="step-content">
       <h3>Xem Atlas</h3>
       <p>Mở bản đồ để xem ảnh địa hình local. Trang hiện chưa có theo dõi người chơi hoặc máy chủ trực tiếp.</p>
       <Link className="step-action-link" href="/map">Mở Atlas <span aria-hidden="true">→</span></Link>
      </div>
     </div>

     <div className="home-guide-step">
      <span className="step-number">03</span>
      <div className="step-content">
       <h3>Theo dõi cập nhật</h3>
       <p>Xem tin tức mới hoặc nội dung cộng đồng để biết các thay đổi đã được công bố.</p>
       <Link className="step-action-link" href="/news">Mở bản tin <span aria-hidden="true">→</span></Link>
      </div>
     </div>
    </div>
   </div>
  </section>

  <section className="home-editorial-section home-map-spotlight-section" aria-labelledby="home-map-title" data-home-section="map">
   <div className="home-section-inner">
    <div className="home-map-spotlight-card">
     <div className="map-spotlight-content">
      <p className="eyebrow">BẢN ĐỒ THẾ GIỚI</p>
      <h2 id="home-map-title">Bản xem trước từ world local</h2>
      <p>Trang bản đồ hiện dùng ảnh địa hình làm atlas dự phòng. Chưa có dữ liệu vị trí người chơi hoặc đồng bộ trực tiếp với một Minecraft server.</p>
      <div className="map-key-stats">
       <div className="map-stat-badge">
        <strong>Nguồn dữ liệu</strong>
        <span>World đã lưu local</span>
       </div>
       <div className="map-stat-badge">
        <strong>Hiển thị</strong>
        <span>Ảnh địa hình dự phòng</span>
       </div>
       <div className="map-stat-badge">
        <strong>Trạng thái</strong>
        <span>Không phải dữ liệu live</span>
       </div>
      </div>
      <div className="map-spotlight-actions">
       <Link className="button button-green" href="/map">MỞ BẢN ĐỒ THẾ GIỚI</Link>
       <Link className="home-section-link" href="/knowledge">Xem Atlas địa danh <span aria-hidden="true">→</span></Link>
      </div>
     </div>
     <div className="map-spotlight-visual">
      <div className="map-frame">
       <img src="/maps/nova-haven/terrain.png" alt="Bản đồ địa hình thế giới Nova Haven" className="map-preview-img" width="600" height="375" loading="lazy" />
      </div>
     </div>
    </div>
   </div>
  </section>

  <section className="home-editorial-section home-wiki-section" aria-labelledby="home-wiki-title" data-home-section="wiki">
   <div className="home-section-inner">
    <header className="home-section-heading"><div><p className="eyebrow">SỔ TAY NGƯỜI LỮ HÀNH</p><h2 id="home-wiki-title">Những trang nên đọc đầu tiên</h2><p>Các bài Wiki mới nhất, lấy từ nội dung công khai đã xuất bản.</p></div><Link className="home-section-link" href="/wiki">Xem toàn bộ Wiki <span aria-hidden="true">→</span></Link></header>
    {wikiPage===null?<div className="home-section-empty" role="status">Tạm thời chưa tải được Wiki. Thư viện vẫn có thể được mở để thử lại.</div>:wikiItems.length?<div className="home-story-grid">
     {wikiItems.map((article:WikiArticleSummary,index)=><article className="home-story-card" key={article.id}>
      <ArticlePreview image={articlePreviewFor({collection:'wiki',slug:article.slug,title:article.title,topic:article.category,imageUrl:article.previewImageUrl,imageAlt:article.previewImageAlt})}/>
      <div className="home-story-copy"><div className="home-card-meta"><span>{categories.find(category=>category.slug===article.category)?.name??'Wiki'}</span><time dateTime={article.publishedAt}>{new Date(article.publishedAt).toLocaleDateString('vi-VN',{day:'numeric',month:'short',year:'numeric',timeZone:'UTC'})}</time></div><h3><Link href={`/wiki/${article.slug}`}>{article.title}</Link></h3><p>{article.summary}</p><Link className="home-card-read" href={`/wiki/${article.slug}`}>Đọc bài <span aria-hidden="true">→</span></Link></div>
     </article>)}
    </div>:<div className="home-section-empty">Chưa có bài Wiki công khai. Các bài sẽ xuất hiện tại đây sau khi được xuất bản.</div>}
   </div>
  </section>

  <section className="home-editorial-section home-atlas-section" aria-labelledby="home-atlas-title" data-home-section="atlas">
   <div className="home-section-inner">
    <header className="home-section-heading"><div><p className="eyebrow">ATLAS THẾ GIỚI</p><h2 id="home-atlas-title">Gặp gỡ những miền đất và cư dân</h2><p>Khám phá hồ sơ địa danh, nhân vật và những ghi chép đã được biên tập.</p></div><Link className="home-section-link" href="/knowledge">Mở bản đồ thế giới <span aria-hidden="true">→</span></Link></header>
    {knowledgePage===null?<div className="home-section-empty" role="status">Tạm thời chưa tải được Atlas. Hãy thử mở lại thư viện thế giới.</div>:knowledgeItems.length?<div className="home-story-grid home-atlas-grid">
     {knowledgeItems.map((entry:KnowledgeSummary)=><article className="home-story-card" key={entry.id}>
      <ArticlePreview image={articlePreviewFor({collection:entry.kind,slug:entry.slug,title:entry.name,imageUrl:entry.previewImageUrl})}/>
      <div className="home-story-copy"><div className="home-card-meta"><span>{knowledgeKindLabels[entry.kind]}</span><time dateTime={entry.publishedAt}>{new Date(entry.publishedAt).toLocaleDateString('vi-VN',{day:'numeric',month:'short',year:'numeric',timeZone:'UTC'})}</time></div><h3><Link href={`/knowledge/${entry.kind}/${entry.slug}`}>{entry.name}</Link></h3><p>{entry.summary}</p><Link className="home-card-read" href={`/knowledge/${entry.kind}/${entry.slug}`}>Khám phá <span aria-hidden="true">→</span></Link></div>
     </article>)}
    </div>:<div className="home-section-empty">Atlas chưa có mục công khai.</div>}
   </div>
  </section>

  <section className="home-editorial-section home-news-section" aria-labelledby="home-news-title" data-home-section="news">
   <div className="home-section-inner">
    <div className="home-news-community-grid">
     <div className="home-news-column">
      <header className="home-section-heading"><div><p className="eyebrow">BẢNG TIN NOVA HAVEN</p><h2 id="home-news-title">Tin mới từ vùng đất</h2><p>Thông báo và bài viết đã được xuất bản.</p></div><Link className="home-section-link" href="/news">Tất cả tin tức <span aria-hidden="true">→</span></Link></header>
      {newsPage===null?<div className="home-section-empty" role="status">Tạm thời chưa tải được bản tin.</div>:newsItems.length?<div className="home-news-grid">
       {newsItems.map(item=>{const markdownImage=extractMarkdownPreview(item.markdown);return <article className="home-story-card home-news-card" key={item.id}>
        <ArticlePreview image={articlePreviewFor({collection:'news',slug:item.slug,title:item.title,imageUrl:markdownImage?.src,imageAlt:markdownImage?.alt})}/>
        <div className="home-story-copy"><time className="home-news-date" dateTime={item.publishedAt}>{new Date(item.publishedAt).toLocaleDateString('vi-VN',{day:'numeric',month:'long',year:'numeric',timeZone:'UTC'})}</time><h3><Link href={`/news/${item.slug}`}>{item.title}</Link></h3><p>{item.summary}</p><Link className="home-card-read" href={`/news/${item.slug}`}>Đọc cập nhật <span aria-hidden="true">→</span></Link></div>
       </article>;})}
      </div>:<div className="home-section-empty">Chưa có tin tức công khai.</div>}
     </div>

     <aside className="home-community-panel" aria-labelledby="home-community-title">
      <p className="eyebrow">GÓC CỘNG ĐỒNG</p><h2 id="home-community-title">Cuộc phiêu lưu vui hơn khi đi cùng nhau.</h2>
      {communityPage===null?<p role="status">Tạm thời chưa tải được nội dung cộng đồng.</p>:communityPage.items[0]?<div className="home-community-feature"><span>{communityKindLabels[communityPage.items[0].kind]}</span><h3><Link href={`/community/${communityPage.items[0].kind}/${communityPage.items[0].slug}`}>{communityPage.items[0].name}</Link></h3><p>{communityPage.items[0].summary}</p></div>:<p>Chưa có nội dung cộng đồng công khai. Khi được xuất bản, sự kiện và hoạt động sẽ xuất hiện tại đây.</p>}
      <p className="home-community-note">Nội dung cộng đồng là bài biên tập, không phải trạng thái trực tiếp trong máy chủ trò chơi.</p>
      <div className="home-discord-box">
       <div className="discord-box-header">
        <SiteIcon name="discord" className="discord-icon" />
        <div>
         <strong>{discordInviteUrl?'Kênh Discord Nova Haven':'Cộng đồng Nova Haven'}</strong>
         <small>Nội dung cộng đồng đã xuất bản</small>
        </div>
       </div>
       <p>Xem các bài viết và hoạt động cộng đồng được đăng trên Nova Haven.</p>
       {discordInviteUrl ? <a className="button button-blue discord-join-btn" href={discordInviteUrl} target="_blank" rel="noreferrer">GIA NHẬP DISCORD <span aria-hidden="true">↗</span></a> : <Link className="button button-blue discord-join-btn" href="/community">MỞ TRANG CỘNG ĐỒNG <span aria-hidden="true">→</span></Link>}
      </div>
      <Link className="home-community-link" href="/community">Khám phá cộng đồng <span aria-hidden="true">→</span></Link>
     </aside>
    </div>
   </div>
  </section>

  <section className="home-editorial-section home-directory-section" aria-labelledby="home-directory-title" data-home-section="directory">
   <div className="home-section-inner">
    <header className="home-section-heading"><div><p className="eyebrow">CÁC KHU VỰC</p><h2 id="home-directory-title">Chọn hành trình tiếp theo</h2><p>Mỗi mục dẫn tới một khu vực riêng của Nova Haven.</p></div></header>
    <nav className="home-destination-grid" aria-label="Khám phá các khu vực Nova Haven">
     {destinations.map(destination=><Link className="home-destination-card" href={destination.href} key={destination.href}><span className="home-destination-number">{destination.number}</span><strong>{destination.title}</strong><span className="home-destination-description">{destination.description}</span><span className="home-destination-status">{destination.total===null?'Tạm thời chưa tải được':destination.total===0?'Chưa có nội dung công khai':`${destination.total} mục đã xuất bản`}</span><span className="home-destination-arrow" aria-hidden="true">↗</span></Link>)}
    </nav>
    <p className="home-editorial-note">Các nội dung trên website được đội ngũ Nova Haven biên tập và xuất bản.</p>
   </div>
  </section>
 </>;
}
