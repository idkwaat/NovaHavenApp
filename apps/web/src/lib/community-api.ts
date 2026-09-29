export type CommunityKind='event'|'guild'|'player'|'housing'|'leaderboard';
export type CommunitySummary={id:string;slug:string;name:string;summary:string;kind:CommunityKind;revision:number;publishedAt:string};
export type CommunityRow={rank:number;participantName:string;score:number;note:string};
export type CommunityMetadata={startsAt?:string;endsAt?:string;location?:{slug:string;name:string;kind:string}|null;capacity?:number|null;registrationOpen?:boolean;motto?:string;discordUrl?:string;handle?:string;bio?:string;avatarUrl?:string;ownerDisplayName?:string;galleryMarkdown?:string;leaderboardCategory?:string;season?:{slug:string;name:string;kind:string}|null;rows:CommunityRow[]};
export type CommunityDetail=CommunitySummary&{markdown:string;metadata:CommunityMetadata};
export type CommunityPage={items:CommunitySummary[];page:number;pageSize:number;total:number};
const apiOrigin=process.env.NOVA_API_ORIGIN??'http://localhost:5080';
export const communityKinds:CommunityKind[]=['event','guild','player','housing','leaderboard'];
async function publicGet<T>(path:string):Promise<T>{const response=await fetch(new URL(path,apiOrigin),{cache:'no-store',headers:{Accept:'application/json'}});if(!response.ok)throw new Error(`Community API ${response.status}`);return response.json() as Promise<T>;}
export const communityApi={
 list:(kind?:CommunityKind,q='',page=1)=>{const path=kind?`/api/v1/community/${encodeURIComponent(kind)}`:'/api/v1/community';const query=new URLSearchParams({page:String(page),pageSize:'20'});if(q)query.set('q',q);return publicGet<CommunityPage>(`${path}?${query}`);},
 detail:(kind:CommunityKind,slug:string)=>publicGet<CommunityDetail>(`/api/v1/community/${encodeURIComponent(kind)}/${encodeURIComponent(slug)}`),
};
export type CommunityOverviewEntry={kind:CommunityKind;page:CommunityPage};
export type CommunityOverview={status:'ready'|'partial'|'unavailable';entries:CommunityOverviewEntry[];failedKinds:CommunityKind[];publishedTotal:number};
export async function loadCommunityOverview(fetchPage:(kind:CommunityKind)=>Promise<CommunityPage>=(kind)=>communityApi.list(kind)):Promise<CommunityOverview>{
 const settled=await Promise.allSettled(communityKinds.map(async kind=>({kind,page:await fetchPage(kind)})));
 const entries:CommunityOverviewEntry[]=[];
 const failedKinds:CommunityKind[]=[];
 for(let index=0;index<settled.length;index++){
  const result=settled[index];
  if(result.status==='fulfilled')entries.push(result.value);
  else failedKinds.push(communityKinds[index]);
 }
 return {status:entries.length===0?'unavailable':failedKinds.length===0?'ready':'partial',entries,failedKinds,publishedTotal:entries.reduce((total,entry)=>total+entry.page.total,0)};
}
export const communityKindLabels:Record<CommunityKind,string>={event:'Sự kiện',guild:'Bang hội',player:'Người chơi',housing:'Không gian sống',leaderboard:'Bảng xếp hạng'};
