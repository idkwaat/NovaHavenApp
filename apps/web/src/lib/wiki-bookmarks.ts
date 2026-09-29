import type {WikiArticle} from './wiki-api';

export const wikiBookmarkStorageKey='nova_haven_wiki_bookmarks_v1';
export const maxWikiBookmarks=100;

const slugPattern=/^[a-z0-9]+(?:-[a-z0-9]+)*$/;

export function isWikiBookmarkSlug(value:unknown):value is string {
  return typeof value==='string'&&value.length>=3&&value.length<=120&&slugPattern.test(value);
}

export function readBookmarkSlugs(storage:Pick<Storage,'getItem'>):string[] {
  const raw=storage.getItem(wikiBookmarkStorageKey);
  if(!raw)return [];
  try{
    const parsed:unknown=JSON.parse(raw);
    if(!Array.isArray(parsed))return [];
    const unique=new Set<string>();
    for(const value of parsed){
      if(isWikiBookmarkSlug(value))unique.add(value);
      if(unique.size>=maxWikiBookmarks)break;
    }
    return [...unique];
  }catch{
    return [];
  }
}

export function toggleBookmark(storage:Pick<Storage,'getItem'|'setItem'>,slug:string):string[] {
  if(!isWikiBookmarkSlug(slug))throw new Error('Slug bài Wiki không hợp lệ.');
  const current=readBookmarkSlugs(storage);
  const next=current.includes(slug)?current.filter(value=>value!==slug):[slug,...current].slice(0,maxWikiBookmarks);
  storage.setItem(wikiBookmarkStorageKey,JSON.stringify(next));
  return next;
}

function normalizeSearchText(value:string):string {
  return value.normalize('NFD').replace(/\p{M}/gu,'').replace(/[-_]+/g,' ').toLocaleLowerCase().replace(/\s+/g,' ').trim();
}

export function filterBookmarkedArticles<T extends Pick<WikiArticle,'title'|'summary'|'tags'|'category'>>(
  articles:readonly T[],
  query:string,
):T[] {
  const normalizedQuery=normalizeSearchText(query);
  if(!normalizedQuery)return [...articles];
  return articles.filter(article=>normalizeSearchText([article.title,article.summary,article.category,...article.tags].join(' ')).includes(normalizedQuery));
}

function isWikiArticle(value:unknown,expectedSlug:string):value is WikiArticle {
  if(!value||typeof value!=='object')return false;
  const article=value as Partial<WikiArticle>;
  return article.slug===expectedSlug
    &&typeof article.id==='string'
    &&typeof article.title==='string'
    &&typeof article.summary==='string'
    &&typeof article.category==='string'
    &&Array.isArray(article.tags)&&article.tags.every(tag=>typeof tag==='string')
    &&typeof article.publishedAt==='string'
    &&Number.isInteger(article.revision)
    &&typeof article.markdown==='string'
    &&Array.isArray(article.related);
}

export async function loadPublishedWikiArticles(
  slugs:readonly string[],
  fetcher:typeof fetch=fetch,
  requestedConcurrency=4,
):Promise<{articles:WikiArticle[];unavailableCount:number}> {
  const validSlugs=[...new Set(slugs.filter(isWikiBookmarkSlug))].slice(0,maxWikiBookmarks);
  const articles:Array<WikiArticle|undefined>=new Array(validSlugs.length);
  let unavailableCount=0;
  let cursor=0;
  const concurrency=Math.max(1,Math.min(maxWikiBookmarks,Math.floor(requestedConcurrency)||1));
  const worker=async()=>{
    while(cursor<validSlugs.length){
      const index=cursor++;
      const slug=validSlugs[index];
      const response=await fetcher(`/api/v1/wiki/articles/${encodeURIComponent(slug)}`,{
        method:'GET',
        cache:'no-store',
        credentials:'same-origin',
        headers:{Accept:'application/json'},
      });
      if(response.status===404){unavailableCount++;continue;}
      if(!response.ok)throw new Error(`Wiki API ${response.status}`);
      const payload:unknown=await response.json();
      if(!isWikiArticle(payload,slug))throw new Error('Wiki API trả về dữ liệu bài viết không hợp lệ.');
      articles[index]=payload;
    }
  };
  await Promise.all(Array.from({length:Math.min(concurrency,validSlugs.length)},()=>worker()));
  return {articles:articles.filter((article):article is WikiArticle=>article!==undefined),unavailableCount};
}
