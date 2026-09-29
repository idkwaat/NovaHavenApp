'use client';

import Image from 'next/image';
import Link from 'next/link';
import {useEffect, useMemo, useRef, useState} from 'react';
import type {CSSProperties, KeyboardEvent, PointerEvent, WheelEvent} from 'react';
import {clampWorldMapZoom, worldCoordinateAt, zoomAroundPoint} from '@/lib/world-map';
import {clampTerrainZoom} from '@/lib/world-map-3d';
import type {TerrainMarker} from '@/lib/world-map-3d';
import type {WorldMapManifest, WorldMapMarker, WorldMapView} from '@/lib/world-map';
import IsometricTerrainCanvas from './IsometricTerrainCanvas';
import styles from './world-map.module.css';

type SelectedPoint = {x: number; z: number; label?: string};
type DragState = {
  pointerId: number;
  startX: number;
  startY: number;
  originX: number;
  originY: number;
  moved: boolean;
};

function clampView(view: WorldMapView, mapSize: {width: number; height: number}, viewport: HTMLDivElement | null): WorldMapView {
  if (!viewport || mapSize.width <= 0 || mapSize.height <= 0) return view;
  const maxX = Math.max(0, (mapSize.width * view.zoom - viewport.clientWidth) / 2);
  const maxY = Math.max(0, (mapSize.height * view.zoom - viewport.clientHeight) / 2);
  return {...view, x: Math.max(-maxX, Math.min(maxX, view.x)), y: Math.max(-maxY, Math.min(maxY, view.y))};
}

function markerPosition(manifest: WorldMapManifest, x: number, z: number): CSSProperties {
  return {
    left: (((x - manifest.bounds.minX) / (manifest.width * manifest.blockScale)) * 100) + '%',
    top: (((z - manifest.bounds.minZ) / (manifest.height * manifest.blockScale)) * 100) + '%',
  };
}

export default function WorldMapExplorer({manifest}: {manifest: WorldMapManifest}) {
  const viewportRef = useRef<HTMLDivElement>(null);
  const planeRef = useRef<HTMLDivElement>(null);
  const dragRef = useRef<DragState | null>(null);
  const [mapSize, setMapSize] = useState({width: 0, height: 0});
  const [view, setView] = useState<WorldMapView>({zoom: 1, x: 0, y: 0});
  const [selected, setSelected] = useState<SelectedPoint | null>(
    manifest.spawn ? {x: manifest.spawn.x, z: manifest.spawn.z, label: manifest.spawn.label} : null,
  );
  const [showSpawn, setShowSpawn] = useState(true);
  const [showSigns, setShowSigns] = useState(true);
  const [query, setQuery] = useState('');
  const [copied, setCopied] = useState(false);
  const [canCopy, setCanCopy] = useState(false);
  const [isDragging, setIsDragging] = useState(false);
  const [imageReady, setImageReady] = useState(false);
  const [imageError, setImageError] = useState(false);
  const [mode, setMode] = useState<'3d' | 'flat'>(manifest.heightmapUrl ? '3d' : 'flat');
  const [terrainZoom, setTerrainZoom] = useState(1);
  const [resetToken, setResetToken] = useState(0);
  const [rendererStatus, setRendererStatus] = useState<{ready: boolean; error?: string}>({ready: false});
  const [cursor, setCursor] = useState<SelectedPoint | null>(null);
  const [center, setCenter] = useState<SelectedPoint | null>(null);

  useEffect(() => {
    setCanCopy(typeof navigator !== 'undefined' && Boolean(navigator.clipboard?.writeText));
    const viewport = viewportRef.current;
    if (!viewport) return;
    const measure = () => {
      const fit = Math.min(viewport.clientWidth / manifest.width, viewport.clientHeight / manifest.height, 1);
      setMapSize({width: manifest.width * fit, height: manifest.height * fit});
    };
    const observer = new ResizeObserver(measure);
    observer.observe(viewport);
    measure();
    return () => observer.disconnect();
  }, [manifest.height, manifest.width]);

  const visibleMarkers = useMemo(() => {
    const search = query.trim().toLocaleLowerCase('vi-VN');
    return manifest.markers.filter(marker => showSigns && (!search || marker.label.toLocaleLowerCase('vi-VN').includes(search)));
  }, [manifest.markers, query, showSigns]);

  const terrainMarkers = useMemo<TerrainMarker[]>(() => {
    const markers: TerrainMarker[] = [];
    if (showSpawn && manifest.spawn) markers.push({x: manifest.spawn.x, z: manifest.spawn.z, kind: 'spawn'});
    markers.push(...visibleMarkers.map(marker => ({x: marker.x, z: marker.z, kind: 'sign' as const})));
    return markers;
  }, [manifest.spawn, visibleMarkers, showSpawn]);

  function changeZoom(multiplier: number, pointer?: {x: number; y: number}) {
    if (mode === '3d') {
      setTerrainZoom(current => clampTerrainZoom(current * multiplier));
      return;
    }
    const viewport = viewportRef.current;
    if (!viewport || imageError) return;
    const rect = viewport.getBoundingClientRect();
    const point = pointer ?? {x: rect.left + rect.width / 2, y: rect.top + rect.height / 2};
    const center = {x: rect.left + rect.width / 2, y: rect.top + rect.height / 2};
    setView(current => {
      const next = zoomAroundPoint(
        current,
        clampWorldMapZoom(current.zoom * multiplier),
        {x: point.x - rect.left, y: point.y - rect.top},
        {x: center.x - rect.left, y: center.y - rect.top},
      );
      return clampView(next, mapSize, viewport);
    });
  }

  function resetView() {
    setView({zoom: 1, x: 0, y: 0});
    setTerrainZoom(1);
    setResetToken(value => value + 1);
    setCopied(false);
  }

  function selectAt(clientX: number, clientY: number) {
    const plane = planeRef.current;
    if (!plane) return;
    const rect = plane.getBoundingClientRect();
    if (rect.width <= 0 || rect.height <= 0) return;
    const point = worldCoordinateAt(
      {
        x: ((clientX - rect.left) / rect.width) * manifest.width,
        y: ((clientY - rect.top) / rect.height) * manifest.height,
      },
      manifest.bounds,
      manifest.blockScale,
      manifest.width,
      manifest.height,
    );
    setSelected(point);
    setCopied(false);
  }

  function onPointerDown(event: PointerEvent<HTMLDivElement>) {
    if (imageError || event.button !== 0 || (event.target instanceof Element && event.target.closest('button'))) return;
    dragRef.current = {
      pointerId: event.pointerId,
      startX: event.clientX,
      startY: event.clientY,
      originX: view.x,
      originY: view.y,
      moved: false,
    };
    setIsDragging(false);
    event.currentTarget.setPointerCapture(event.pointerId);
  }

  function onPointerMove(event: PointerEvent<HTMLDivElement>) {
    const drag = dragRef.current;
    if (!drag || drag.pointerId !== event.pointerId) return;
    const dx = event.clientX - drag.startX;
    const dy = event.clientY - drag.startY;
    if (Math.abs(dx) + Math.abs(dy) > 4) {
      drag.moved = true;
      setIsDragging(true);
    }
    if (!drag.moved) return;
    setView(current => clampView({...current, x: drag.originX + dx, y: drag.originY + dy}, mapSize, viewportRef.current));
  }

  function onPointerUp(event: PointerEvent<HTMLDivElement>) {
    const drag = dragRef.current;
    if (!drag || drag.pointerId !== event.pointerId) return;
    if (!drag.moved) selectAt(event.clientX, event.clientY);
    dragRef.current = null;
    setIsDragging(false);
  }

  function onPointerCancel() {
    dragRef.current = null;
    setIsDragging(false);
  }

  function onWheel(event: WheelEvent<HTMLDivElement>) {
    event.preventDefault();
    changeZoom(event.deltaY < 0 ? 1.2 : 1 / 1.2, {x: event.clientX, y: event.clientY});
  }

  function onKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.target !== event.currentTarget) return;
    const viewport = viewportRef.current;
    if (!viewport) return;
    const step = 56;
    if (event.key === 'ArrowLeft') setView(current => clampView({...current, x: current.x + step}, mapSize, viewport));
    else if (event.key === 'ArrowRight') setView(current => clampView({...current, x: current.x - step}, mapSize, viewport));
    else if (event.key === 'ArrowUp') setView(current => clampView({...current, y: current.y + step}, mapSize, viewport));
    else if (event.key === 'ArrowDown') setView(current => clampView({...current, y: current.y - step}, mapSize, viewport));
    else if (event.key === '+' || event.key === '=' || event.key === 'Add') changeZoom(1.2);
    else if (event.key === '-' || event.key === 'Subtract') changeZoom(1 / 1.2);
    else if (event.key === 'Home') resetView();
    else if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      const rect = viewport.getBoundingClientRect();
      selectAt(rect.left + rect.width / 2, rect.top + rect.height / 2);
      return;
    } else return;
    event.preventDefault();
  }

  async function copyCoordinates() {
    if (!selected || !canCopy) return;
    try {
      await navigator.clipboard.writeText('X ' + selected.x + ', Z ' + selected.z);
      setCopied(true);
    } catch {
      setCopied(false);
    }
  }

  function chooseMarker(marker: WorldMapMarker) {
    setSelected({x: marker.x, z: marker.z, label: marker.label});
    setCopied(false);
  }

  const planeStyle: CSSProperties = {
    width: mapSize.width + 'px',
    height: mapSize.height + 'px',
    transform: 'translate(calc(-50% + ' + view.x + 'px), calc(-50% + ' + view.y + 'px)) scale(' + view.zoom + ')',
  };
  const activeZoom = mode === '3d' ? terrainZoom : view.zoom;
  const rendererFailed = Boolean(rendererStatus.error);

  return <section className={styles.page}>
    <div className={styles.breadcrumbs}><Link href="/">Trang chủ</Link><span aria-hidden="true">/</span><span>Bản đồ</span></div>
    <header className={styles.heading}>
      <div>
        <p className={styles.eyebrow}>NOVA HAVEN · BẢN ĐỒ ĐỊA HÌNH</p>
        <h1>Bản đồ thế giới</h1>
        <p className={styles.intro}>Khám phá địa hình từ world Minecraft đã cung cấp. Chọn một điểm để đọc tọa độ block X/Z.</p>
      </div>
      <div className={styles.mapFacts}>
        <strong>{manifest.chunksRendered?.toLocaleString('vi-VN') ?? '—'}</strong>
        <span>chunk mặt địa hình</span>
        <small>{manifest.blockScale} block / ô · Bản đồ thế giới</small>
      </div>
    </header>

    <div className={styles.mapGrid}>
      <section className={styles.mapCard} aria-label="Bản đồ địa hình tương tác">
        <div className={styles.mapToolbar}>
          <div className={styles.mapToolbarTitle}>
            <strong>Địa hình Nova Haven</strong>
            <span>Tọa độ theo block Minecraft</span>
          </div>
          <div className={styles.mapToolbarActions}>
            <div className={styles.mapModes} role="group" aria-label="Chế độ bản đồ">
              <button type="button" aria-pressed={mode === '3d'} disabled={!manifest.heightmapUrl || rendererFailed}
                onClick={() => setMode('3d')}>Địa hình 3D</button>
              <button type="button" aria-pressed={mode === 'flat'} onClick={() => setMode('flat')}>Bản đồ phẳng</button>
            </div>
            <div className={styles.zoomControls} aria-label="Điều khiển thu phóng">
              <button type="button" onClick={() => changeZoom(1 / 1.25)} disabled={activeZoom <= 1 || (mode === 'flat' && imageError) || (mode === '3d' && !rendererStatus.ready)} aria-label="Thu nhỏ bản đồ" title="Thu nhỏ">−</button>
              <output aria-live="polite" aria-label="Mức thu phóng">{Math.round(activeZoom * 100)}%</output>
              <button type="button" onClick={() => changeZoom(1.25)} disabled={activeZoom >= 32 || (mode === 'flat' && imageError) || (mode === '3d' && !rendererStatus.ready)} aria-label="Phóng to bản đồ" title="Phóng to">+</button>
              <button type="button" onClick={resetView} disabled={mode === 'flat' && imageError} aria-label="Đưa bản đồ về toàn cảnh" title="Toàn cảnh">Toàn cảnh</button>
            </div>
          </div>
        </div>

        {rendererFailed && <p className={styles.fallbackMessage} role="status">{rendererStatus.error} Đang hiển thị bản đồ phẳng thay thế.</p>}
        <div
          className={styles.viewport + (isDragging ? ' ' + styles.dragging : '')}
          ref={viewportRef}
          role="region"
          aria-label={mode === '3d' ? 'Địa hình isometric, camera cố định; dùng phím mũi tên để di chuyển và cộng trừ để zoom.' : 'Bản đồ phẳng tương tác; dùng phím mũi tên để di chuyển và cộng trừ để zoom.'}
          tabIndex={mode === 'flat' ? 0 : -1}
          onPointerDown={mode === 'flat' ? onPointerDown : undefined}
          onPointerMove={mode === 'flat' ? onPointerMove : undefined}
          onPointerUp={mode === 'flat' ? onPointerUp : undefined}
          onPointerCancel={mode === 'flat' ? onPointerCancel : undefined}
          onWheel={mode === 'flat' ? onWheel : undefined}
          onKeyDown={mode === 'flat' ? onKeyDown : undefined}
        >
          {mode === '3d' && !rendererFailed && <>
            {!rendererStatus.ready && <p className={styles.imageStatus} role="status">Đang dựng mô hình địa hình 3D…</p>}
            <IsometricTerrainCanvas
              manifest={manifest}
              markers={terrainMarkers}
              zoom={terrainZoom}
              resetToken={resetToken}
              onZoomChange={setTerrainZoom}
              onSelect={point => { setSelected(point); setCopied(false); }}
              onCursor={setCursor}
              onCenter={setCenter}
              onStatus={status => { setRendererStatus(status); if (status.error) setMode('flat'); }}
            />
            {rendererStatus.ready && <>
              <span className={styles.mapNorth}>BẮC · GÓC NHÌN CỐ ĐỊNH</span>
              <div className={styles.mapHud} aria-label="Tọa độ con trỏ và tâm bản đồ">
                <span><small>Con trỏ</small><strong>{cursor ? `X ${cursor.x} · Z ${cursor.z}` : 'Di chuột lên địa hình'}</strong></span>
                <span><small>Tâm</small><strong>{center ? `X ${center.x} · Z ${center.z}` : '—'}</strong></span>
              </div>
            </>}
          </>}
          {(mode === 'flat' || rendererFailed) && <>
            {!imageReady && !imageError && <p className={styles.imageStatus} role="status">Đang tải bản đồ địa hình…</p>}
            {imageError && <div className={styles.imageError} role="alert"><strong>Không tải được ảnh địa hình.</strong><span>Hãy render lại bản đồ từ save cục bộ.</span></div>}
            <div className={styles.mapPlane} ref={planeRef} style={planeStyle} aria-hidden={imageError}>
              <Image
                className={styles.terrain}
                src={manifest.imageUrl}
                alt="Địa hình được render từ world Minecraft do chủ dự án cung cấp"
                width={manifest.width}
                height={manifest.height}
                unoptimized
                draggable={false}
                onLoad={() => setImageReady(true)}
                onError={() => setImageError(true)}
                priority
              />
              {showSpawn && manifest.spawn && <button
                type="button"
                className={styles.marker + ' ' + styles.spawnMarker}
                style={markerPosition(manifest, manifest.spawn.x, manifest.spawn.z)}
                onPointerDown={event => event.stopPropagation()}
                onClick={() => chooseMarker({id: 'spawn', x: manifest.spawn!.x, y: 0, z: manifest.spawn!.z, label: manifest.spawn!.label, kind: 'sign'})}
                aria-label={'Điểm xuất hiện, X ' + manifest.spawn.x + ', Z ' + manifest.spawn.z}
                title={manifest.spawn.label}
              ><span aria-hidden="true"/></button>}
              {visibleMarkers.map(marker => <button
                key={marker.id}
                type="button"
                className={styles.marker + ' ' + styles.signMarker}
                style={markerPosition(manifest, marker.x, marker.z)}
                onPointerDown={event => event.stopPropagation()}
                onClick={() => chooseMarker(marker)}
                aria-label={marker.label + ', X ' + marker.x + ', Z ' + marker.z}
                title={marker.label}
              ><span aria-hidden="true"/></button>)}
            </div>
            {!imageError && <span className={styles.mapNorth} aria-label="Bắc: hướng Z âm">BẮC ↑</span>}
            {!imageError && <span className={styles.mapScale}>1 ô ≈ {manifest.blockScale} block</span>}
          </>}
        </div>
        <p className={styles.mapHelp}>{mode === '3d' && !rendererFailed ? 'Kéo để di chuyển · cuộn hoặc nhấn + / − để zoom tới từng block · camera luôn giữ nguyên hướng' : 'Kéo để di chuyển · lăn chuột hoặc dùng + / − để zoom · chọn một điểm để xem tọa độ'}</p>
      </section>

      <aside className={styles.sidebar} aria-label="Thông tin và lớp bản đồ">
        <section className={styles.panel}>
          <p className={styles.panelEyebrow}>TỌA ĐỘ ĐANG CHỌN</p>
          <h2>{selected?.label ?? 'Chọn một điểm trên bản đồ'}</h2>
          {selected ? <>
            <dl className={styles.coordinates}>
              <div><dt>X</dt><dd>{selected.x}</dd></div>
              <div><dt>Z</dt><dd>{selected.z}</dd></div>
            </dl>
            <button className={styles.copyButton} type="button" onClick={copyCoordinates} disabled={!canCopy}>
              {copied ? 'Đã sao chép' : 'Sao chép X, Z'}
            </button>
          </> : <p>Nhấn vào một vị trí bất kỳ để đọc tọa độ Minecraft theo block.</p>}
        </section>

        <section className={styles.panel}>
          <p className={styles.panelEyebrow}>LỚP BẢN ĐỒ</p>
          <button className={styles.layerToggle} type="button" aria-pressed={showSpawn} onClick={() => setShowSpawn(value => !value)} disabled={!manifest.spawn}>
            <span className={styles.layerSwatch + ' ' + styles.spawnSwatch} aria-hidden="true"/>
            <span>Điểm xuất hiện</span><small>{manifest.spawn ? 'Có trong save' : 'Không có dữ liệu'}</small>
          </button>
          <button className={styles.layerToggle} type="button" aria-pressed={showSigns} onClick={() => setShowSigns(value => !value)} disabled={manifest.markers.length === 0}>
            <span className={styles.layerSwatch + ' ' + styles.signSwatch} aria-hidden="true"/>
            <span>Biển tên</span><small>{manifest.markers.length}</small>
          </button>
          {manifest.markers.length > 0 ? <>
            <label className={styles.searchLabel} htmlFor="map-place-search">Tìm nhãn trong world</label>
            <input id="map-place-search" className={styles.searchInput} value={query} onChange={event => setQuery(event.target.value)} placeholder="Nhập tên trên biển…" type="search"/>
            <ul className={styles.placeList}>
              {visibleMarkers.slice(0, 100).map(marker => <li key={marker.id}><button type="button" onClick={() => chooseMarker(marker)}>{marker.label}<small>X {marker.x} · Z {marker.z}</small></button></li>)}
            </ul>
            {visibleMarkers.length === 0 && <p className={styles.emptyText}>Không có biển tên khớp từ khóa.</p>}
            {visibleMarkers.length > 100 && <p className={styles.emptyText}>Đang hiện 100 nhãn đầu. Hãy tìm theo tên để thu hẹp.</p>}
          </> : <p className={styles.emptyText}>Save này không có biển tên để làm nhãn. Chọn bản đồ để tra cứu X/Z.</p>}
        </section>

        <p className={styles.localNote}>Bản đồ địa hình thế giới chính thức · Khám phá tọa độ và các mốc địa danh.</p>
        <Link className={styles.knowledgeLink} href="/knowledge">Thư viện địa danh <span aria-hidden="true">→</span></Link>
      </aside>
    </div>
    <p className={styles.coordinateNote}>Hệ tọa độ: X tăng sang phải, Z tăng xuống dưới. Mỗi điểm ảnh địa hình đại diện {manifest.blockScale} block; tọa độ hiển thị theo block chuẩn thế giới.</p>
  </section>;
}
