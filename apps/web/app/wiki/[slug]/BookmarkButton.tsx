'use client';

import {useWikiBookmarks} from '@/lib/use-wiki-bookmarks';

export default function BookmarkButton({slug,title}:{slug:string;title:string}){
  const {slugs,ready,storageError,toggle}=useWikiBookmarks();
  const saved=slugs.includes(slug);
  const label=saved?'Bỏ lưu bài':'Lưu bài';
  return <div className="wiki-bookmark-action">
    <button type="button" className="wiki-bookmark-button" aria-pressed={saved} aria-label={`${label}: ${title}`} disabled={!ready||storageError} onClick={()=>toggle(slug)}>
      <span aria-hidden="true" className="wiki-bookmark-mark">{saved?'✓':'+'}</span>{label}
    </button>
    {storageError&&<span className="wiki-bookmark-status" role="status">Trình duyệt hiện không cho phép lưu danh sách đọc.</span>}
  </div>;
}
