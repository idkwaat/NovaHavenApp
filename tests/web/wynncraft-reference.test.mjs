import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import test from 'node:test';

const root = new URL('../../', import.meta.url);
const read = path => readFile(new URL(path, root), 'utf8');
const lastRule = (css, selector) =>
  [...css.matchAll(new RegExp(`(?:^|\\n)${selector}\\s*\\{([^}]*)\\}`, 'g'))].at(-1)?.[1] ?? '';

function relativeLuminance(hex) {
  const [red, green, blue] = hex.match(/[\da-f]{2}/gi).map(channel => {
    const value = Number.parseInt(channel, 16) / 255;
    return value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
  });
  return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
}

function contrastRatio(first, second) {
  const values = [relativeLuminance(first), relativeLuminance(second)].sort((a, b) => b - a);
  return (values[0] + 0.05) / (values[1] + 0.05);
}

test('home replaces the blurred screenshot with a crisp landscape photo and keeps the timber Nova sign', async () => {
  const home = await read('apps/web/app/page.tsx');
  const theme = await read('apps/web/app/wynn-parity.css');

  assert.match(home, /className="hero hero-home"/);
  assert.match(home, /className="hero-signboard"[^>]*>[\s\S]*?<p className="hero-brand">NOVA HAVEN<\/p>/);
  assert.match(home, /className="hero-server"/);
  assert.match(theme, /\.hero-home \.hero-landscape-image\s*\{[^}]*background-size:\s*cover;[^}]*background-position:\s*50% 52%;/s);
  assert.match(theme, /photo-1760138338534-50b64e75267c[^\"]*w=1920[^\"]*h=1280/);
  assert.doesNotMatch(theme, /Minecraft_Beta_1\.8\.1_Gameplay_Screenshot\.png/);
  assert.match(theme, /\.hero-home \.hero-signboard\s*\{[^}]*background:\s*linear-gradient\(/s);
  assert.match(theme, /\.hero-home \.hero-signboard\s*\{[^}]*border:\s*[0-9]+px solid/s);
  assert.match(theme, /\.hero-home \.hero-copy\s*\{[^}]*position:\s*absolute;[^}]*left:\s*50%;[^}]*transform:\s*translate\(-50%,\s*-50%\)/s);
  assert.match(theme, /\.hero-home \.hero-copy\s*\{[^}]*text-align:\s*center/s);
  assert.match(theme, /\.hero-home \.hero-landscape-image\s*\{[^}]*filter:\s*brightness\(\.64\) saturate\(\.76\) sepia\(\.2\) contrast\(1\.06\)/s);
  assert.match(theme, /\.hero-home h1\s*\{[^}]*font-family:\s*Arial,[^}]*font-style:\s*normal/s);
  assert.doesNotMatch(home, /hero-art|hero-character|hero-dragon|starter-card/);
});

test('Wiki and item guide have distinct, readable visual surfaces', async () => {
  const wiki = await read('apps/web/app/wiki/page.tsx');
  const catalog = await read('apps/web/app/catalog/page.tsx');
  const theme = await read('apps/web/app/wynn-parity.css');

  assert.match(wiki, /className="wiki-layout"/);
  assert.match(wiki, /articles\.items\.map/);
  assert.match(catalog, /className="catalog-layout"/);
  assert.match(catalog, /result\.items\.map/);
  assert.match(lastRule(theme, String.raw`body\s*>\s*main\s*>\s*\.content\.wiki-page`), /width:\s*min\(100%,\s*1240px\)[\s\S]*background:\s*var\(--nh-paper\)/i);
  assert.match(theme, /\.wiki-page\s*>\s*h1\s*\{[^}]*text-align:\s*center/s);
  assert.match(lastRule(theme, String.raw`body\s*>\s*main\s*>\s*\.content\.wiki-page`), /max-width:\s*1240px/s);
  assert.match(theme, /\.wiki-layout\s*\{[^}]*background:\s*transparent/i);
  assert.match(theme, /\.wiki-results \.article-grid\s*\{[^}]*grid-template-columns:\s*1fr/s);
  assert.match(lastRule(theme, String.raw`\.wiki-results \.article-card`), /background:\s*var\(--nh-paper-light\)/i);
  assert.match(lastRule(theme, String.raw`body\s*>\s*main\s*>\s*\.content\.catalog-page`), /background:\s*var\(--nh-sage\)/i);
  assert.match(lastRule(theme, String.raw`(?:\.wiki-filters,\s*)?\.catalog-filters`), /background:\s*var\(--nh-paper-light\)/i);
  assert.match(lastRule(theme, String.raw`\.catalog-layout`), /grid-template-columns:\s*minmax\(240px,\s*290px\)/s);
  assert.match(theme, /\.catalog-results \.article-grid\s*\{[^}]*grid-template-columns:\s*1fr/s);
});

test('News remains API-backed and uses an image-led, credited editorial treatment', async () => {
  const news = await read('apps/web/app/news/page.tsx');
  const theme = await read('apps/web/app/wynn-parity.css');
  const layout = await read('apps/web/app/layout.tsx');

  assert.match(news, /result\.items\.map/);
  assert.match(news, /<ArticlePreview image=\{preview\}/);
  assert.match(news, /extractMarkdownPreview/);
  assert.match(news, /className="news-aside"/);
  assert.match(theme, /\.editorial-preview-frame\s*\{[^}]*aspect-ratio:\s*16\s*\/\s*9/s);
  assert.match(lastRule(theme, String.raw`\.news-card\.editorial-news-card \.editorial-preview`), /display:\s*flex/i);
  assert.match(lastRule(theme, String.raw`\.news-card\.editorial-news-card \.editorial-preview-frame`), /flex:\s*1\s+1\s+0/i);
  assert.match(theme, /@media\s*\(max-width:\s*760px\)[\s\S]*?\.news-card\.editorial-news-card \.editorial-preview\s*\{[^}]*display:\s*block/s);
  assert.doesNotMatch(theme, /\.news-image(?:\s|::)/);
  assert.match(theme, /\.news-layout\s*\{[^}]*grid-template-columns:/s);
  assert.match(theme, /\.news-layout\s*\{[^}]*width:\s*min\(100%,\s*880px\)/s);
  assert.match(lastRule(theme, String.raw`body\s*>\s*main\s*>\s*\.content\.news-page`), /background:\s*var\(--nh-sky\)/i);
  assert.match(layout, /Xbox México/);
  assert.match(layout, /https:\/\/unsplash\.com\/photos\/green-meadow-with-trees-and-a-river-near-mountains-X7FEgrPDFNU/);
  assert.match(layout, /Ảnh phong cảnh: Einar Storsul · Unsplash/);
  assert.match(layout, /https:\/\/commons\.wikimedia\.org\/wiki\/File:Minecraft_-_1\.18_mountains\.jpg/);
});

test('public typography avoids forced smoothing and layouts collapse on mobile', async () => {
  const theme = await read('apps/web/app/wynn-parity.css');
  const layout = await read('apps/web/app/layout.tsx');

  assert.match(theme, /text-rendering:\s*auto/);
  assert.match(theme, /-webkit-font-smoothing:\s*auto/);
  assert.match(theme, /@media\s*\(max-width:\s*760px\)/);
  assert.ok(layout.indexOf("import './styles.css'") < layout.indexOf("import './wynn-parity.css'"));
});

test('secondary navigation links keep mobile touch targets at least 44px high', async () => {
  const theme = await read('apps/web/app/wynn-parity.css');

  for (const selector of [
    String.raw`\.home-intro \.text-link`,
    String.raw`\.catalog-filter-form \.filter-reset`,
    String.raw`\.catalog-page \.recipe-link`,
    String.raw`\.community-kind-page > a:first-child`,
  ]) {
    assert.match(theme, new RegExp(`${selector}\\s*\\{[^}]*min-height:\\s*44px`, 's'));
  }
});

test('editorial form fields keep a 44px mobile touch height', async () => {
  const theme = await read('apps/web/app/wynn-parity.css');

  assert.match(theme, /\.form input:not\(\[type=checkbox\]\):not\(\[type=radio\]\),\s*\.form select,\s*\.form textarea\s*\{[^}]*min-height:\s*44px/s);
});

test('home controls, labels, and CTA colors meet the UI accessibility baseline', async () => {
  const theme = await read('apps/web/app/wynn-parity.css');
  const token = name => theme.match(new RegExp(`--nh-${name}:\\s*(#[\\da-f]{6})`, 'i'))?.[1];
  const introForeground = token('harvest');
  const introBackground = token('paper');
  const text = token('text');
  const green = token('cta-green');
  const blue = token('cta-blue');
  const greenHover = theme.match(/\.button-green:hover\s*\{[^}]*background:\s*(#[\da-f]{6})/i)?.[1];
  const blueHover = theme.match(/\.button-blue:hover\s*\{[^}]*background:\s*(#[\da-f]{6})/i)?.[1];

  assert.ok(introForeground && introBackground, 'home intro label and surface colors are defined');
  assert.ok(contrastRatio(introForeground, introBackground) >= 4.5, 'home intro label has WCAG AA text contrast');
  assert.ok(green && text && contrastRatio(text, green) >= 4.5, 'green CTA has WCAG AA contrast with its text');
  assert.ok(blue && text && contrastRatio(text, blue) >= 4.5, 'blue CTA has WCAG AA contrast with its text');
  assert.ok(greenHover && text && contrastRatio(greenHover, text) >= 4.5, 'green CTA hover keeps WCAG AA contrast');
  assert.ok(blueHover && text && contrastRatio(blueHover, text) >= 4.5, 'blue CTA hover keeps WCAG AA contrast');
  assert.match(theme, /\.site-header \.menu-button,[\s\S]*?\.site-header \.home-button,[\s\S]*?\.site-header \.search-button\s*\{\s*width:\s*44px;\s*min-width:\s*44px;\s*height:\s*44px;/);
  assert.match(theme, /\.site-header \.menu-button,[\s\S]*?\.toolbar-search-form button\s*\{\s*min-height:\s*44px;/);
  assert.match(theme, /\.content \.eyebrow\s*\{[^}]*font-size:\s*\.75rem/s);
  assert.match(theme, /\.footer-brand span, \.footer-meta span, \.footer-meta a\s*\{[^}]*font-size:\s*\.78rem/s);
  assert.match(theme, /\.wiki-filters \.search-bar button, \.catalog-filter-form button\s*\{[^}]*min-height:\s*44px/s);
  assert.match(theme, /\.category-list a, \.tag-list a\s*\{[^}]*min-height:\s*44px/s);
  assert.match(theme, /\.footer-links a\s*\{[^}]*min-width:\s*44px/s);
});

test('public content fills the viewport and the footer brand stays on one line', async () => {
  const theme = await read('apps/web/app/wynn-parity.css');

  assert.match(theme, /body\s*>\s*main\s*\{[^}]*display:\s*flex;[^}]*flex-direction:\s*column/s);
  assert.match(theme, /body\s*>\s*main\s*>\s*\.content\s*\{[^}]*flex:\s*1/s);
  assert.match(theme, /\.site-footer \.footer-brand strong\s*\{[^}]*white-space:\s*nowrap/s);
});

test('public screens use distinct dark-earth surfaces with crisp readable text', async () => {
  const theme = await read('apps/web/app/wynn-parity.css');
  const token = name => theme.match(new RegExp(`--nh-${name}:\\s*(#[\\da-f]{6})`, 'i'))?.[1];
  const forest = token('forest');
  const lightText = token('text');
  const ink = token('ink');
  const paper = token('paper-light');
  const muted = token('page-muted');
  const sage = token('sage');
  const sky = token('sky');

  assert.ok(forest && lightText && ink && paper && muted && sage && sky, 'shared dark-earth and text roles are defined');
  assert.match(theme, /color-scheme:\s*dark/i);
  assert.ok(contrastRatio(lightText, forest) >= 4.5, 'light text remains readable on the forest frame');
  assert.ok(contrastRatio(ink, paper) >= 4.5, 'primary text remains readable on raised soil surfaces');
  assert.ok(contrastRatio(muted, paper) >= 4.5, 'secondary text remains readable on raised soil surfaces');
  assert.ok(contrastRatio(ink, sage) >= 4.5, 'primary text remains readable on olive surfaces');
  assert.ok(contrastRatio(ink, sky) >= 4.5, 'primary text remains readable on teal-brown surfaces');
  assert.equal(new Set([forest, paper, sage, sky]).size, 4, 'page surfaces have visible tonal separation');
  assert.match(theme, /\.wiki-page\s*\{[^}]*background:\s*var\(--nh-paper\)/s);
  assert.match(theme, /\.catalog-page\s*\{[^}]*background:\s*var\(--nh-sage\)/s);
  assert.match(theme, /\.news-card\s*\{[^}]*background:\s*var\(--nh-paper-light\)/s);
  assert.match(theme, /\.site-nav-drawer\s*\{[^}]*background:\s*var\(--nh-paper\)/s);
  assert.match(lastRule(theme, String.raw`\.content h1`), /text-shadow:\s*none/s);
  assert.match(lastRule(theme, String.raw`\.content h1`), /line-height:\s*1\.18/s);
  assert.match(lastRule(theme, String.raw`\.content h1,\s*\.content h2,\s*\.content h3`), /font-family:\s*Arial,\s*"Segoe UI",\s*Tahoma,\s*sans-serif/s);
  assert.match(lastRule(theme, String.raw`body\s*>\s*main\s*>\s*\.content\.quote-panel`), /background:\s*var\(--nh-paper\)/s);
  assert.match(lastRule(theme, String.raw`\.content\.quote-panel h2`), /color:\s*var\(--nh-ink\)/s);
  assert.match(lastRule(theme, String.raw`\.content\.quote-panel > p:not\(\.eyebrow\)`), /color:\s*var\(--nh-page-muted\)/s);
  assert.match(lastRule(theme, String.raw`\.news-card`), /background:\s*var\(--nh-paper-light\)/s);
  assert.match(lastRule(theme, String.raw`\.site-nav-drawer`), /background:\s*var\(--nh-paper\)/s);
});

test('web and companion share a charcoal-olive-teal palette instead of brown cards', async () => {
  const theme = await read('apps/web/app/wynn-parity.css');
  const mobile = await read('apps/mobile/lib/nova_theme.dart');
  assert.match(theme, /--nh-paper:\s*#252d2a/i);
  assert.match(theme, /--nh-paper-light:\s*#303b35/i);
  assert.match(theme, /--nh-sage:\s*#2b3530/i);
  assert.match(theme, /--nh-sky:\s*#293a3b/i);
  assert.match(mobile, /static const paper = Color\(0xFF252D2A\)/);
  assert.match(mobile, /static const raisedPaper = Color\(0xFF303B35\)/);
});

test('the shared palette uses dark soil, timber, olive and restrained harvest tones accessibly', async () => {
  const theme = await read('apps/web/app/wynn-parity.css');
  const token = name => theme.match(new RegExp(`--nh-${name}:\\s*(#[\\da-f]{6})`, 'i'))?.[1];
  const ink = token('ink');
  const paper = token('paper-light');
  const sage = token('sage');
  const forest = token('forest');
  const text = token('text');
  const muted = token('page-muted');

  const sky = token('sky');
  const ctaGreen = token('cta-green');
  const ctaBlue = token('cta-blue');
  assert.ok(ink && paper && sage && forest && text && muted && sky, 'the shared earthy, olive and text tokens exist');
  assert.match(theme, /--nh-harvest:\s*#[\da-f]{6}/i);
  assert.match(theme, /--nh-berry:\s*#[\da-f]{6}/i);
  assert.ok(contrastRatio(ink, paper) >= 4.5, 'warm text is readable on wood surfaces');
  assert.ok(contrastRatio(ink, sage) >= 4.5, 'warm text is readable on olive surfaces');
  assert.ok(contrastRatio(ink, sky) >= 4.5, 'warm text is readable on muted teal surfaces');
  assert.ok(contrastRatio(muted, paper) >= 4.5, 'muted editorial text remains readable on wood surfaces');
  assert.ok(contrastRatio(text, forest) >= 4.5, 'light toolbar/footer text is readable on forest');
  assert.ok(ctaGreen && contrastRatio(text, ctaGreen) >= 4.5, 'olive CTA remains readable');
  assert.ok(ctaBlue && contrastRatio(text, ctaBlue) >= 4.5, 'muted-teal CTA remains readable');
  assert.ok(relativeLuminance(paper) < 0.06, 'paper is a dark earthy value');
  assert.ok(relativeLuminance(token('paper')) < 0.06, 'raised paper stays dark');
  assert.match(theme, /body\s*>\s*main\s*>\s*\.content\.home-categories\s*\{[^}]*color:\s*var\(--nh-ink\);\s*background:\s*var\(--nh-sage\)/s);
  assert.match(theme, /\.home-categories \.tile\s*\{[^}]*background:\s*var\(--nh-paper-light\)/s);
  assert.match(theme, /@media\s*\(max-width:\s*760px\)[\s\S]*?\.hero-home \.hero-landscape-image\s*\{[^}]*background-size:\s*cover/s);
});
