export const wikiRecentStorageKey='nova_haven_wiki_recent_v1';
export const maxRecentWikiArticles=20;
const wikiSlugPattern=/^[a-z0-9]+(?:-[a-z0-9]+)*$/;

function isRecentWikiSlug(value:unknown):value is string {
  return typeof value==='string'&&value.length>=3&&value.length<=120&&wikiSlugPattern.test(value);
}

export function readRecentWikiSlugs(storage:Pick<Storage,'getItem'>):string[] {
  const raw=storage.getItem(wikiRecentStorageKey);
  if(!raw)return [];
  try{
    const parsed:unknown=JSON.parse(raw);
    if(!Array.isArray(parsed))return [];
    const unique=new Set<string>();
    for(const value of parsed){
      if(isRecentWikiSlug(value))unique.add(value);
      if(unique.size>=maxRecentWikiArticles)break;
    }
    return [...unique];
  }catch{
    return [];
  }
}

export function recordRecentWikiArticle(storage:Pick<Storage,'getItem'|'setItem'>,slug:string):string[] {
  if(!isRecentWikiSlug(slug))throw new Error('Slug bài Wiki không hợp lệ.');
  const next=[slug,...readRecentWikiSlugs(storage).filter(value=>value!==slug)].slice(0,maxRecentWikiArticles);
  storage.setItem(wikiRecentStorageKey,JSON.stringify(next));
  return next;
}

export function clearRecentWikiArticles(storage:Pick<Storage,'removeItem'>):void {
  storage.removeItem(wikiRecentStorageKey);
}
