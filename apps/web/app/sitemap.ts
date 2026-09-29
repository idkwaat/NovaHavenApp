import type {MetadataRoute} from 'next';

type ApiRow={slug:string;publishedAt?:string;updatedAt?:string};
type ApiPage={items:ApiRow[];pageSize:number;total:number};
type Endpoint={path:(page:number)=>string;route:(row:ApiRow)=>string};

const apiOrigin=process.env.NOVA_API_ORIGIN??'http://localhost:5080';
const staticRoutes=['/','/wiki','/knowledge','/community','/catalog','/catalog/recipes','/rewards','/commerce','/news'];
const knowledgeKinds=['npc','quest','location','season'];
const communityKinds=['event','guild','player','housing','leaderboard'];

async function page(path:string):Promise<ApiPage|null>{
 try{
  const response=await fetch(new URL(path,apiOrigin),{cache:'no-store',headers:{Accept:'application/json'}});
  if(!response.ok)return null;
  return await response.json() as ApiPage;
 }catch{return null;}
}

async function collect(endpoint:Endpoint):Promise<MetadataRoute.Sitemap>{
 const first=await page(endpoint.path(1));
 if(!first)return [];
 const rows=[...first.items];
 const pageSize=Math.max(1,first.pageSize||first.items.length||50);
 const pages=Math.min(100,Math.ceil(first.total/pageSize));
 for(let current=2;current<=pages;current++){
  const next=await page(endpoint.path(current));
  if(!next)break;
  rows.push(...next.items);
 }
 return rows.map(row=>({url:row.slug?endpoint.route(row):'',...(row.updatedAt||row.publishedAt?{lastModified:row.updatedAt??row.publishedAt}: {})})).filter(item=>item.url);
}

export default async function sitemap():Promise<MetadataRoute.Sitemap>{
 const base=staticRoutes.map(path=>({url:path}));
 const endpoints:Endpoint[]=[
  {path:pageNumber=>`/api/v1/wiki/articles?page=${pageNumber}&pageSize=50`,route:row=>`/wiki/${encodeURIComponent(row.slug)}`},
  {path:pageNumber=>`/api/v1/catalog/items?page=${pageNumber}&pageSize=50`,route:row=>`/catalog/${encodeURIComponent(row.slug)}`},
  {path:pageNumber=>`/api/v1/catalog/recipes?page=${pageNumber}&pageSize=50`,route:row=>`/catalog/recipes/${encodeURIComponent(row.slug)}`},
  {path:pageNumber=>`/api/v1/rewards?page=${pageNumber}&pageSize=50`,route:row=>`/rewards/${encodeURIComponent(row.slug)}`},
  {path:pageNumber=>`/api/v1/commerce/offers?page=${pageNumber}&pageSize=50`,route:row=>`/commerce/${encodeURIComponent(row.slug)}`},
  {path:pageNumber=>`/api/v1/news?page=${pageNumber}&pageSize=50`,route:row=>`/news/${encodeURIComponent(row.slug)}`},
  ...knowledgeKinds.map(kind=>({path:(pageNumber:number)=>`/api/v1/knowledge/${kind}?page=${pageNumber}&pageSize=50`,route:(row:ApiRow)=>`/knowledge/${kind}/${encodeURIComponent(row.slug)}`})),
  ...communityKinds.map(kind=>({path:(pageNumber:number)=>`/api/v1/community/${kind}?page=${pageNumber}&pageSize=50`,route:(row:ApiRow)=>`/community/${kind}/${encodeURIComponent(row.slug)}`}))
 ];
 const dynamic=(await Promise.all(endpoints.map(collect))).flat();
 const unique=new Map<string,MetadataRoute.Sitemap[number]>();
 for(const entry of [...base,...dynamic])unique.set(entry.url,entry);
 return [...unique.values()];
}
