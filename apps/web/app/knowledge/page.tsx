import Link from 'next/link';
import {knowledgeKindLabels,loadKnowledgeOverview} from '@/lib/knowledge-api';

const kindHeadings={npc:'NHÂN VẬT',quest:'NHIỆM VỤ',location:'ĐỊA DANH',season:'MÙA SỰ KIỆN'} as const;

export default async function Knowledge(){
 const overview=await loadKnowledgeOverview();
 return <section className="content knowledge-overview">
  <header className="knowledge-intro">
   <div><p className="eyebrow">THƯ VIỆN THẾ GIỚI · NOVA HAVEN</p><h1>Thư viện thế giới</h1><p>Tra cứu nhân vật, nhiệm vụ, địa danh và mùa sự kiện đã được xuất bản.</p></div>
   <div className="knowledge-count"><strong>{overview.status==='unavailable'?'—':overview.publishedTotal}</strong><span>{overview.status==='unavailable'?'CHƯA KẾT NỐI':overview.status==='partial'?'BÀI ĐÃ TẢI':'BÀI ĐÃ XUẤT BẢN'}</span></div>
  </header>

  {overview.status==='unavailable'&&<div className="knowledge-state knowledge-state-error" role="alert"><div><h2>Chưa tải được thư viện</h2><p>Dịch vụ dữ liệu thế giới đang tạm gián đoạn. Bạn có thể thử tải lại hoặc tiếp tục đọc Wiki.</p></div><div className="knowledge-state-actions"><Link href="/knowledge">Thử lại</Link><Link href="/wiki">Mở thư viện Wiki</Link></div></div>}
  {overview.status==='partial'&&<div className="knowledge-state knowledge-state-partial" role="status"><p>Một số nhóm nội dung chưa tải được: {overview.failedKinds.map(kind=>knowledgeKindLabels[kind]).join(', ')}.</p><Link href="/knowledge">Tải lại thư viện</Link></div>}
  {overview.status!=='unavailable'&&overview.publishedTotal===0&&<div className="knowledge-state knowledge-state-empty" role="status"><div><h2>Chưa có bài thế giới được xuất bản</h2><p>Các bài trong bản nháp chỉ xuất hiện sau khi biên tập viên xuất bản. Trong lúc chờ, bạn có thể đọc cẩm nang Wiki.</p></div><Link href="/wiki">Đọc cẩm nang Wiki</Link></div>}

  {overview.entries.length>0&&<div className="knowledge-kind-grid">{overview.entries.map(({kind,page},index)=><Link className={`article-card knowledge-card knowledge-card-${kind}`} href={`/knowledge/${kind}`} key={kind}>
   <div className="knowledge-card-top"><small>{kindHeadings[kind]}</small><span aria-hidden="true">{String(index+1).padStart(2,'0')}</span></div>
   <h2>{knowledgeKindLabels[kind]}</h2>
   <p>{page.total===0?'Chưa có bài xuất bản.':`${page.total} bài đã xuất bản.`}</p>
   <span className="knowledge-card-link">{page.total===0?'Xem nhóm nội dung':'Khám phá nội dung'} <span aria-hidden="true">→</span></span>
  </Link>)}</div>}
 </section>;
}
