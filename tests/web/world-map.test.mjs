import assert from 'node:assert/strict';
import {mkdtemp, mkdir, readFile, rm, writeFile} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import test from 'node:test';

let mapModule;
let mapServerModule;
try {
  mapModule = await import('../../apps/web/src/lib/world-map.ts');
  mapServerModule = await import('../../apps/web/src/lib/world-map-server.ts');
} catch {
  mapModule = null;
  mapServerModule = null;
}

const manifest = {
  version: 1,
  imageUrl: '/maps/nova-haven/terrain.png',
  blockScale: 4,
  width: 800,
  height: 576,
  bounds: {minX: -1536, maxX: 1663, minZ: -1152, maxZ: 1151},
  coordinateConvention: {unit: 'block', xDirection: 'right', zDirection: 'down'},
  spawn: {x: 191, z: -71, label: 'Điểm xuất hiện'},
  markers: [],
  dataVersion: 3955,
  chunksRendered: 27915,
};

function pngHeader(width, height) {
  const header = Buffer.alloc(24);
  Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]).copy(header);
  header.write('IHDR', 12, 'ascii');
  header.writeUInt32BE(width, 16);
  header.writeUInt32BE(height, 20);
  return header;
}

test('world coordinate lookup maps negative atlas pixels to floored block X/Z', () => {
  assert.ok(mapModule, 'world-map coordinate helpers have not been implemented');
  assert.deepEqual(
    mapModule.worldCoordinateAt({x: 0.5, y: 0.5}, manifest.bounds, manifest.blockScale, manifest.width, manifest.height),
    {x: -1534, z: -1150},
  );
});

test('world coordinate lookup clamps clicks on the far image edge to the final block', () => {
  assert.ok(mapModule, 'world-map coordinate helpers have not been implemented');
  assert.deepEqual(
    mapModule.worldCoordinateAt({x: manifest.width, y: manifest.height}, manifest.bounds, manifest.blockScale, manifest.width, manifest.height),
    {x: 1663, z: 1151},
  );
});

test('zooming around the pointer keeps its world point fixed', () => {
  assert.ok(mapModule, 'world-map zoom helpers have not been implemented');
  const next = mapModule.zoomAroundPoint(
    {zoom: 1, x: 0, y: 0},
    2,
    {x: 100, y: 120},
    {x: 0, y: 0},
  );
  assert.deepEqual(next, {zoom: 2, x: -100, y: -120});
});

test('zoom level remains inside supported interaction limits', () => {
  assert.ok(mapModule, 'world-map zoom helpers have not been implemented');
  assert.equal(mapModule.clampWorldMapZoom(0.1), 1);
  assert.equal(mapModule.clampWorldMapZoom(9), 9);
  assert.equal(mapModule.clampWorldMapZoom(80), 32);
});

test('local height asset is accepted only with matching block dimensions and signed Y range', async () => {
  assert.ok(mapServerModule, 'local world-map asset loader has not been implemented');
  const root = await mkdtemp(join(tmpdir(), 'nova-world-map-height-'));
  const mapDir = join(root, 'maps', 'nova-haven');
  const heightManifest = {
    ...manifest,
    blockScale: 1,
    width: 32,
    height: 32,
    bounds: {minX: -16, maxX: 15, minZ: -16, maxZ: 15},
    spawn: null,
    heightmapUrl: '/maps/nova-haven/heightmap.bin',
    minY: -64,
    maxY: 120,
  };
  try {
    await mkdir(mapDir, {recursive: true});
    await writeFile(join(mapDir, 'terrain.png'), pngHeader(32, 32));
    await writeFile(join(mapDir, 'manifest.json'), JSON.stringify(heightManifest));
    assert.equal(await mapServerModule.loadWorldMapAssets(root), null);
    await writeFile(join(mapDir, 'heightmap.bin'), Buffer.alloc(32 * 32 * 2));
    assert.equal((await mapServerModule.loadWorldMapAssets(root))?.heightmapUrl, heightManifest.heightmapUrl);
    await writeFile(join(mapDir, 'heightmap.bin'), Buffer.alloc(32));
    assert.equal(await mapServerModule.loadWorldMapAssets(root), null);
    await writeFile(join(mapDir, 'heightmap.bin'), Buffer.alloc(32 * 32 * 2));
    await writeFile(join(mapDir, 'manifest.json'), JSON.stringify({...heightManifest, heightmapUrl: 'https://example.invalid/heights.bin'}));
    assert.equal(await mapServerModule.loadWorldMapAssets(root), null);
  } finally {
    await rm(root, {recursive: true, force: true});
  }
});

test('asset loader returns the actual local map manifest only when image exists', async () => {
  assert.ok(mapServerModule, 'local world-map asset loader has not been implemented');
  const root = await mkdtemp(join(tmpdir(), 'nova-world-map-'));
  const mapDir = join(root, 'maps', 'nova-haven');
  try {
    await mkdir(mapDir, {recursive: true});
    await writeFile(join(mapDir, 'manifest.json'), JSON.stringify(manifest));
    assert.equal(await mapServerModule.loadWorldMapAssets(root), null);
    await writeFile(join(mapDir, 'terrain.png'), pngHeader(manifest.width, manifest.height));
    const loaded = await mapServerModule.loadWorldMapAssets(root);
    assert.equal(loaded?.bounds.minX, -1536);
    assert.equal(loaded?.spawn.x, 191);
    assert.equal(loaded?.imageUrl, '/maps/nova-haven/terrain.png');
  } finally {
    await rm(root, {recursive: true, force: true});
  }
});

test('asset loader rejects malformed bounds and external image URLs', async () => {
  assert.ok(mapServerModule, 'local world-map asset loader has not been implemented');
  const root = await mkdtemp(join(tmpdir(), 'nova-world-map-'));
  const mapDir = join(root, 'maps', 'nova-haven');
  try {
    await mkdir(mapDir, {recursive: true});
    await writeFile(join(mapDir, 'terrain.png'), pngHeader(manifest.width, manifest.height));
    await writeFile(join(mapDir, 'manifest.json'), JSON.stringify({...manifest, imageUrl: 'https://example.invalid/map.png'}));
    assert.equal(await mapServerModule.loadWorldMapAssets(root), null);
    await writeFile(join(mapDir, 'manifest.json'), JSON.stringify({...manifest, bounds: {...manifest.bounds, minX: 'west'}}));
    assert.equal(await mapServerModule.loadWorldMapAssets(root), null);
  } finally {
    await rm(root, {recursive: true, force: true});
  }
});

test('mobile map overview control preserves a 44px touch height', async () => {
  const css = await readFile(new URL('../../apps/web/app/map/world-map.module.css', import.meta.url), 'utf8');
  const mobile = css.slice(css.indexOf('@media (max-width: 600px)'));

  assert.match(mobile, /\.zoomControls button:last-child\s*\{[^}]*min-height:\s*44px/s);
});

test('mobile map zoom controls keep the recommended 8px separation', async () => {
  const css = await readFile(new URL('../../apps/web/app/map/world-map.module.css', import.meta.url), 'utf8');
  const mobile = css.slice(css.indexOf('@media (max-width: 600px)'));

  assert.match(mobile, /\.zoomControls\s*\{[^}]*gap:\s*8px/s);
});

test('mobile map viewport stays close to the atlas aspect ratio', async () => {
  const css = await readFile(new URL('../../apps/web/app/map/world-map.module.css', import.meta.url), 'utf8');
  const mobile = css.slice(css.indexOf('@media (max-width: 600px)'));

  assert.match(mobile, /\.viewport\s*\{[^}]*height:\s*min\(70vw,\s*420px\);[^}]*min-height:\s*200px/s);
});
