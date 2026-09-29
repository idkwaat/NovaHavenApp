'use client';

import Link from 'next/link';
import {useEffect,useState} from 'react';
import type {WikiArticle} from '@/lib/wiki-api';
import {filterBookmarkedArticles,loadPublishedWikiArticles} from '@/lib/wiki-bookmarks';
import {useWikiBookmarks} from '@/lib/use-wiki-bookmarks';
import BookmarkButton from '../wiki/[slug]/BookmarkButton';

export default function BookmarkList(){
  const {slugs,ready,storageError}=useWikiBookmarks();
  const [articles,setArticles]=useState<WikiArticle[]>([]);
  const [unavailableCount,setUnavailableCount]=useState(0);
  const [loading,setLoading]=useState(false);
  const [loadError,setLoadError]=useState(false);
  const [attempt,setAttempt]=useState(0);
  const [query,setQuery]=useState('');

  useEffect(()=>{
    if(!ready)return;
    let active=true;
    setLoadError(false);
    setArticles([]);
    setUnavailableCount(0);
    if(storageError||slugs.length===0){setLoading(false);return()=>{active=false;};}
    setLoading(true);
    loadPublishedWikiArticles(slugs).then(result=>{
      if(!active)return;
      setArticles(result.articles);
      setUnavailableCount(result.unavailableCount);
      setLoading(false);
    }).catch(()=>{
      if(!active)return;
      setLoadError(true);
      setLoading(false);
    });
    return()=>{active=false;};
  },[ready,storageError,slugs,attempt]);

  if(!ready)return <p className="bookmark-state" role="status">Đang đọc danh sách trên trình duyệt…</p>;
  if(storageError)return <div className="bookmark-state bookmark-state-error" role="alert"><h2>Không thể truy cập danh sách đọc</h2><p>Trình duyệt đang chặn bộ nhớ cục bộ. Hãy cho phép lưu dữ liệu của trang rồi tải lại.</p></div>;
  if(loading)return <p className="bookmark-state" role="status">Đang kiểm tra bài nào còn được xuất bản…</p>;
  if(loadError)return <div className="bookmark-state bookmark-state-error" role="alert"><h2>Chưa tải được bài đã lưu</h2><p>Wiki chưa phản hồi ổn định. Danh sách của bạn vẫn nằm trên trình duyệt này.</p><button type="button" className="wiki-bookmark-button" onClick={()=>setAttempt(value=>value+1)}>Thử lại</button></div>;
  if(slugs.length===0)return <div className="bookmark-state bookmark-state-empty"><h2>Chưa có bài nào trong danh sách</h2><p>Mở một bài Wiki rồi chọn “Lưu bài” để giữ lại những nội dung muốn đọc sau.</p><Link className="bookmark-primary-link" href="/wiki">Khám phá Wiki</Link></div>;
  if(articles.length===0)return <div className="bookmark-state bookmark-state-empty"><h2>Không còn bài công khai trong danh sách</h2><p>Bài có thể đã được gỡ xuất bản. Những slug đã lưu vẫn được giữ trên trình duyệt.</p><Link className="bookmark-primary-link" href="/wiki">Tìm bài khác</Link></div>;

  const visibleArticles=filterBookmarkedArticles(articles,query);

  return <div className="bookmark-results">
    <div className="bookmark-results-heading"><h2>{visibleArticles.length===articles.length?articles.length:`${visibleArticles.length} / ${articles.length}`} {articles.length===1?'bài':'bài viết'}</h2><span>đã kiểm tra từ Wiki công khai</span></div>
    {unavailableCount>0&&<p className="bookmark-unavailable" role="status">{unavailableCount} bài đã lưu hiện không còn công khai nên được ẩn khỏi danh sách.</p>}
    <div className="bookmark-search"><label htmlFor="saved-wiki-search">Tìm trong bài đã lưu</label><div><input id="saved-wiki-search" type="search" value={query} maxLength={100} placeholder="Tên bài, nội dung hoặc tag…" onChange={event=>setQuery(event.target.value)}/>{query&&<button type="button" onClick={()=>setQuery('')}>Xóa tìm kiếm</button>}</div></div>
    {visibleArticles.length===0?<p className="bookmark-state" role="status">Không tìm thấy bài nào khớp với “{query}”.</p>:<div className="bookmark-grid">{visibleArticles.map(article=><article className="bookmark-card" key={article.slug}>
      <p className="eyebrow">Bài Wiki · Phiên bản {article.revision}</p>
      <h3><Link href={`/wiki/${encodeURIComponent(article.slug)}`}>{article.title}</Link></h3>
      <p>{article.summary}</p>
      <div className="bookmark-card-footer"><Link className="bookmark-read-link" href={`/wiki/${encodeURIComponent(article.slug)}`}>Đọc bài <span aria-hidden="true">→</span></Link><BookmarkButton slug={article.slug} title={article.title}/></div>
    </article>)}</div>}
  </div>;
}
