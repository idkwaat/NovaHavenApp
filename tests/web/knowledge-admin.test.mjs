import assert from 'node:assert/strict';
import test from 'node:test';
import {readFileSync} from 'node:fs';

test('Knowledge admin editor exposes typed lifecycle and relation controls',()=>{
 const source=readFileSync(new URL('../../apps/web/app/admin/KnowledgeManager.tsx',import.meta.url),'utf8');
 assert.match(source,/\/api\/v1\/admin\/knowledge/);
 assert.match(source,/etag/);
 assert.match(source,/publish/);
 assert.match(source,/locationEntryId|targetEntryId/);
 assert.match(source,/Quest|NPC|Location|Season/);
});
