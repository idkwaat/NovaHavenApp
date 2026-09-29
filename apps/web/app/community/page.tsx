import Link from 'next/link';
import {communityKindLabels,communityKinds,loadCommunityOverview} from '@/lib/community-api';
import DemoDataNotice from '@/components/DemoDataNotice';

const kindHeadings={event:'SỰ KIỆN',guild:'BANG HỘI',player:'NGƯỜI CHƠI',housing:'KHÔNG GIAN SỐNG',leaderboard:'BẢNG XẾP HẠNG'} as const;

export default async function Community(){
 const overview=await loadCommunityOverview();
 const entries=new Map(overview.entries.map(entry=>[entry.kind,entry.page]));

 return <section className="content community-overview">
 <header className="community-intro">
   <div><p className="eyebrow">CỘNG ĐỒNG · NOVA HAVEN</p><h1>Cộng đồng</h1><p>Gặp gỡ các bang hội, theo dõi sự kiện và khám phá những dấu ấn do cộng đồng cùng biên tập.</p></div>
   <div className="community-count"><strong>{overview.status==='unavailable'?'—':overview.publishedTotal}</strong><span>{overview.status==='unavailable'?'CHƯA KẾT NỐI':'NỘI DUNG ĐÃ XUẤT BẢN'}</span></div>
  </header>
  <DemoDataNotice/>

  {overview.status==='unavailable'&&<div className="community-state community-state-error" role="alert">
   <div><h2>Chưa tải được các mục cộng đồng</h2><p>Dịch vụ dữ liệu đang tạm gián đoạn. Hãy thử lại sau hoặc ghé thư viện Wiki trong lúc chờ.</p></div>
   <div className="community-state-actions"><Link href="/community">Thử tải lại</Link><Link href="/wiki">Mở thư viện Wiki</Link></div>
  </div>}
  {overview.status==='partial'&&<div className="community-state community-state-partial" role="status">
   <p>Một số mục chưa tải được: {overview.failedKinds.map(kind=>communityKindLabels[kind]).join(', ')}. Những mục đã tải vẫn dùng được.</p><Link href="/community">Tải lại</Link>
  </div>}
  {overview.status!=='unavailable'&&overview.publishedTotal===0&&<div className="community-state community-state-empty" role="status">
   <div><h2>Chưa có nội dung cộng đồng được xuất bản</h2><p>Các nhóm bên dưới sẽ hiện nội dung tại đây sau khi được biên tập và xuất bản.</p></div><Link href="/wiki">Đọc thư viện Wiki</Link>
  </div>}

  {overview.status!=='unavailable'&&<div className="community-overview-grid">
   {communityKinds.map((kind,index)=>{
    const page=entries.get(kind);
    const heading=<><div className="community-card-top"><small>{kindHeadings[kind]}</small><span aria-hidden="true">{String(index+1).padStart(2,'0')}</span></div><h2>{communityKindLabels[kind]}</h2></>;
    if(page)return <Link className={`article-card community-card community-card-${kind}`} href={`/community/${kind}`} key={kind}>{heading}
     <p>{page.total===0?'Chưa có nội dung được xuất bản.':`${page.total} nội dung đã xuất bản.`}</p>
     <span className="community-card-link">{page.total===0?'Xem mục này':'Khám phá nội dung'} <span aria-hidden="true">→</span></span>
    </Link>;
    return <article className={`article-card community-card community-card-${kind} community-card-unavailable`} key={kind}>{heading}
     <p>Tạm thời chưa tải được mục này.</p><Link className="community-retry-link" href="/community" aria-label={`Tải lại mục ${communityKindLabels[kind]}`}>Tải lại mục này</Link>
    </article>;
   })}
  </div>}
 </section>;
}
