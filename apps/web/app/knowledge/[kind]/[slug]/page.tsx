import Link from 'next/link';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import rehypeSanitize from 'rehype-sanitize';
import {omitRepeatedTitleHeading} from '@/lib/markdown-display';
import {knowledgeApi,knowledgeKindLabels,type KnowledgeKind} from '@/lib/knowledge-api';

function isKind(value:string):value is KnowledgeKind{return ['npc','quest','location','season'].includes(value);}

export default async function KnowledgeDetail({params}:{params:Promise<{kind:string;slug:string}>}){
 const {kind,slug}=await params;
 if(!isKind(kind))return <section className="content"><h1>Không tìm thấy nội dung</h1></section>;
 try{
  const item=await knowledgeApi.detail(kind,slug);
  const metadata=item.metadata;
  const markdown=omitRepeatedTitleHeading(item.markdown,item.name);
  const relationHref=(type:string,targetSlug:string)=>type==='catalog'?`/catalog/${targetSlug}`:isKind(type)?`/knowledge/${type}/${targetSlug}`:null;
  return <article className="content detail"><Link href={`/knowledge/${kind}`}>← {knowledgeKindLabels[kind]}</Link><p className="eyebrow">{knowledgeKindLabels[kind].toLocaleUpperCase('vi-VN')} · Bản biên tập {item.revision}</p><h1>{item.name}</h1><p className="intro">{item.summary}</p><div className="knowledge-metadata"><strong>Thông tin biên tập</strong>{kind==='npc'&&<p>Vai trò: {metadata.role}</p>}{kind==='quest'&&<><p>Độ khó: {metadata.difficulty}/5</p><p>Phần thưởng mô tả: {metadata.rewardDescription}</p>{metadata.steps?.length?<ol>{metadata.steps.map(step=><li key={step.position}><strong>{step.title}</strong> — {step.description}</li>)}</ol>:null}</>}{kind==='location'&&<p>{metadata.region} · {metadata.locationType}{metadata.latitude!=null&&metadata.longitude!=null?` · ${metadata.latitude}, ${metadata.longitude}`:''}</p>}{kind==='season'&&<p>{metadata.theme} · {metadata.startsAt?new Date(metadata.startsAt).toLocaleDateString('vi-VN'):''} – {metadata.endsAt?new Date(metadata.endsAt).toLocaleDateString('vi-VN'):''}</p>}</div><div className="markdown"><ReactMarkdown remarkPlugins={[remarkGfm]} rehypePlugins={[rehypeSanitize]} skipHtml>{markdown}</ReactMarkdown></div>{item.links.length?<section className="knowledge-links"><h2>Liên kết trong đồ thị</h2><div className="article-grid">{item.links.map((link,index)=>{const href=relationHref(link.type,link.slug);return href?<Link className="article-card" href={href} key={`${link.slug}-${index}`}><small>{link.linkType}</small><h3>{link.name}</h3><span>Xem liên kết →</span></Link>:<div className="article-card" key={`${link.slug}-${index}`}><small>{link.linkType}</small><h3>{link.name}</h3></div>;})}</div></section>:null}</article>;
 }catch{return <section className="content"><Link href={`/knowledge/${kind}`}>← Quay lại</Link><h1>Không tìm thấy nội dung</h1><p className="notice">Nội dung chưa xuất bản hoặc không tồn tại.</p></section>;}
}
