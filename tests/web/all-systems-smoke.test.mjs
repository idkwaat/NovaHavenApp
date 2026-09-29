import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import test from 'node:test';

const smoke = readFileSync(new URL('../../scripts/all-systems-smoke.mjs', import.meta.url), 'utf8');

test('all-systems smoke is local-only and covers every implemented public slice', () => {
  assert.match(smoke, /NOVA_API_URL/);
  assert.match(smoke, /localhost\|127/);
  assert.match(smoke, /Content-Type.*application\/json/);
  for (const path of [
    '/api/v1/knowledge',
    '/api/v1/community',
    '/api/v1/integrations/status',
    '/api/v1/rewards',
    '/api/v1/commerce/offers',
    '/api/v1/admin/integrations/capabilities',
    '/api/v1/admin/rewards',
    '/api/v1/admin/commerce/offers',
    '/api/v1/admin/commerce/orders'
  ]) assert.match(smoke, new RegExp(path.replaceAll('/', '\\/')));
  assert.match(smoke, /definitionOnly/);
  assert.match(smoke, /externalAcknowledgementRequired/);
  assert.match(smoke, /commerce\/orders/);
});

test('all-systems smoke does not expose or invoke external execution', () => {
  assert.doesNotMatch(smoke, /providerUrl|stripe|paypal|grantReward|minecraftDatabase/i);
  assert.match(smoke, /local demo/i);
  assert.match(smoke, /400/);
});
