import styles from './world-map.module.css';
import {buildBlueMapViewerUrl} from '@/lib/bluemap-local-server';

export default function BlueMapViewer({origin}: {origin: string}) {
  const viewerUrl = buildBlueMapViewerUrl(origin);
  return <div className={styles.blueMapCard}>
    <iframe
      className={styles.blueMapFrame}
      src={viewerUrl}
      title="Bản đồ Minecraft Nova Haven — BlueMap local"
      loading="eager"
      referrerPolicy="no-referrer"
      sandbox="allow-scripts allow-same-origin allow-forms allow-popups"
      allow="fullscreen"
      allowFullScreen
    />
    <a className={styles.blueMapOpen} href={viewerUrl} target="_blank" rel="noreferrer">
      Mở bản đồ trong tab riêng <span aria-hidden="true">↗</span>
    </a>
  </div>;
}
