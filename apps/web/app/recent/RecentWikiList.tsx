'use client';

import Link from 'next/link';
import {useEffect,useState} from 'react';
import type {WikiArticle} from '@/lib/wiki-api';
import {loadPublishedWikiArticles} from '@/lib/wiki-bookmarks';
import {useWikiHistory} from '@/lib/use-wiki-history';

export default function RecentWikiList(){
  const {slugs,ready,storageError,clear}=useWikiHistory();
  const [articles,setArticles]=useState<WikiArticle[]>([]);
  const [unavailableCount,setUnavailableCount]=useState(0);
  const [loading,setLoading]=useState(false);
  const [loadError,setLoadError]=useState(false);
  const [attempt,setAttempt]=useState(0);
  const [announcement,setAnnouncement]=useState('');

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

  function clearHistory(){
    if(!window.confirm('Xóa lịch sử đọc được lưu trên trình duyệt này?'))return;
    if(clear())setAnnouncement('Đã xóa lịch sử đọc trên trình duyệt này.');
  }

  if(!ready)return <p className="bookmark-state" role="status">Đang đọc lịch sử trên trình duyệt…</p>;
  if(storageError)return <div className="bookmark-state bookmark-state-error" role="alert"><h2>Không thể truy cập lịch sử đọc</h2><p>Trình duyệt đang chặn bộ nhớ cục bộ. Hãy cho phép lưu dữ liệu của trang rồi tải lại.</p></div>;
  if(loading)return <p className="bookmark-state" role="status">Đang kiểm tra bài nào còn được xuất bản…</p>;
  if(loadError)return <div className="bookmark-state bookmark-state-error" role="alert"><h2>Chưa tải được lịch sử đọc</h2><p>Lịch sử vẫn được giữ trên trình duyệt này, nhưng Wiki chưa phản hồi ổn định.</p><button type="button" className="wiki-bookmark-button" onClick={()=>setAttempt(value=>value+1)}>Thử lại</button></div>;
  if(slugs.length===0)return <div className="bookmark-state bookmark-state-empty"><h2>Bạn chưa mở bài Wiki nào</h2><p>Những bài đã đọc sẽ xuất hiện ở đây để bạn có thể quay lại nhanh.</p><Link className="bookmark-primary-link" href="/wiki">Khám phá Wiki</Link><p className="recent-announcement" role="status">{announcement}</p></div>;
  if(articles.length===0)return <div className="bookmark-state bookmark-state-empty"><h2>Không còn bài công khai trong lịch sử</h2><p>Bài đã được gỡ sẽ không hiện nội dung cũ tại đây.</p><button type="button" className="wiki-bookmark-button" onClick={clearHistory}>Xóa lịch sử</button><p className="recent-announcement" role="status">{announcement}</p></div>;

  return <div className="bookmark-results">
    <div className="recent-results-heading"><div className="bookmark-results-heading"><h2>{articles.length} bài gần đây</h2><span>lưu trên thiết bị này</span></div><button type="button" className="wiki-bookmark-button recent-clear-button" onClick={clearHistory}>Xóa lịch sử</button></div>
    {unavailableCount>0&&<p className="bookmark-unavailable" role="status">{unavailableCount} bài cũ đã gỡ xuất bản nên được ẩn khỏi danh sách.</p>}
    <p className="recent-announcement" role="status">{announcement}</p>
    <div className="bookmark-grid">{articles.map(article=><article className="bookmark-card" key={article.slug}>
      <p className="eyebrow">Bài Wiki · Phiên bản {article.revision}</p>
      <h3><Link href={`/wiki/${encodeURIComponent(article.slug)}`}>{article.title}</Link></h3>
      <p>{article.summary}</p>
      <div className="bookmark-card-footer"><Link className="bookmark-read-link" href={`/wiki/${encodeURIComponent(article.slug)}`}>Đọc tiếp <span aria-hidden="true">→</span></Link><span className="recent-entry-note">Vừa xem</span></div>
    </article>)}</div>
  </div>;
}
