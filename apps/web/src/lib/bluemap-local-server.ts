const DEFAULT_BLUE_MAP_ORIGIN = 'http://127.0.0.1:8100';
const LOOPBACK_HOSTS = new Set(['127.0.0.1', 'localhost', '::1', '[::1]']);
const BLUE_MAP_START_ANCHOR = 'nova_haven:191:64:-71:444:0:0.85:0:0:perspective';

export function normalizeBlueMapOrigin(value: string | undefined): string | null {
  if (!value) return null;
  try {
    const url = new URL(value);
    if (url.protocol !== 'http:' || !LOOPBACK_HOSTS.has(url.hostname.toLowerCase()) ||
        url.username || url.password || !['', '/'].includes(url.pathname) || url.search || url.hash) return null;
    const port = url.port ? Number(url.port) : 80;
    if (!Number.isInteger(port) || port < 1 || port > 65535) return null;
    return url.origin;
  } catch {
    return null;
  }
}

export function buildBlueMapViewerUrl(origin: string): string {
  const safeOrigin = normalizeBlueMapOrigin(origin);
  if (!safeOrigin) throw new TypeError('BlueMap viewer origin must be a local HTTP loopback address.');
  return `${safeOrigin}/#${BLUE_MAP_START_ANCHOR}`;
}

export async function probeBlueMap(
  origin: string | null,
  fetcher: typeof fetch = fetch,
): Promise<boolean> {
  const safeOrigin = normalizeBlueMapOrigin(origin ?? undefined);
  if (!safeOrigin) return false;
  try {
    const response = await fetcher(safeOrigin + '/settings.json', {
      method: 'GET',
      cache: 'no-store',
      redirect: 'manual',
      signal: AbortSignal.timeout(800),
    });
    if (!response.ok) {
      await response.body?.cancel().catch(() => undefined);
      return false;
    }
    if (!response.body) return false;
    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let size = 0;
    let body = '';
    while (true) {
      const {done, value} = await reader.read();
      if (done) break;
      size += value.byteLength;
      if (size > 64 * 1024) {
        await reader.cancel().catch(() => undefined);
        return false;
      }
      body += decoder.decode(value, {stream: true});
    }
    body += decoder.decode();
    const settings: unknown = JSON.parse(body);
    if (typeof settings !== 'object' || settings === null) return false;
    const candidate = settings as {version?: unknown; maps?: unknown};
    return candidate.version === '5.16' && Array.isArray(candidate.maps) && candidate.maps.includes('nova_haven');
  } catch {
    return false;
  }
}

export async function getBlueMapLocalOrigin(fetcher: typeof fetch = fetch): Promise<string | null> {
  const origin = normalizeBlueMapOrigin(process.env.NOVA_BLUEMAP_ORIGIN ?? DEFAULT_BLUE_MAP_ORIGIN);
  return await probeBlueMap(origin, fetcher) ? origin : null;
}
