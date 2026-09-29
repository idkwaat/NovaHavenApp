import Link from 'next/link';
import {knowledgeApi,knowledgeKindLabels,type KnowledgeKind} from '@/lib/knowledge-api';
import ArticlePreview from '../../ArticlePreview';
import {articlePreviewFor} from '@/lib/article-previews';

function isKind(value:string):value is KnowledgeKind{return ['npc','quest','location','season'].includes(value);}

export default async function KnowledgeKindPage({params}:{params:Promise<{kind:string}>}){
 const {kind}=await params;
 if(!isKind(kind))return <section className="content"><h1>Không tìm thấy nhóm nội dung</h1></section>;
 try{
  const result=await knowledgeApi.list(kind);
  return <section className="content"><Link href="/knowledge">← Thư viện thế giới</Link><p className="eyebrow">{kind.toUpperCase()}</p><h1>{knowledgeKindLabels[kind]}</h1><p>Chỉ hiển thị bản xuất bản hiện tại.</p>{result.items.length?<div className="article-grid">{result.items.map(item=>{
   const preview=articlePreviewFor({collection:kind,topic:kind,slug:item.slug,title:item.name,imageUrl:item.previewImageUrl,imageAlt:item.name});
   return <article className="article-card editorial-card" key={item.id}>
    <ArticlePreview image={preview}/>
    <div className="editorial-card-copy"><small>Phiên bản {item.revision}</small><h2><Link href={`/knowledge/${kind}/${item.slug}`}>{item.name}</Link></h2><p>{item.summary}</p><Link className="editorial-read-link" href={`/knowledge/${kind}/${item.slug}`}>Xem chi tiết →</Link></div>
   </article>;
  })}</div>:<div className="notice">Chưa có nội dung đã xuất bản cho nhóm này.</div>}</section>;
 }catch{return <section className="content"><Link href="/knowledge">← Thư viện thế giới</Link><h1>{knowledgeKindLabels[kind]}</h1><div className="notice">Không tải được nội dung.</div></section>;}
}
