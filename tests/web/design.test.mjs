import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import test from 'node:test';

const root = new URL('../../', import.meta.url);
const read = path => readFile(new URL(path, root), 'utf8');

test('public visual system uses the real home hero and stays scoped to Nova components', async () => {
  const css = await read('apps/web/app/wynn-parity.css');
  const home = await read('apps/web/app/page.tsx');
  const layout = await read('apps/web/app/layout.tsx');
  const navigation = await read('apps/web/app/SiteNavigation.tsx');

  assert.match(home, /className="hero hero-home"/);
  assert.match(home, /className="hero-landscape-image"/);
  assert.match(home, /className="hero-brand"/);
  assert.match(home, /className="home-intro-band"/);
  assert.doesNotMatch(home, /hero-art|scene-art|hero-character|hero-dragon/);
  assert.match(navigation, /game-toolbar/);
  assert.match(navigation, /server-pill/);
  assert.match(css, /--nh-paper:/);
  assert.match(css, /--nh-green:/);
  assert.match(layout, /import '\.\/wynn-parity\.css'/);
  assert.match(layout, /Ảnh phong cảnh: Einar Storsul · Unsplash/);
  assert.match(layout, /green-meadow-with-trees-and-a-river-near-mountains-X7FEgrPDFNU/);
  assert.match(layout, /Minecraft_-_1\.18_mountains\.jpg/);
  assert.match(layout, /Xbox México · CC BY 3\.0/);
});

test('homepage hero fits short landscape viewports without hiding its primary actions', async () => {
  const css = await read('apps/web/app/wynn-parity.css');

  assert.match(css, /@media\s*\(orientation:\s*landscape\)\s*and\s*\(max-height:\s*500px\)[\s\S]*?\.hero\.hero-home\s*\{[^}]*min-height:\s*calc\(100svh - 63px\)/s);
  assert.match(css, /\.hero-home \.hero-copy\s*\{[^}]*top:\s*50%/s);
  assert.match(css, /\.hero-home \.button\s*\{[^}]*min-height:\s*44px/s);
});
