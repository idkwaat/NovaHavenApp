import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import test from 'node:test';

let blueMapServer;
try {
  blueMapServer = await import('../../apps/web/src/lib/bluemap-local-server.ts');
} catch {
  blueMapServer = null;
}

test('BlueMap origin is restricted to an HTTP loopback origin', () => {
  assert.ok(blueMapServer, 'local BlueMap health integration has not been implemented');
  assert.equal(blueMapServer.normalizeBlueMapOrigin('http://127.0.0.1:8100/'), 'http://127.0.0.1:8100');
  assert.equal(blueMapServer.normalizeBlueMapOrigin('http://localhost:8123'), 'http://localhost:8123');
  assert.equal(blueMapServer.normalizeBlueMapOrigin('http://[::1]:8123'), 'http://[::1]:8123');
  assert.equal(blueMapServer.normalizeBlueMapOrigin('http://192.168.1.4:8100'), null);
  assert.equal(blueMapServer.normalizeBlueMapOrigin('https://127.0.0.1:8100'), null);
  assert.equal(blueMapServer.normalizeBlueMapOrigin('http://user@127.0.0.1:8100'), null);
});

test('BlueMap viewer opens the local world at its spawn with a tilted perspective camera', () => {
  assert.ok(blueMapServer, 'local BlueMap health integration has not been implemented');
  const viewerUrl = new URL(blueMapServer.buildBlueMapViewerUrl('http://127.0.0.1:8100'));
  assert.equal(viewerUrl.origin, 'http://127.0.0.1:8100');
  assert.equal(viewerUrl.hash, '#nova_haven:191:64:-71:444:0:0.85:0:0:perspective');
});

test('BlueMap health probe is uncached, bounded and falls back when the service is absent', async () => {
  assert.ok(blueMapServer, 'local BlueMap health integration has not been implemented');
  let request;
  const reachable = await blueMapServer.probeBlueMap('http://127.0.0.1:8100', async (url, options) => {
    request = {url, options};
    return new Response(JSON.stringify({version: '5.16', maps: ['nova_haven']}), {status: 200});
  });
  assert.equal(reachable, true);
  assert.equal(request.url, 'http://127.0.0.1:8100/settings.json');
  assert.equal(request.options.cache, 'no-store');
  assert.equal(request.options.redirect, 'manual');
  assert.equal(await blueMapServer.probeBlueMap('http://127.0.0.1:8100', async () => new Response('<html></html>', {status: 200})), false);
  assert.equal(await blueMapServer.probeBlueMap('http://127.0.0.1:8100', async () => new Response(
    JSON.stringify({version: '5.16', maps: ['overworld']}), {status: 200},
  )), false);
  assert.equal(await blueMapServer.probeBlueMap('http://127.0.0.1:8100', async () => new Response(null, {status: 503})), false);
  assert.equal(await blueMapServer.probeBlueMap(null, async () => { throw new Error('should not fetch'); }), false);
});

test('/map prefers the running local BlueMap perspective viewer and retains the existing terrain fallback', async () => {
  const page = await readFile(new URL('../../apps/web/app/map/page.tsx', import.meta.url), 'utf8');
  const viewer = await readFile(new URL('../../apps/web/app/map/BlueMapViewer.tsx', import.meta.url), 'utf8');
  const css = await readFile(new URL('../../apps/web/app/map/world-map.module.css', import.meta.url), 'utf8');

  assert.match(page, /force-dynamic/);
  assert.match(page, /BlueMapViewer/);
  assert.match(page, /WorldMapExplorer/);
  assert.match(viewer, /<iframe/);
  assert.match(viewer, /title=/);
  assert.match(viewer, /sandbox=/);
  assert.match(css, /\.blueMapFrame\s*\{/);
  assert.match(css, /height:\s*clamp\(420px,\s*78svh,\s*920px\)/);
  assert.match(css, /@media \(max-width: 600px\)/);
});

test('runner pins Java 21 and BlueMap 5.16 and keeps assets outside public web paths', async () => {
  const source = await readFile(new URL('../../scripts/bluemap_local.py', import.meta.url), 'utf8');
  assert.match(source, /BLUEMAP_VERSION\s*=\s*["']5\.16["']/);
  assert.match(source, /require_java_21/);
  assert.match(source, /--accept-mojang-downloads/);
  assert.doesNotMatch(source, /_set_hocon_scalar\(core,\s*["']accept-download["']\s*,\s*["']true["']\)/);
  assert.match(source, /"-m",\s*"nova_haven"/);
  assert.match(source, /example-map-configs/);
  assert.match(source, /127\.0\.0\.1/);
  assert.match(source, /ROOT\s*\/\s*["']\.local["']\s*\/\s*["']bluemap/);
  assert.match(source, /validate_local_root/);
});
