import type {Metadata} from 'next';
import Link from 'next/link';
import SiteNavigation from './SiteNavigation';
import './styles.css';
import './wynn-parity.css';
import './commerce/store.css';
import './goodgames-theme.css';
import './account-notifications.css';
const siteUrl=process.env.NEXT_PUBLIC_SITE_URL??'http://localhost:3000';
export const metadata:Metadata={
 metadataBase:new URL(siteUrl),
 title:{default:'Nova Haven | Minecraft RPG',template:'%s | Nova Haven'},
 description:'Khám phá thế giới và Wiki hướng dẫn Nova Haven.',
 alternates:{canonical:'/'},
 openGraph:{type:'website',locale:'vi_VN',siteName:'Nova Haven',title:'Nova Haven | Minecraft RPG',description:'Khám phá thế giới và Wiki hướng dẫn Nova Haven.',url:'/'},
 robots:{index:true,follow:true}
};
export default function RootLayout({children}:{children:React.ReactNode}) {
 return <html lang="vi"><head><link rel="preconnect" href="https://fonts.googleapis.com"/><link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous"/><link href="https://fonts.googleapis.com/css2?family=Montserrat:wght@600;700;800;900&family=Open+Sans:wght@400;600;700&display=swap" rel="stylesheet"/></head><body><SiteNavigation/><main>{children}</main><footer className="site-footer"><div className="footer-brand"><strong>NOVA HAVEN</strong><span>Wiki · cộng đồng · phiêu lưu</span></div><nav className="footer-links" aria-label="Điều hướng cuối trang"><Link href="/">Trang chủ</Link><Link href="/wiki">Wiki</Link><Link href="/bookmarks">Bài đã lưu</Link><Link href="/knowledge">Thế giới</Link><Link href="/map">Bản đồ</Link><Link href="/catalog">Vật phẩm</Link><Link href="/commerce">Cửa hàng</Link><Link href="/cart">Giỏ hàng</Link><Link href="/community">Cộng đồng</Link><Link href="/news">Tin tức</Link><Link href="/admin">Quản trị</Link></nav><div className="footer-meta"><span>© 2026 Nova Haven. Bảo lưu mọi quyền.</span><span>Máy chủ RPG không liên kết với Mojang hay Microsoft.</span><span style={{display: 'none'}} aria-hidden="true"><span>play.novahaven.local · bản xem trước local</span><a href="https://unsplash.com/photos/green-meadow-with-trees-and-a-river-near-mountains-X7FEgrPDFNU" target="_blank" rel="noreferrer">Ảnh phong cảnh: Einar Storsul · Unsplash</a><a href="https://commons.wikimedia.org/wiki/File:Minecraft_-_1.18_mountains.jpg" target="_blank" rel="noreferrer">Ảnh Minecraft: Xbox México · CC BY 3.0</a></span></div></footer></body></html>;
}
