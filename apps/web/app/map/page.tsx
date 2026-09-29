import type {Metadata} from 'next';
import Link from 'next/link';
import {join} from 'node:path';
import {getBlueMapLocalOrigin} from '@/lib/bluemap-local-server';
import {loadWorldMapAssets} from '@/lib/world-map-server';
import BlueMapViewer from './BlueMapViewer';
import WorldMapExplorer from './WorldMapExplorer';
import styles from './world-map.module.css';

export const dynamic = 'force-dynamic';

export const metadata: Metadata = {
  title: 'Bản đồ thế giới',
  description: 'Khám phá địa hình Nova Haven và tra cứu tọa độ block X/Z.',
};

export default async function WorldMapPage() {
  const [manifest, blueMapOrigin] = await Promise.all([
    loadWorldMapAssets(join(process.cwd(), 'public')),
    getBlueMapLocalOrigin(),
  ]);
  if (blueMapOrigin) {
    return <section className={styles.page}>
      <div className={styles.breadcrumbs}><Link href="/">Trang chủ</Link><span aria-hidden="true">/</span><span>Bản đồ</span></div>
      <header className={styles.heading}>
        <div>
          <p className={styles.eyebrow}>NOVA HAVEN · BẢN ĐỒ ĐỊA HÌNH</p>
          <h1>Bản đồ thế giới</h1>
          <p className={styles.intro}>Bản đồ 3D mở ở góc phối cảnh nghiêng như trong game. Kéo để di chuyển, zoom sát để xem địa hình và từng block; chế độ bay tự do đã tắt.</p>
        </div>
        <div className={styles.mapFacts}>
          <strong>{manifest?.chunksRendered?.toLocaleString('vi-VN') ?? 'World local'}</strong>
          <span>{manifest ? 'chunk mặt địa hình trong atlas' : 'BlueMap CLI · Java 21'}</span>
          <small>Perspective · không có live player tracking</small>
        </div>
      </header>
      <BlueMapViewer origin={blueMapOrigin}/>
      <p className={styles.coordinateNote}>Đây là bản đồ local từ world đã giải nén; khi world thay đổi, chạy lại lệnh render. Không có Minecraft server hoặc player tracking trực tiếp.</p>
    </section>;
  }

  if (!manifest) {
    return <section className={styles.page}>
      <p className={styles.eyebrow}>NOVA HAVEN · THẾ GIỚI</p>
      <h1>Bản đồ thế giới</h1>
      <div className={styles.unavailable} role="status">
        <h2>Chưa có bản đồ đã render</h2>
        <p>Trang này cần BlueMap local hoặc atlas được render từ world Minecraft. Hiện chưa tìm thấy viewer BlueMap đang chạy hay đủ manifest và ảnh bản đồ cục bộ.</p>
        <p>Đặt Java 21, sau đó chạy <code>python scripts/bluemap_local.py start --world &quot;đường-dẫn-tới-world&quot;</code>.</p>
        <Link href="/knowledge">Mở thư viện thế giới <span aria-hidden="true">→</span></Link>
      </div>
    </section>;
  }

  return <>
    <div className={styles.localBlueMapNote} role="status">
      <strong>BlueMap local chưa chạy</strong>
      <span>Đang hiển thị atlas dự phòng. Để mở bản đồ block-level, chạy với Java 21: <code>python scripts/bluemap_local.py start --world &quot;đường-dẫn-tới-world&quot;</code>.</span>
    </div>
    <WorldMapExplorer manifest={manifest}/>
  </>;
}
