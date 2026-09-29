import assert from 'node:assert/strict';
import test from 'node:test';
import source from 'node:fs/promises';

test('Integration admin exposes capability status and uses ETag mutation',async()=>{
 const text=await source.readFile(new URL('../../apps/web/app/admin/IntegrationManager.tsx',import.meta.url),'utf8');
 assert.match(text,/\/api\/v1\/admin\/integrations\/capabilities/);
 assert.match(text,/selected\.etag/);
 assert.match(text,/safeMessage/);
});
