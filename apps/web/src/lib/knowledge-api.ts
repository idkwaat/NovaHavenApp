export type KnowledgeKind='npc'|'quest'|'location'|'season';
export type KnowledgeSummary={id:string;slug:string;name:string;summary:string;kind:KnowledgeKind;revision:number;publishedAt:string;previewImageUrl?:string|null};
export type KnowledgeLink={linkType:string;slug:string;name:string;type:string;sortOrder:number};
export type KnowledgeMetadata={
 role?:string;portraitUrl?:string|null;location?:{slug:string;name:string;kind:KnowledgeKind}|null;
 difficulty?:number;giver?:{slug:string;name:string;kind:KnowledgeKind}|null;
 rewardDescription?:string;steps?:{position:number;title:string;description:string}[];
 region?:string;locationType?:string;latitude?:number|null;longitude?:number|null;mapImageUrl?:string|null;
 startsAt?:string;endsAt?:string;theme?:string;eventDescription?:string|null;
};
export type KnowledgeDetail=KnowledgeSummary&{markdown:string;metadata:KnowledgeMetadata;links:KnowledgeLink[]};
export type KnowledgePage={items:KnowledgeSummary[];page:number;pageSize:number;total:number};
export const knowledgeKinds:KnowledgeKind[]=['npc','quest','location','season'];
export type KnowledgeOverviewEntry={kind:KnowledgeKind;page:KnowledgePage};
export type KnowledgeOverview={
 status:'ready'|'partial'|'unavailable';
 entries:KnowledgeOverviewEntry[];
 failedKinds:KnowledgeKind[];
 publishedTotal:number;
};

const apiOrigin=process.env.NOVA_API_ORIGIN??'http://localhost:5080';

async function publicGet<T>(path:string):Promise<T>{
 const response=await fetch(new URL(path,apiOrigin),{cache:'no-store',headers:{Accept:'application/json'}});
 if(!response.ok)throw new Error(`Knowledge API ${response.status}`);
 return response.json() as Promise<T>;
}

export const knowledgeApi={
 list:(kind?:KnowledgeKind,q='',page=1)=>{
  const path=kind?`/api/v1/knowledge/${encodeURIComponent(kind)}`:'/api/v1/knowledge';
  const query=new URLSearchParams({page:String(page),pageSize:'20'});
  if(q)query.set('q',q);
  return publicGet<KnowledgePage>(`${path}?${query}`);
 },
 detail:(kind:KnowledgeKind,slug:string)=>publicGet<KnowledgeDetail>(`/api/v1/knowledge/${encodeURIComponent(kind)}/${encodeURIComponent(slug)}`),
};

export async function loadKnowledgeOverview(
 list:(kind:KnowledgeKind)=>Promise<KnowledgePage>=kind=>knowledgeApi.list(kind),
):Promise<KnowledgeOverview>{
 const results=await Promise.allSettled(knowledgeKinds.map(kind=>list(kind)));
 const entries:KnowledgeOverviewEntry[]=[];
 const failedKinds:KnowledgeKind[]=[];
 for(const [index,result] of results.entries()){
  const kind=knowledgeKinds[index];
  if(!kind)continue;
  if(result.status==='fulfilled')entries.push({kind,page:result.value});
  else failedKinds.push(kind);
 }
 return {
  status:entries.length===0?'unavailable':failedKinds.length?'partial':'ready',
  entries,
  failedKinds,
  publishedTotal:entries.reduce((total,entry)=>total+entry.page.total,0),
 };
}

export const knowledgeKindLabels:Record<KnowledgeKind,string>={npc:'Nhân vật (NPC)',quest:'Sổ nhiệm vụ',location:'Bản đồ thế giới',season:'Mùa sự kiện'};
