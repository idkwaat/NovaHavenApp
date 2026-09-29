import type {WorldMapBounds} from './world-map';

export const MIN_TERRAIN_ZOOM = 1;
export const MAX_TERRAIN_ZOOM = 32;
export const TERRAIN_MESH_STEP = 4;
export const TERRAIN_HEIGHT_EXAGGERATION = 1.5;
export const TERRAIN_YAW = Math.PI / 4;
export const TERRAIN_ELEVATION = Math.PI / 3;

export type TerrainGrid = {
  positions: Float32Array;
  indices: Uint32Array;
  columns: number;
  rows: number;
};

export type TerrainPoint = {x: number; z: number; y: number};
export type ProjectedTerrainPoint = {x: number; y: number};
export type TerrainMarker = {x: number; z: number; kind: 'spawn' | 'sign'};

export function buildTerrainMarkerData(markers: TerrainMarker[], bounds: WorldMapBounds): Float32Array {
  const visible = markers.filter(marker => Number.isInteger(marker.x) && Number.isInteger(marker.z) &&
    marker.x >= bounds.minX && marker.x <= bounds.maxX && marker.z >= bounds.minZ && marker.z <= bounds.maxZ);
  const data = new Float32Array(visible.length * 3);
  visible.forEach((marker, index) => {
    data[index * 3] = marker.x - bounds.minX;
    data[index * 3 + 1] = marker.z - bounds.minZ;
    data[index * 3 + 2] = marker.kind === 'spawn' ? 0 : 1;
  });
  return data;
}

export function clampTerrainZoom(zoom: number): number {
  if (!Number.isFinite(zoom)) return MIN_TERRAIN_ZOOM;
  return Math.max(MIN_TERRAIN_ZOOM, Math.min(MAX_TERRAIN_ZOOM, zoom));
}

function sampleAxis(size: number, step: number): number[] {
  const result: number[] = [];
  for (let value = 0; value < size; value += step) result.push(value);
  if (result.at(-1) !== size - 1) result.push(size - 1);
  return result;
}

export function buildTerrainGrid(width: number, height: number, validMask: Uint8Array, step = TERRAIN_MESH_STEP): TerrainGrid {
  if (!Number.isInteger(width) || width < 2 || !Number.isInteger(height) || height < 2 ||
      !Number.isInteger(step) || step < 1 || validMask.length !== width * height) {
    throw new RangeError('terrain grid dimensions, step and mask must agree');
  }
  const xs = sampleAxis(width, step);
  const zs = sampleAxis(height, step);
  const vertexCount = xs.length * zs.length;
  if (vertexCount > 1_000_000) throw new RangeError('terrain mesh exceeds the vertex budget');
  const positions = new Float32Array(vertexCount * 2);
  for (let row = 0; row < zs.length; row++) {
    for (let column = 0; column < xs.length; column++) {
      const index = (row * xs.length + column) * 2;
      positions[index] = xs[column];
      positions[index + 1] = zs[row];
    }
  }
  const triangles: number[] = [];
  for (let row = 0; row < zs.length - 1; row++) {
    for (let column = 0; column < xs.length - 1; column++) {
      const x0 = xs[column], x1 = xs[column + 1];
      const z0 = zs[row], z1 = zs[row + 1];
      const corners = [z0 * width + x0, z0 * width + x1, z1 * width + x0, z1 * width + x1];
      if (corners.some(index => validMask[index] === 0)) continue;
      const topLeft = row * xs.length + column;
      const topRight = topLeft + 1;
      const bottomLeft = topLeft + xs.length;
      const bottomRight = bottomLeft + 1;
      triangles.push(topLeft, bottomLeft, topRight, topRight, bottomLeft, bottomRight);
    }
  }
  return {positions, indices: Uint32Array.from(triangles), columns: xs.length, rows: zs.length};
}

export function projectTerrainPoint(point: TerrainPoint, bounds: WorldMapBounds, minY: number): ProjectedTerrainPoint {
  const x = point.x - (bounds.minX + bounds.maxX) / 2;
  const z = point.z - (bounds.minZ + bounds.maxZ) / 2;
  const y = (point.y - minY) * TERRAIN_HEIGHT_EXAGGERATION;
  const cosYaw = Math.cos(TERRAIN_YAW), sinYaw = Math.sin(TERRAIN_YAW);
  const cosElevation = Math.cos(TERRAIN_ELEVATION), sinElevation = Math.sin(TERRAIN_ELEVATION);
  return {
    x: cosYaw * x - sinYaw * z,
    y: -sinElevation * (sinYaw * x + cosYaw * z) + cosElevation * y,
  };
}

export function unprojectTerrainPoint(point: ProjectedTerrainPoint, bounds: WorldMapBounds, minY: number, surfaceY: number): {x: number; z: number} {
  const cosYaw = Math.cos(TERRAIN_YAW), sinYaw = Math.sin(TERRAIN_YAW);
  const cosElevation = Math.cos(TERRAIN_ELEVATION), sinElevation = Math.sin(TERRAIN_ELEVATION);
  const height = (surfaceY - minY) * TERRAIN_HEIGHT_EXAGGERATION;
  const q = (cosElevation * height - point.y) / sinElevation;
  return {
    x: Math.round((bounds.minX + bounds.maxX) / 2 + cosYaw * point.x + sinYaw * q),
    z: Math.round((bounds.minZ + bounds.maxZ) / 2 - sinYaw * point.x + cosYaw * q),
  };
}

export function terrainViewHeight(width: number, height: number, aspect: number, zoom: number, minY: number, maxY: number): number {
  if (![width, height, aspect, minY, maxY].every(Number.isFinite) || width < 1 || height < 1 || aspect <= 0) {
    throw new RangeError('terrain view dimensions must be finite and positive');
  }
  const safeZoom = clampTerrainZoom(zoom);
  const cosYaw = Math.abs(Math.cos(TERRAIN_YAW)), sinYaw = Math.abs(Math.sin(TERRAIN_YAW));
  const cosElevation = Math.cos(TERRAIN_ELEVATION), sinElevation = Math.sin(TERRAIN_ELEVATION);
  const projectedWidth = cosYaw * width + sinYaw * height;
  const projectedHeight = (sinYaw * width + cosYaw * height) * sinElevation +
    Math.max(0, maxY - minY) * TERRAIN_HEIGHT_EXAGGERATION * cosElevation;
  return Math.max(projectedHeight, projectedWidth / aspect) * 1.12 / safeZoom;
}

export function decodeTerrainPick(bytes: Uint8Array, bounds: WorldMapBounds, width: number, height: number): {x: number; z: number} | null {
  if (bytes.length < 4 || bytes[3] === 0) return null;
  const pixelX = bytes[0] | ((bytes[1] & 0x0f) << 8);
  const pixelZ = bytes[2] | ((bytes[1] >> 4) << 8);
  if (pixelX >= width || pixelZ >= height) return null;
  const x = bounds.minX + pixelX;
  const z = bounds.minZ + pixelZ;
  if (x > bounds.maxX || z > bounds.maxZ) return null;
  return {x, z};
}
