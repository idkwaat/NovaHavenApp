import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import test from 'node:test';

const developmentSettings = JSON.parse(await readFile(
  new URL('../../backend/NovaHaven.Api/appsettings.Development.json', import.meta.url),
  'utf8'
));
const baseSettings = JSON.parse(await readFile(
  new URL('../../backend/NovaHaven.Api/appsettings.json', import.meta.url),
  'utf8'
));

test('API config requires a local database connection and Admin bootstrap to be set explicitly', () => {
  assert.equal(developmentSettings.SeedAdmin?.Enabled, false);
  assert.equal(Object.hasOwn(developmentSettings.SeedAdmin ?? {}, 'Email'), false);
  assert.equal(Object.hasOwn(developmentSettings.SeedAdmin ?? {}, 'Password'), false);
  assert.equal('ConnectionStrings' in developmentSettings, false);
  assert.equal('ConnectionStrings' in baseSettings, false);
});
