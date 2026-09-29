import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import test from 'node:test';
import {validateArticle, normalizeSearch, isSafeLink, escapeHtml, markdownToc, wikiCategoryLabel} from '../../apps/web/src/lib/wiki-model.ts';
import contract from '../../contracts/openapi/wiki-v1.json' with {type:'json'};
const root = new URL('../../', import.meta.url);

const valid = {title:'Getting Started',slug:'getting-started',summary:'First steps',markdown:'# Welcome',categoryId:'00000000-0000-4000-8000-000000000001'};
test('draft accepts valid input',()=>assert.deepEqual(validateArticle(valid),{}));
test('draft rejects invalid title, slug, summary and body',()=>{
  const errors=validateArticle({...valid,title:' ',slug:'A--B',summary:'s'.repeat(301),markdown:' '});
  assert.deepEqual(Object.keys(errors).sort(),['markdown','slug','summary','title']);
});
test('search is bounded and whitespace-normalized',()=>{
  assert.equal(normalizeSearch('   fishing   tips  '),'fishing tips');
  assert.equal(normalizeSearch('x'.repeat(121)).length,100);
});
test('Wiki detail shows the published category name instead of its URL slug',()=>{
 assert.equal(wikiCategoryLabel('vung-dat',[{slug:'vung-dat',name:'Vùng đất'}]),'Vùng đất');
 assert.equal(wikiCategoryLabel('missing',[]),'Danh mục Wiki');
});
test('Wiki cards and related links display category names instead of category slugs',async()=>{
 const [listing,detail]=await Promise.all([
  readFile(new URL('apps/web/app/wiki/page.tsx',root),'utf8'),
  readFile(new URL('apps/web/app/wiki/[slug]/page.tsx',root),'utf8'),
 ]);
 assert.match(listing,/wikiCategoryLabel\(item\.category,categories\)/);
 assert.match(detail,/wikiCategoryLabel\(item\.category,categories\)/);
});
test('only http, https and relative wiki links permitted',()=>{
 assert.equal(isSafeLink('javascript:alert(1)'),false);
 assert.equal(isSafeLink('data:text/html,a'),false);
 assert.equal(isSafeLink('//evil.example'),false);
 assert.equal(isSafeLink('/wiki/fishing'),true);
 assert.equal(isSafeLink('https://example.com'),true);
});
test('untrusted Markdown HTML is escaped',()=>assert.equal(escapeHtml('<img src=x onerror=alert(1)>'),'&lt;img src=x onerror=alert(1)&gt;'));
test('markdown TOC creates deterministic duplicate-safe anchors for level one to three headings',()=>{
 const toc=markdownToc('# Overview\n\n#### ignored\n\n## Overview\n\n### Loot [guide](/wiki/loot)');
 assert.deepEqual(toc,[
  {id:'heading-overview',text:'Overview',level:1},
  {id:'heading-overview-2',text:'Overview',level:2},
  {id:'heading-loot-guide',text:'Loot guide',level:3},
 ]);
});
test('markdown TOC ignores headings inside fenced code blocks',()=>{
 assert.deepEqual(markdownToc('```md\n## Not a heading\n```\n\n## Real heading'),[
  {id:'heading-real-heading',text:'Real heading',level:2},
 ]);
});
test('API contract includes public and editorial paths',()=>{
 assert.ok(contract.paths['/api/v1/wiki/articles']);
 assert.ok(contract.paths['/api/v1/admin/wiki/articles/{id}/publish']);
 assert.equal(contract.info.version,'1.0.0');
});
