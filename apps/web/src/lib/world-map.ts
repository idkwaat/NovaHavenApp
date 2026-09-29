export type WorldMapBounds = {
  minX: number;
  maxX: number;
  minZ: number;
  maxZ: number;
};

export type WorldMapMarker = {
  id: string;
  x: number;
  y: number;
  z: number;
  label: string;
  kind: 'sign';
};

export type WorldMapManifest = {
  version: 1;
  imageUrl: '/maps/nova-haven/terrain.png';
  blockScale: number;
  width: number;
  height: number;
  bounds: WorldMapBounds;
  coordinateConvention: {unit: 'block'; xDirection: 'right'; zDirection: 'down'};
  spawn: {x: number; z: number; label: string} | null;
  markers: WorldMapMarker[];
  dataVersion?: number;
  chunksRendered?: number;
  heightmapUrl?: '/maps/nova-haven/heightmap.bin';
  minY?: number;
  maxY?: number;
};

export type WorldMapView = {zoom: number; x: number; y: number};
export type WorldMapPoint = {x: number; y: number};

export const MIN_WORLD_MAP_ZOOM = 1;
export const MAX_WORLD_MAP_ZOOM = 32;

export function clampWorldMapZoom(zoom: number): number {
  if (!Number.isFinite(zoom)) return MIN_WORLD_MAP_ZOOM;
  return Math.max(MIN_WORLD_MAP_ZOOM, Math.min(MAX_WORLD_MAP_ZOOM, zoom));
}

export function zoomAroundPoint(
  view: WorldMapView,
  requestedZoom: number,
  point: WorldMapPoint,
  center: WorldMapPoint,
): WorldMapView {
  const currentZoom = clampWorldMapZoom(view.zoom);
  const zoom = clampWorldMapZoom(requestedZoom);
  const ratio = zoom / currentZoom;
  return {
    zoom,
    x: point.x - center.x - (point.x - center.x - view.x) * ratio,
    y: point.y - center.y - (point.y - center.y - view.y) * ratio,
  };
}

export function worldCoordinateAt(
  pixel: WorldMapPoint,
  bounds: WorldMapBounds,
  blockScale: number,
  width: number,
  height: number,
): {x: number; z: number} {
  if (
    !Number.isInteger(blockScale) || blockScale < 1 ||
    !Number.isInteger(width) || width < 1 ||
    !Number.isInteger(height) || height < 1
  ) {
    throw new RangeError('World map dimensions and block scale must be positive integers');
  }
  const pixelX = Number.isFinite(pixel.x) ? Math.max(0, Math.min(width - Number.EPSILON, pixel.x)) : 0;
  const pixelY = Number.isFinite(pixel.y) ? Math.max(0, Math.min(height - Number.EPSILON, pixel.y)) : 0;
  return {
    x: Math.min(bounds.maxX, bounds.minX + Math.floor(pixelX * blockScale)),
    z: Math.min(bounds.maxZ, bounds.minZ + Math.floor(pixelY * blockScale)),
  };
}
