export type CatalogKind = 'item'|'weapon'|'armor'|'material'|'fish'|'crop';
export type CatalogSummary = {id:string;slug:string;name:string;summary:string;kind:CatalogKind;revision:number;publishedAt:string;updatedAt:string};
export type CatalogItem = CatalogSummary & {markdown:string};
export type CatalogPage = {items:CatalogSummary[];page:number;pageSize:number;total:number};
export type RecipeComponent = {itemId:string;itemSlug:string;itemName:string;quantity:number};
export type CatalogRecipeSummary = {id:string;slug:string;name:string;summary:string;revision:number;publishedAt:string;updatedAt:string};
export type CatalogRecipe = CatalogRecipeSummary & {markdown:string;ingredients:RecipeComponent[];outputs:RecipeComponent[]};

const apiOrigin = process.env.NOVA_API_ORIGIN ?? 'http://localhost:5080';

async function publicGet<T>(path:string):Promise<T> {
  const response = await fetch(new URL(path,apiOrigin),{cache:'no-store',headers:{Accept:'application/json'}});
  if (!response.ok) throw new Error(`Catalog API ${response.status}`);
  return response.json() as Promise<T>;
}

export const catalogApi = {
  items:(q='',kind='',page=1)=>{
    const query = new URLSearchParams({page:String(page),pageSize:'20'});
    if (q) query.set('q',q);
    if (kind) query.set('kind',kind);
    return publicGet<CatalogPage>(`/api/v1/catalog/items?${query}`);
  },
  item:(slug:string)=>publicGet<CatalogItem>(`/api/v1/catalog/items/${encodeURIComponent(slug)}`),
  recipes:(page=1)=>publicGet<{items:CatalogRecipeSummary[];page:number;pageSize:number;total:number}>(`/api/v1/catalog/recipes?page=${page}&pageSize=20`),
  recipe:(slug:string)=>publicGet<CatalogRecipe>(`/api/v1/catalog/recipes/${encodeURIComponent(slug)}`),
};

export const catalogKindLabels:Record<CatalogKind,string> = {
  item:'Vật phẩm', weapon:'Vũ khí', armor:'Giáp', material:'Nguyên liệu', fish:'Cá', crop:'Nông sản',
};
