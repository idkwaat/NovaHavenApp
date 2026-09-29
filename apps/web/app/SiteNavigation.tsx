import Link from 'next/link';
import SiteIcon from './SiteIcon';
import CartStatus from './CartStatus';
import NotificationBell from './NotificationBell';
import {minecraftServerAddress} from '@/lib/public-site-config';

const links = [
  {href: '/', label: 'Trang chủ', note: 'Nova Haven'},
  {href: '/wiki', label: 'Wiki', note: 'Cẩm nang thế giới'},
  {href: '/bookmarks', label: 'Bài đã lưu', note: 'Danh sách đọc riêng trên máy này'},
  {href: '/recent', label: 'Đọc gần đây', note: 'Mở lại bài vừa xem'},
  {href: '/knowledge', label: 'Thư viện thế giới', note: 'Nhân vật · nhiệm vụ · địa danh'},
  {href: '/map', label: 'Bản đồ thế giới', note: 'Địa hình · tọa độ X/Z'},
  {href: '/catalog', label: 'Vật phẩm', note: 'Danh mục biên tập'},
  {href: '/commerce', label: 'Cửa hàng', note: 'Vật phẩm & gói hỗ trợ'},
  {href: '/community', label: 'Cộng đồng', note: 'Guild & hoạt động'},
  {href: '/news', label: 'Tin tức', note: 'Cập nhật mới nhất'},
  {href: '/notifications', label: 'Thông báo', note: 'Hộp thư tài khoản'},
  {href: '/account', label: 'Tài khoản', note: 'Đăng nhập & đăng ký'},
  {href: '/admin', label: 'Quản trị', note: 'Nova CMS'},
];

export default function SiteNavigation() {
  return <header className="site-header">
    <div className="site-nav-menu">
      <input className="site-nav-toggle" id="site-nav-toggle" type="checkbox" aria-label="Mở menu" aria-controls="site-nav-drawer" />
      <label className="menu-button" htmlFor="site-nav-toggle" title="Mở điều hướng"><SiteIcon name="menu" className="ui-icon"/></label>
      <div className="site-nav-layer">
        <label className="site-nav-backdrop" htmlFor="site-nav-toggle" aria-label="Đóng menu" />
        <aside className="site-nav-drawer" id="site-nav-drawer" aria-label="Điều hướng chính">
        <div className="site-nav-heading">
          <div><span className="eyebrow">NOVA HAVEN</span><strong>Đi đâu tiếp?</strong></div>
          <label className="menu-button site-nav-close" htmlFor="site-nav-toggle" aria-label="Đóng menu" title="Nhấn ☰ để đóng">
            <SiteIcon name="menu" className="ui-icon"/>
            <span className="sr-only">Nhấn ☰ để đóng</span>
          </label>
        </div>
        <nav className="site-nav-links">
          {links.map(link => <Link href={link.href} key={link.href}>
            <span>{link.label}</span><small>{link.note}</small><SiteIcon name="arrow" className="nav-arrow"/>
          </Link>)}
        </nav>
        <p className="site-nav-footer">Cổng thông tin Nova Haven · Dữ liệu local</p>
        </aside>
      </div>
    </div>
    <div className="game-toolbar">
      <Link className="home-button" href="/" aria-label="Về trang chủ" title="Trang chủ"><SiteIcon name="home" className="ui-icon"/><span>Trang chủ</span></Link>
      <Link className="signin-button" href="/account"><span className="signin-icon"><SiteIcon name="user" className="ui-icon"/></span> Đăng nhập</Link>
      <NotificationBell/>
      <Link className="server-pill" href="/community" aria-label="Thông tin máy chủ: chưa có trạng thái trực tiếp" title="Địa chỉ chỉ xuất hiện khi được cấu hình">{minecraftServerAddress??'Máy chủ chưa công bố'} <SiteIcon name="arrow" className="server-arrow"/></Link>
      <CartStatus/>
      <details className="search-popover">
        <summary className="search-button" aria-label="Mở tìm kiếm" title="Tìm Wiki"><SiteIcon name="search" className="ui-icon"/></summary>
        <form action="/wiki" className="toolbar-search-form">
          <label className="sr-only" htmlFor="toolbar-search">Tìm bài viết trong Wiki</label>
          <input id="toolbar-search" type="search" name="q" placeholder="Tìm trong Wiki…" maxLength={100}/>
          <button type="submit"><SiteIcon name="search" className="ui-icon"/><span>Tìm</span></button>
        </form>
      </details>
    </div>
  </header>;
}
