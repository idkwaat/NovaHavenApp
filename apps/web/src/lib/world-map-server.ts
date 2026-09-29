import {open, readFile} from 'node:fs/promises';
import {join} from 'node:path';
import type {WorldMapBounds, WorldMapManifest, WorldMapMarker} from './world-map';

const IMAGE_URL = '/maps/nova-haven/terrain.png';
const HEIGHTMAP_URL = '/maps/nova-haven/heightmap.bin';
const MAX_PIXELS = 40_000_000;
const MAX_MARKERS = 2_500;

function isInteger(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value);
}

function parseBounds(value: unknown, width: number, height: number, scale: number): WorldMapBounds | null {
  if (!value || typeof value !== 'object') return null;
  const bounds = value as Record<string, unknown>;
  const {minX, maxX, minZ, maxZ} = bounds;
  if (![minX, maxX, minZ, maxZ].every(isInteger)) return null;
  if ((maxX as number) < (minX as number) || (maxZ as number) < (minZ as number)) return null;
  if ((maxX as number) - (minX as number) + 1 !== width * scale) return null;
  if ((maxZ as number) - (minZ as number) + 1 !== height * scale) return null;
  return {minX: minX as number, maxX: maxX as number, minZ: minZ as number, maxZ: maxZ as number};
}

function parseMarkers(value: unknown, bounds: WorldMapBounds): WorldMapMarker[] {
  if (!Array.isArray(value)) return [];
  return value.slice(0, MAX_MARKERS).flatMap((item): WorldMapMarker[] => {
    if (!item || typeof item !== 'object') return [];
    const marker = item as Record<string, unknown>;
    if (
      typeof marker.id !== 'string' || !isInteger(marker.x) || !isInteger(marker.y) ||
      !isInteger(marker.z) || typeof marker.label !== 'string' ||
      marker.label.trim().length < 2 || marker.label.trim().length > 80 ||
      marker.kind !== 'sign'
    ) return [];
    if (
      marker.x < bounds.minX || marker.x > bounds.maxX ||
      marker.z < bounds.minZ || marker.z > bounds.maxZ
    ) return [];
    return [{
      id: marker.id.slice(0, 128),
      x: marker.x,
      y: marker.y,
      z: marker.z,
      label: marker.label.trim(),
      kind: 'sign',
    }];
  });
}

function parseManifest(value: unknown): WorldMapManifest | null {
  if (!value || typeof value !== 'object') return null;
  const source = value as Record<string, unknown>;
  const scale = source.blockScale;
  const width = source.width;
  const height = source.height;
  if (
    source.version !== 1 || source.imageUrl !== IMAGE_URL ||
    !isInteger(scale) || scale < 1 || scale > 64 ||
    !isInteger(width) || width < 1 || width > 16_384 ||
    !isInteger(height) || height < 1 || height > 16_384 ||
    width * height > MAX_PIXELS
  ) return null;
  const bounds = parseBounds(source.bounds, width, height, scale);
  if (!bounds) return null;
  const hasHeightmap = source.heightmapUrl !== undefined;
  if (hasHeightmap && (
    source.heightmapUrl !== HEIGHTMAP_URL || scale !== 1 || width > 4095 || height > 4095 ||
    !isInteger(source.minY) || !isInteger(source.maxY) || source.minY > source.maxY ||
    source.minY < -2048 || source.maxY > 2048
  )) return null;
  if (!hasHeightmap && (source.minY !== undefined || source.maxY !== undefined)) return null;
  const convention = source.coordinateConvention as Record<string, unknown> | null;
  if (!convention || convention.unit !== 'block' || convention.xDirection !== 'right' || convention.zDirection !== 'down') {
    return null;
  }
  const spawnSource = source.spawn;
  let spawn: WorldMapManifest['spawn'] = null;
  if (spawnSource !== null && spawnSource !== undefined) {
    if (!spawnSource || typeof spawnSource !== 'object') return null;
    const point = spawnSource as Record<string, unknown>;
    if (
      !isInteger(point.x) || !isInteger(point.z) ||
      point.x < bounds.minX || point.x > bounds.maxX ||
      point.z < bounds.minZ || point.z > bounds.maxZ
    ) return null;
    spawn = {x: point.x, z: point.z, label: 'Điểm xuất hiện'};
  }
  const manifest: WorldMapManifest = {
    version: 1,
    imageUrl: IMAGE_URL,
    blockScale: scale,
    width,
    height,
    bounds,
    coordinateConvention: {unit: 'block', xDirection: 'right', zDirection: 'down'},
    spawn,
    markers: parseMarkers(source.markers, bounds),
  };
  if (isInteger(source.dataVersion) && source.dataVersion >= 0) manifest.dataVersion = source.dataVersion;
  if (isInteger(source.chunksRendered) && source.chunksRendered >= 0) manifest.chunksRendered = source.chunksRendered;
  if (hasHeightmap) {
    manifest.heightmapUrl = HEIGHTMAP_URL;
    manifest.minY = source.minY as number;
    manifest.maxY = source.maxY as number;
  }
  return manifest;
}

async function hasMatchingPng(publicDirectory: string, width: number, height: number): Promise<boolean> {
  const handle = await open(join(publicDirectory, 'maps', 'nova-haven', 'terrain.png'), 'r');
  try {
    const header = Buffer.alloc(24);
    const {bytesRead} = await handle.read(header, 0, header.length, 0);
    return bytesRead === header.length &&
      header.subarray(0, 8).equals(Buffer.from([137, 80, 78, 71, 13, 10, 26, 10])) &&
      header.toString('ascii', 12, 16) === 'IHDR' &&
      header.readUInt32BE(16) === width &&
      header.readUInt32BE(20) === height;
  } finally {
    await handle.close();
  }
}

async function hasMatchingHeightmap(publicDirectory: string, width: number, height: number): Promise<boolean> {
  const handle = await open(join(publicDirectory, 'maps', 'nova-haven', 'heightmap.bin'), 'r');
  try {
    const {size} = await handle.stat();
    return size === width * height * 2;
  } finally {
    await handle.close();
  }
}

export async function loadWorldMapAssets(publicDirectory: string): Promise<WorldMapManifest | null> {
  try {
    const raw = await readFile(join(publicDirectory, 'maps', 'nova-haven', 'manifest.json'), 'utf8');
    const manifest = parseManifest(JSON.parse(raw) as unknown);
    if (!manifest || !(await hasMatchingPng(publicDirectory, manifest.width, manifest.height))) return null;
    if (manifest.heightmapUrl && !(await hasMatchingHeightmap(publicDirectory, manifest.width, manifest.height))) return null;
    return manifest;
  } catch {
    return null;
  }
}
