import type {Metadata} from 'next';
import Link from 'next/link';
import {notFound} from 'next/navigation';
import {createElement,type ReactNode} from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import rehypeSanitize from 'rehype-sanitize';
import {omitRepeatedTitleHeading} from '@/lib/markdown-display';
import {wikiApi} from '@/lib/wiki-api';
import {isSafeLink,markdownToc,wikiCategoryLabel,wikiListHref} from '@/lib/wiki-model';
import ArticlePreview from '../../ArticlePreview';
import {articlePreviewFor} from '@/lib/article-previews';
import BookmarkButton from './BookmarkButton';
import RecentArticleTracker from './RecentArticleTracker';
type Props={params:Promise<{slug:string}>};
export async function generateMetadata({params}:Props):Promise<Metadata>{try{const {slug}=await params;const item=await wikiApi.article(slug);return{title:item.title,description:item.summary,alternates:{canonical:`/wiki/${encodeURIComponent(item.slug)}`},openGraph:{type:'article',title:item.title,description:item.summary,url:`/wiki/${encodeURIComponent(item.slug)}`},robots:{index:true,follow:true}};}catch{return{title:'Wiki',robots:{index:false}};}}
export default async function ArticlePage({params}:Props){
  let article:Awaited<ReturnType<typeof wikiApi.article>>;
  let categories:Awaited<ReturnType<typeof wikiApi.categories>>=[];
  try{
    const slug=(await params).slug;
    [article,categories]=await Promise.all([wikiApi.article(slug),wikiApi.categories().catch(()=>[])]);
  }catch{notFound();}
  const categoryLabel=wikiCategoryLabel(article.category,categories);
  const markdown=omitRepeatedTitleHeading(article.markdown,article.title);
  const toc=markdownToc(markdown);
  const headingCursor={value:0};
  const renderHeading=(level:number)=>(props:{children?:ReactNode})=>{
    const id=toc[headingCursor.value++]?.id;
    return createElement(`h${level}`,id?{id}:undefined,props.children);
  };
  return <article className="content detail">
    <RecentArticleTracker slug={article.slug}/>
    <Link href="/wiki">← Quay lại Wiki</Link>
    <p className="eyebrow">{categoryLabel} · Bản biên tập {article.revision}</p>
    <h1>{article.title}</h1>
    <BookmarkButton slug={article.slug} title={article.title}/>
    <div className="tag-list">{article.tags.map(tag=><Link key={tag} href={wikiListHref({tag})}>#{tag}</Link>)}</div>
    <p className="intro">{article.summary}</p>
    {toc.length>0&&<nav aria-label="Mục lục Wiki" className="notice">
      <strong>Mục lục</strong>
      <ol>{toc.map(item=><li key={item.id} style={{marginLeft:`${(item.level-1)*1}rem`}}><a href={`#${item.id}`}>{item.text}</a></li>)}</ol>
    </nav>}
    <div className="markdown"><ReactMarkdown
      remarkPlugins={[remarkGfm]}
      rehypePlugins={[rehypeSanitize]}
      skipHtml
      components={{
        h1:renderHeading(1),
        h2:renderHeading(2),
        h3:renderHeading(3),
        a:({href,children})=>href&&isSafeLink(href)?<a href={href} rel="noopener noreferrer" target="_blank">{children}</a>:<span>{children}</span>,
        img:({src,alt})=>typeof src==='string'&&src.startsWith('/api/v1/wiki/media/')?<img src={src} alt={alt??''}/>:<span>{alt}</span>,
      }}
    >{markdown}</ReactMarkdown></div>
    {article.related.length>0&&<section>
      <h2>Bài viết liên quan</h2>
      <div className="article-grid">{article.related.map(item=>{
        const preview=articlePreviewFor({collection:'wiki',topic:item.category,slug:item.slug,title:item.title});
        return <article className="article-card editorial-card" key={item.id}>
          <ArticlePreview image={preview}/>
          <div className="editorial-card-copy"><small>{wikiCategoryLabel(item.category,categories)} · Phiên bản {item.revision}</small><h3><Link href={`/wiki/${encodeURIComponent(item.slug)}`}>{item.title}</Link></h3><p>{item.summary||'Đọc nội dung chi tiết →'}</p><Link className="editorial-read-link" href={`/wiki/${encodeURIComponent(item.slug)}`}>Đọc bài →</Link></div>
        </article>;
      })}</div>
    </section>}
    <p className="updated">Xuất bản: {new Date(article.publishedAt).toLocaleDateString('vi-VN',{timeZone:'UTC'})}</p>
  </article>;
}
