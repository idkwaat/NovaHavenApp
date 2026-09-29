import Link from 'next/link';
import ArticlePreview from './ArticlePreview';
import CopyIpButton from './CopyIpButton';
import SiteIcon from './SiteIcon';
import {articlePreviewFor, extractMarkdownPreview} from '@/lib/article-previews';
import {catalogApi, type CatalogSummary} from '@/lib/catalog-api';
import {communityApi, communityKindLabels, type CommunitySummary} from '@/lib/community-api';
import {knowledgeApi, knowledgeKindLabels, type KnowledgeSummary} from '@/lib/knowledge-api';
import {wikiApi, type WikiArticleSummary} from '@/lib/wiki-api';

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

 const [categoriesResult,wikiResult,newsResult,knowledgeResult,catalogResult,communityResult]=await Promise.allSettled([
  wikiApi.categories(),
  wikiApi.articles(),
  newsRequest,
  knowledgeApi.list(),
  catalogApi.items(),
  communityApi.list(),
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

 const destinations=[
  {href:'/wiki',number:'01',title:'Thư viện Wiki',description:'Cẩm nang và bài viết đã xuất bản.',total:wikiPage?.total??null},
  {href:'/knowledge',number:'02',title:'Bản đồ thế giới',description:'Địa danh, nhân vật và sổ tay phiêu lưu.',total:knowledgePage?.total??null},
  {href:'/catalog',number:'03',title:'Vật phẩm',description:'Danh mục nội dung được biên tập.',total:catalogPage?.total??null},
  {href:'/community',number:'04',title:'Cộng đồng',description:'Sự kiện, bang hội và hoạt động cộng đồng.',total:communityPage?.total??null},
  {href:'/news',number:'05',title:'Bản tin',description:'Tin tức và cập nhật dự án.',total:newsPage?.total??null},
  {href:'/commerce',number:'06',title:'Cửa hàng',description:'Vật phẩm, trang bị và gói hỗ trợ máy chủ.',total:null},
 ];

 return <>
  <section className="hero hero-home" aria-labelledby="home-title">
   <div className="hero-landscape-image" aria-hidden="true"/><div className="hero-image-shade" aria-hidden="true"/>
   <div className="hero-copy">
    <div className="hero-signboard"><p className="hero-brand">NOVA HAVEN</p></div>
    <h1 id="home-title">Thế giới Minecraft nhập vai</h1>
    <p className="hero-tagline">KHÁM PHÁ · CHIẾN ĐẤU · PHIÊU LƯU</p>
    <p className="hero-server"><span className="online-dot" aria-hidden="true"/> play.novahaven.net <span>· Máy chủ chính thức</span><CopyIpButton ip="play.novahaven.net" /></p>
    <div className="actions">
      <Link className="button button-green" href="/wiki">ĐỌC CẨM NANG</Link>
      <Link className="button button-blue" href="/map">BẢN ĐỒ THẾ GIỚI</Link>
      <Link className="button button-blue" href="/community">CỘNG ĐỒNG</Link>
      <Link className="button button-coral" href="/commerce">GIAN HÀNG</Link>
    </div>
   </div>
  </section>

  <div className="home-telemetry-band" aria-label="Thông số thế giới Nova Haven">
   <div className="content home-telemetry-grid">
    <div className="telemetry-item">
     <span className="telemetry-number">4</span>
     <span className="telemetry-label">Hệ Phái Chiến Đấu</span>
    </div>
    <div className="telemetry-item">
     <span className="telemetry-number">40+</span>
     <span className="telemetry-label">Trùm & Quái Vật</span>
    </div>
    <div className="telemetry-item">
     <span className="telemetry-number">{wikiPage?.total ? `${wikiPage.total}` : '500+'}</span>
     <span className="telemetry-label">Nhiệm Vụ & Cẩm Nang</span>
    </div>
    <div className="telemetry-item">
     <span className="telemetry-number">100%</span>
     <span className="telemetry-label">Nhập Vai Miễn Phí</span>
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
      <p className="eyebrow">ĐẶC SẮC MÁY CHỦ</p>
      <h2 id="home-features-title">Trải nghiệm nhập vai hoàn chỉnh & khác biệt</h2>
      <p>Thế giới Nova Haven được xây dựng thủ công với lối chơi nhập vai đậm nét, tự do tùy biến nhân vật và khám phá không giới hạn.</p>
     </div>
    </header>
    <div className="home-feature-grid">
     <div className="home-feature-card">
      <div className="feature-card-header">
       <span className="feature-icon" aria-hidden="true"><SiteIcon name="swords" /></span>
       <span className="feature-badge">4 Hệ Phái</span>
      </div>
      <h3>Hệ Thống Nghề & Kỹ Năng Độc Bản</h3>
      <p>Lựa chọn giữa Chiến Binh, Pháp Sư, Xạ Thủ và Du Hiệp. Mở khóa hàng chục kỹ năng chủ động, nội tại và xây dựng bảng thuộc tính theo phong cách riêng của bạn.</p>
      <div className="feature-card-footer">
       <Link className="feature-card-link" href="/wiki?category=classes">Tìm hiểu chức nghiệp <span aria-hidden="true">→</span></Link>
      </div>
     </div>

     <div className="home-feature-card">
      <div className="feature-card-header">
       <span className="feature-icon" aria-hidden="true"><SiteIcon name="shield" /></span>
       <span className="feature-badge">Thử Thách Lớn</span>
      </div>
      <h3>Hầm Ngục & Trùm Thế Giới Sử Thi</h3>
      <p>Thám hiểm các dungeon bí ẩn theo cấp độ, đối đầu Boss sở hữu cơ chế tấn công đặc biệt đòi hỏi kỹ năng di chuyển và sự phối hợp ăn ý của cả tổ đội.</p>
      <div className="feature-card-footer">
       <Link className="feature-card-link" href="/wiki">Xem cẩm nang chiến đấu <span aria-hidden="true">→</span></Link>
      </div>
     </div>

     <div className="home-feature-card">
      <div className="feature-card-header">
       <span className="feature-icon" aria-hidden="true"><SiteIcon name="compass" /></span>
       <span className="feature-badge">Thế Giới Mở</span>
      </div>
      <h3>Bản Đồ Rộng Lớn & Cốt Truyện Sâu</h3>
      <p>Từ Thung Lũng Sao trù phú đến những miền đất hoang sơ kỳ bí. Gặp gỡ các NPC có câu chuyện riêng, chuỗi nhiệm vụ phong phú hé lộ bí mật ngàn năm.</p>
      <div className="feature-card-footer">
       <Link className="feature-card-link" href="/map">Xem bản đồ thế giới <span aria-hidden="true">→</span></Link>
      </div>
     </div>

     <div className="home-feature-card">
      <div className="feature-card-header">
       <span className="feature-icon" aria-hidden="true"><SiteIcon name="alchemy" /></span>
       <span className="feature-badge">Tự Do Giao Thương</span>
      </div>
      <h3>Khai Khoáng, Chế Tác & Bang Hội</h3>
      <p>Thu thập tài nguyên độc nhất, rèn trang bị cổ xưa có chỉ số ngẫu nhiên, lập bang hội cùng anh em gây dựng thanh danh và thâu tóm lãnh địa.</p>
      <div className="feature-card-footer">
       <Link className="feature-card-link" href="/community">Khám phá bang hội <span aria-hidden="true">→</span></Link>
      </div>
     </div>
    </div>
   </div>
  </section>

  <section className="home-editorial-section home-join-guide-section" aria-labelledby="home-guide-title" data-home-section="guide">
   <div className="home-section-inner">
    <header className="home-section-heading">
     <div>
      <p className="eyebrow">HƯỚNG DẪN THAM GIA</p>
      <h2 id="home-guide-title">Bắt đầu hành trình trong 3 bước</h2>
      <p>Chỉ cần bản game Minecraft Java Edition tiêu chuẩn, không yêu cầu cài đặt mod phức tạp.</p>
     </div>
    </header>
    <div className="home-guide-grid">
     <div className="home-guide-step">
      <span className="step-number">01</span>
      <div className="step-content">
       <h3>Khởi động Minecraft</h3>
       <p>Mở Minecraft Java Edition phiên bản <strong>1.20.4 – 1.21.x</strong> (khuyến nghị dùng OptiFine hoặc Iris Shader để có trải nghiệm đồ họa tối ưu).</p>
      </div>
     </div>

     <div className="home-guide-step">
      <span className="step-number">02</span>
      <div className="step-content">
       <h3>Thêm Máy Chủ</h3>
       <p>Vào mục <strong>Chơi Mạng (Multiplayer)</strong> → <strong>Thêm Máy Chủ (Add Server)</strong> rồi nhập địa chỉ:</p>
       <div className="step-ip-box">
        <code>play.novahaven.net</code>
        <CopyIpButton ip="play.novahaven.net" label="Chép" />
       </div>
      </div>
     </div>

     <div className="home-guide-step">
      <span className="step-number">03</span>
      <div className="step-content">
       <h3>Hòa Mình Vào Thế Giới</h3>
       <p>Tham gia máy chủ, lựa chọn chức nghiệp ban đầu và nhận chuỗi nhiệm vụ tân thủ tại Thung Lũng Sao.</p>
       <Link className="step-action-link" href="/wiki">Đọc cẩm nang người mới <span aria-hidden="true">→</span></Link>
      </div>
     </div>
    </div>
   </div>
  </section>

  <section className="home-editorial-section home-map-spotlight-section" aria-labelledby="home-map-title" data-home-section="map">
   <div className="home-section-inner">
    <div className="home-map-spotlight-card">
     <div className="map-spotlight-content">
      <p className="eyebrow">BẢN ĐỒ VÙNG ĐẤT TƯƠNG TÁC</p>
      <h2 id="home-map-title">Tra cứu địa hình & Tọa độ X/Z thời gian thực</h2>
      <p>Thế giới Nova Haven được mô phỏng chi tiết với đầy đủ địa hình núi non, sông suối và các khu định cư. Sử dụng bản đồ trực tuyến để định vị các điểm then chốt trên hành trình.</p>
      <div className="map-key-stats">
       <div className="map-stat-badge">
        <strong>Tọa độ Spawn</strong>
        <span>X: 191 · Z: -71</span>
       </div>
       <div className="map-stat-badge">
        <strong>Độ cao địa hình</strong>
        <span>Y: 46 đến 309</span>
       </div>
       <div className="map-stat-badge">
        <strong>Ranh giới thế giới</strong>
        <span>3,200 × 2,304 Blocks</span>
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
       <div className="map-spawn-pin" title="Điểm xuất hiện (X: 191, Z: -71)">
        <span className="pin-pulse" aria-hidden="true" />
        <span className="pin-label">Điểm Xuất Hiện</span>
       </div>
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
         <strong>Kênh Discord Cộng Đồng</strong>
         <small>Hơn 1,200 người chơi đồng hành</small>
        </div>
       </div>
       <p>Trao đổi lối build, lập tổ đội săn Boss và nhận thông báo bảo trì, sự kiện sớm nhất.</p>
       <a className="button button-blue discord-join-btn" href="https://discord.gg" target="_blank" rel="noreferrer">
        GIA NHẬP DISCORD <span aria-hidden="true">↗</span>
       </a>
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
    <p className="home-editorial-note">Hệ thống cẩm nang và tài liệu thế giới chính thức từ đội ngũ quản trị Nova Haven.</p>
   </div>
  </section>
 </>;
}
