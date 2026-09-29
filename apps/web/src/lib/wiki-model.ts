/** Client-side hints only; the API independently validates every field. */
export type ArticleDraftInput = {
  title: string;
  slug: string;
  summary: string;
  markdown: string;
  categoryId: string;
  tagIds?: string[];
  mediaIds?: string[];
};

export function validateArticle(input: ArticleDraftInput): Record<string,string> {
  const errors: Record<string,string> = {};
  if (!input.title?.trim() || input.title.trim().length > 120) errors.title = 'Tiêu đề cần từ 1 đến 120 ký tự.';
  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(input.slug ?? '') || input.slug.length < 3 || input.slug.length > 120) {
    errors.slug = 'Slug phải dài 3–120 ký tự, chỉ gồm a-z, 0-9 và dấu nối đơn.';
  }
  if ((input.summary ?? '').length > 300) errors.summary = 'Tóm tắt tối đa 300 ký tự.';
  if (!input.markdown?.trim() || input.markdown.length > 50000) errors.markdown = 'Nội dung cần từ 1 đến 50.000 ký tự.';
  if (!/^[\da-f]{8}-(?:[\da-f]{4}-){3}[\da-f]{12}$/i.test(input.categoryId ?? '')) errors.categoryId = 'Chọn danh mục hợp lệ.';
  if (input.tagIds !== undefined && (!Array.isArray(input.tagIds) || input.tagIds.length > 10 ||
      new Set(input.tagIds).size !== input.tagIds.length ||
      input.tagIds.some(id => !/^[\da-f]{8}-(?:[\da-f]{4}-){3}[\da-f]{12}$/i.test(id) || /^0{8}-0{4}-0{4}-0{4}-0{12}$/i.test(id)))) {
    errors.tagIds = 'Chọn tối đa 10 tag khác nhau và có ID hợp lệ.';
  }
  if (input.mediaIds !== undefined && (!Array.isArray(input.mediaIds) || input.mediaIds.length > 20 ||
      new Set(input.mediaIds).size !== input.mediaIds.length ||
      input.mediaIds.some(id => !/^[\da-f]{8}-(?:[\da-f]{4}-){3}[\da-f]{12}$/i.test(id) || /^0{8}-0{4}-0{4}-0{4}-0{12}$/i.test(id)))) {
    errors.mediaIds = 'Chọn tối đa 20 media khác nhau và có ID hợp lệ.';
  }
  return errors;
}

export function normalizeSearch(value: string): string {
  return value.trim().replace(/\s+/g, ' ').slice(0,100);
}

export function isSafeLink(href: string): boolean {
  if (href.startsWith('/') && !href.startsWith('//') && !href.includes('\\')) return true;
  try {
    const url = new URL(href);
    return (url.protocol === 'http:' || url.protocol === 'https:') && Boolean(url.hostname);
  } catch {
    return false;
  }
}

export function escapeHtml(value: string): string {
  return value.replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;').replaceAll('"','&quot;').replaceAll("'",'&#39;');
}

export function wikiListHref(filters:{q?:string;category?:string;tag?:string;page?:number}):string {
  const params = new URLSearchParams();
  if (filters.q) params.set('q',filters.q);
  if (filters.category) params.set('category',filters.category);
  if (filters.tag) params.set('tag',filters.tag);
  if (filters.page && filters.page>1) params.set('page',String(filters.page));
  const query=params.toString();
  return query?`/wiki?${query}`:'/wiki';
}

export function wikiCategoryLabel(categorySlug:string,categories:ReadonlyArray<{slug:string;name:string}>):string{
  return categories.find(category=>category.slug===categorySlug)?.name??'Danh mục Wiki';
}

export type MarkdownHeading = {id:string;text:string;level:number};

export function markdownToc(markdown:string):MarkdownHeading[] {
  const seen=new Map<string,number>();
  const headings:MarkdownHeading[]=[];
  let fenceChar:string|undefined;
  for(const line of markdown.split(/\r?\n/)){
    const fence=/^ {0,3}(`{3,}|~{3,})/.exec(line);
    if(fence){
      const nextFence=fence[1][0];
      if(fenceChar===undefined)fenceChar=nextFence;
      else if(fenceChar===nextFence)fenceChar=undefined;
      continue;
    }
    if(fenceChar!==undefined)continue;
    const match=/^ {0,3}(#{1,3})\s+(.+?)\s*#*\s*$/.exec(line);
    if(!match)continue;
    const text=match[2]
      .replace(/\[([^\]]+)\]\([^)]*\)/g,'$1')
      .replace(/`([^`]+)`/g,'$1')
      .replace(/[\*_~]/g,'')
      .replace(/<[^>]*>/g,'')
      .trim();
    if(!text)continue;
    const base=text.normalize('NFKD').replace(/[\u0300-\u036f]/g,'').toLowerCase()
      .replace(/[^a-z0-9]+/g,'-').replace(/^-+|-+$/g,'')||'section';
    const count=(seen.get(base)??0)+1;
    seen.set(base,count);
    headings.push({id:`heading-${base}${count>1?`-${count}`:''}`,text,level:match[1].length});
  }
  return headings;
}
