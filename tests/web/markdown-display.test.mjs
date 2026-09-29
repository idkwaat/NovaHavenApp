import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import test from 'node:test';
import {omitRepeatedTitleHeading} from '../../apps/web/src/lib/markdown-display.ts';
const root = new URL('../../', import.meta.url);

test('public article renderer omits a leading Markdown heading that duplicates the page title',()=>{
 assert.equal(omitRepeatedTitleHeading('\n# Những Người Mở Đường\n\nĐoạn giới thiệu.', 'Những Người Mở Đường'),'Đoạn giới thiệu.');
});

test('duplicate heading comparison ignores case, Unicode composition and extra spacing',()=>{
 assert.equal(omitRepeatedTitleHeading('# CAFE\u0301  SAO\n\nNội dung', 'Café Sao'),'Nội dung');
});

test('meaningful first headings and title mentions in prose are preserved',()=>{
 const markdown='# Bối cảnh\n\n# Những Người Mở Đường\n\nĐoạn giới thiệu.';
 assert.equal(omitRepeatedTitleHeading(markdown,'Những Người Mở Đường'),markdown);
});

test('public Markdown detail routes suppress only a duplicate page-title heading',async()=>{
 const paths=['apps/web/app/wiki/[slug]/page.tsx','apps/web/app/catalog/[slug]/page.tsx','apps/web/app/catalog/recipes/[slug]/page.tsx','apps/web/app/news/[slug]/page.tsx','apps/web/app/knowledge/[kind]/[slug]/page.tsx','apps/web/app/community/[kind]/[slug]/page.tsx'];
 const pages=await Promise.all(paths.map(path=>readFile(new URL(path,root),'utf8')));
 for(const page of pages)assert.match(page,/omitRepeatedTitleHeading/);
 assert.match(pages[0],/wikiCategoryLabel\(article\.category,categories\)/);
});
