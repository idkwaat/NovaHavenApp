import Link from 'next/link';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import rehypeSanitize from 'rehype-sanitize';
import {omitRepeatedTitleHeading} from '@/lib/markdown-display';

type NewsDetail={id:string;slug:string;title:string;summary:string;markdown:string;publishedAt:string};
const apiOrigin=process.env.NOVA_API_ORIGIN??'http://localhost:5080';

export default async function NewsArticle({params}:{params:Promise<{slug:string}>}){
 const {slug}=await params;
 const response=await fetch(new URL(`/api/v1/news/${encodeURIComponent(slug)}`,apiOrigin),{cache:'no-store'});
 if(!response.ok)return <section className="content"><Link href="/news">← Quay lại News</Link><h1>Không tìm thấy tin</h1></section>;
 const item=await response.json() as NewsDetail;
 const markdown=omitRepeatedTitleHeading(item.markdown,item.title);
 return <article className="content detail"><Link href="/news">← Quay lại Tin tức</Link><p className="eyebrow">TIN TỨC · {new Date(item.publishedAt).toLocaleDateString('vi-VN',{timeZone:'UTC'})}</p><h1>{item.title}</h1><p className="intro">{item.summary}</p><div className="markdown"><ReactMarkdown remarkPlugins={[remarkGfm]} rehypePlugins={[rehypeSanitize]} skipHtml>{markdown}</ReactMarkdown></div></article>;
}
