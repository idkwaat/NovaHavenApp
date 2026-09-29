import type {Metadata} from 'next';
import Link from 'next/link';
import BookmarkList from './BookmarkList';

export const metadata:Metadata={
  title:'Bài đã lưu',
  description:'Danh sách bài Wiki bạn lưu trên trình duyệt này.',
  robots:{index:false,follow:false},
};

export default function BookmarksPage(){
  return <section className="content detail bookmarks-page">
    <Link href="/wiki" className="bookmarks-back">← Quay lại Wiki</Link>
    <header className="bookmarks-heading">
      <p className="eyebrow">THƯ VIỆN CỦA BẠN</p>
      <h1>Bài đã lưu</h1>
      <p>Các bài được lưu riêng trên trình duyệt này. Nội dung luôn được tải lại từ Wiki công khai.</p>
    </header>
    <BookmarkList/>
  </section>;
}
