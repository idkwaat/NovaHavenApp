import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import test from 'node:test';

let terrain3d;
try {
  terrain3d = await import('../../apps/web/src/lib/world-map-3d.ts');
} catch {
  terrain3d = null;
}

const bounds = {minX: -16, maxX: 15, minZ: -16, maxZ: 15};

test('3D zoom remains finite and bounded from overview through block detail', () => {
  assert.ok(terrain3d, 'fixed-angle world-map helpers have not been implemented');
  assert.equal(terrain3d.clampTerrainZoom(0.1), 1);
  assert.equal(terrain3d.clampTerrainZoom(32), 32);
  assert.equal(terrain3d.clampTerrainZoom(80), 32);
  assert.equal(terrain3d.clampTerrainZoom(Number.NaN), 1);
});

test('fixed camera stays overhead with restrained height exaggeration', () => {
  assert.ok(terrain3d, 'fixed-angle world-map helpers have not been implemented');
  assert.equal(terrain3d.TERRAIN_YAW, Math.PI / 4);
  assert.equal(terrain3d.TERRAIN_ELEVATION, Math.PI / 3);
  assert.equal(terrain3d.TERRAIN_HEIGHT_EXAGGERATION, 1.5);
});

test('fixed isometric projection round-trips negative X/Z block coordinates', () => {
  assert.ok(terrain3d, 'fixed-angle world-map helpers have not been implemented');
  const point = {x: -5, z: 13, y: 64};
  const projected = terrain3d.projectTerrainPoint(point, bounds, 60);

  assert.deepEqual(terrain3d.unprojectTerrainPoint(projected, bounds, 60, 64), {x: -5, z: 13});
});

test('terrain and height textures keep the same north-to-south row origin', async () => {
  const source = await readFile(new URL('../../apps/web/app/map/IsometricTerrainCanvas.tsx', import.meta.url), 'utf8');
  const imageUpload = source.match(/gl\.pixelStorei\(gl\.UNPACK_FLIP_Y_WEBGL,\s*(\d)\);[\s\S]*?gl\.texImage2D\(gl\.TEXTURE_2D,\s*0,\s*gl\.RGBA,\s*gl\.RGBA,\s*gl\.UNSIGNED_BYTE,\s*image\)/);
  assert.ok(imageUpload, 'terrain image upload must set an explicit row orientation');
  assert.equal(imageUpload[1], '0', 'typed heightmap and image rows both start at min Z');
});

test('terrain mesh emits upward-facing triangles only for complete sampled quads', () => {
  assert.ok(terrain3d, 'fixed-angle world-map helpers have not been implemented');
  const grid = terrain3d.buildTerrainGrid(3, 2, Uint8Array.from([1, 1, 0, 1, 1, 1]), 1);

  assert.deepEqual([...grid.indices], [0, 3, 1, 1, 3, 4]);
  assert.equal(grid.columns, 3);
  assert.equal(grid.rows, 2);
  assert.deepEqual([...grid.positions], [0, 0, 1, 0, 2, 0, 0, 1, 1, 1, 2, 1]);
});

test('GPU terrain pick bytes decode to bounded negative block coordinates', () => {
  assert.ok(terrain3d, 'fixed-angle world-map helpers have not been implemented');
  const largeBounds = {minX: -2048, maxX: 2047, minZ: -2048, maxZ: 2047};
  const picked = terrain3d.decodeTerrainPick(Uint8Array.from([0x45, 0x19, 0x2c, 0xff]), largeBounds, 4096, 4096);

  assert.deepEqual(picked, {x: -2048 + 0x945, z: -2048 + 0x12c});
  assert.equal(terrain3d.decodeTerrainPick(Uint8Array.from([0, 0, 0, 0]), bounds, 32, 32), null);
});

test('source-derived spawn and sign markers map to in-bounds block texels', () => {
  assert.ok(terrain3d, 'fixed-angle world-map helpers have not been implemented');
  const data = terrain3d.buildTerrainMarkerData([
    {x: -14, z: 8, kind: 'spawn'},
    {x: 15, z: -16, kind: 'sign'},
    {x: 16, z: 0, kind: 'sign'},
  ], bounds);

  assert.deepEqual([...data], [2, 24, 0, 31, 0, 1]);
});

test('hover and viewport center coordinates use the rendered terrain picker', async () => {
  const source = await readFile(new URL('../../apps/web/app/map/IsometricTerrainCanvas.tsx', import.meta.url), 'utf8');
  assert.match(source, /const point = pickRef\.current\?\.\(clientX, clientY\);/);
  assert.match(source, /pickRef\.current\?\.\(rect\.left \+ rect\.width \/ 2, rect\.top \+ rect\.height \/ 2\)/);
});

test('isometric view fits the full world at overview and shrinks consistently at 32x', () => {
  assert.ok(terrain3d, 'fixed-angle world-map helpers have not been implemented');
  const overview = terrain3d.terrainViewHeight(3200, 2304, 1.5, 1, -64, 120);
  const detail = terrain3d.terrainViewHeight(3200, 2304, 1.5, 32, -64, 120);

  assert.ok(overview > 0);
  assert.equal(detail, overview / 32);
});

test('WebGL integer height samplers declare explicit high precision in both shaders', async () => {
  const source = await readFile(new URL('../../apps/web/app/map/IsometricTerrainCanvas.tsx', import.meta.url), 'utf8');
  const vertex = source.match(/const VERTEX_SHADER = `([\s\S]*?)`;/)?.[1];
  const fragment = source.match(/const FRAGMENT_SHADER = `([\s\S]*?)`;/)?.[1];
  assert.ok(vertex && fragment, 'WebGL shader sources must be present');
  assert.match(vertex, /precision highp usampler2D;/);
  assert.match(fragment, /precision highp usampler2D;/);
  const vertexMapSize = vertex.match(/uniform (\w+) u_mapSize;/)?.[1];
  const fragmentMapSize = fragment.match(/uniform (\w+) u_mapSize;/)?.[1];
  assert.equal(vertexMapSize, fragmentMapSize, 'linked shader uniforms must have matching types');
  assert.match(vertex, /in float a_markerKind;/);
  assert.match(vertex, /uniform bool u_markerMode;/);
  assert.match(source, /float yaw = \$\{TERRAIN_YAW\};/);
  assert.match(source, /float elevation = \$\{TERRAIN_ELEVATION\};/);
  assert.match(fragment, /if \(u_markerMode\)/);
  assert.match(source, /gl\.drawArrays\(gl\.POINTS/);
});
