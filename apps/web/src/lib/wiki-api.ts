export type WikiArticleSummary = {id:string;slug:string;title:string;summary:string;category:string;tags:string[];publishedAt:string;revision:number;previewImageUrl?:string|null;previewImageAlt?:string|null};
export type WikiRelatedArticle = Omit<WikiArticleSummary,'tags'>;
export type WikiArticle = WikiArticleSummary & {markdown:string;related:WikiRelatedArticle[]};
export type WikiTag = {id:string;name:string;slug:string;articleCount:number};
export type WikiCategory = {id:string;name:string;slug:string;articleCount:number;displayOrder:number};
export type Page<T> = {items:T[];page:number;pageSize:number;total:number};

// Only the server executes this client; browser Admin uses same-origin /api requests.
const apiOrigin = process.env.NOVA_API_ORIGIN ?? 'http://localhost:5080';

async function publicGet<T>(path:string):Promise<T> {
  const response=await fetch(new URL(path,apiOrigin),{cache:'no-store',headers:{Accept:'application/json'}});
  if(!response.ok)throw new Error(`Wiki API ${response.status}`);
  return response.json() as Promise<T>;
}
export const wikiApi = {
  tags:()=>publicGet<WikiTag[]>('/api/v1/wiki/tags'),
  categories:()=>publicGet<WikiCategory[]>('/api/v1/wiki/categories'),
  articles:(q='',category='',page=1,tag='')=>{
    const qs=new URLSearchParams({page:String(page),pageSize:'20'});
    if(q)qs.set('q',q);
    if(category)qs.set('category',category);
    if(tag)qs.set('tag',tag);
    return publicGet<Page<WikiArticleSummary>>(`/api/v1/wiki/articles?${qs}`);
  },
  article:(slug:string)=>publicGet<WikiArticle>(`/api/v1/wiki/articles/${encodeURIComponent(slug)}`),
};
