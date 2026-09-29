import assert from 'node:assert/strict';
import test from 'node:test';
import {readFileSync} from 'node:fs';

test('Community admin exposes event, guild, player, housing and leaderboard lifecycle controls',()=>{
 const source=readFileSync(new URL('../../apps/web/app/admin/CommunityManager.tsx',import.meta.url),'utf8');
 assert.match(source,/\/api\/v1\/admin\/community/);
 assert.match(source,/publish/);
 assert.match(source,/etag/);
 assert.match(source,/Event|Guild|Player|Housing|Leaderboard/);
 assert.match(source,/registrations|rows/);
});
