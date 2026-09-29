import Link from 'next/link';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import rehypeSanitize from 'rehype-sanitize';
import {omitRepeatedTitleHeading} from '@/lib/markdown-display';
import {catalogApi,catalogKindLabels,type CatalogKind} from '@/lib/catalog-api';
import DemoDataNotice from '@/components/DemoDataNotice';

export default async function CatalogItem({params}:{params:Promise<{slug:string}>}){
  const {slug}=await params;
  try{
    const item=await catalogApi.item(slug);
    const markdown=omitRepeatedTitleHeading(item.markdown,item.name);
    return <article className="content detail"><Link href="/catalog">← Quay lại danh mục vật phẩm</Link><DemoDataNotice/><p className="eyebrow">{catalogKindLabels[item.kind as CatalogKind]??item.kind} · Bản biên tập {item.revision}</p><h1>{item.name}</h1><p className="intro">{item.summary}</p><div className="markdown"><ReactMarkdown remarkPlugins={[remarkGfm]} rehypePlugins={[rehypeSanitize]} skipHtml>{markdown}</ReactMarkdown></div></article>;
  }catch{return <section className="content"><Link href="/catalog">← Quay lại Catalog</Link><h1>Không tìm thấy vật phẩm</h1><p className="notice">Vật phẩm chưa được xuất bản hoặc không tồn tại.</p></section>;}
}
