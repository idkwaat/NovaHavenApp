export type RewardKind='item'|'currency'|'permission'|'title'|'cosmetic'|'custom';
export type RewardSummary={id:string;slug:string;name:string;summary:string;kind:RewardKind;revision:number;externalAcknowledgementRequired:boolean;updatedAt:string};
export type RewardDetail=RewardSummary&{markdown:string;deliveryDescription:string;publishedAt:string};
export type RewardPage={items:RewardSummary[];page:number;pageSize:number;total:number};
const origin=process.env.NOVA_API_ORIGIN??'http://localhost:5080';
async function get<T>(path:string):Promise<T>{const response=await fetch(new URL(path,origin),{cache:'no-store'});if(!response.ok)throw Error(`Rewards API ${response.status}`);return response.json() as Promise<T>;}
export const rewardsApi={list:()=>get<RewardPage>('/api/v1/rewards?page=1&pageSize=50'),detail:(slug:string)=>get<RewardDetail>(`/api/v1/rewards/${encodeURIComponent(slug)}`)};
