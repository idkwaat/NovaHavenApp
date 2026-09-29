import type {Metadata} from 'next';
import Link from 'next/link';
import RecentWikiList from './RecentWikiList';

export const metadata:Metadata={
  title:'Đọc gần đây',
  description:'Mở lại các bài Wiki bạn vừa xem trên trình duyệt này.',
  robots:{index:false,follow:false},
};

export default function RecentPage(){
  return <section className="content detail bookmarks-page recent-page">
    <Link href="/wiki" className="bookmarks-back">← Quay lại Wiki</Link>
    <header className="bookmarks-heading">
      <p className="eyebrow">DẤU VẾT PHIÊU LƯU</p>
      <h1>Đọc gần đây</h1>
      <p>Lịch sử chỉ lưu trên trình duyệt này và tự ẩn bài không còn được xuất bản.</p>
    </header>
    <RecentWikiList/>
  </section>;
}
