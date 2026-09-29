export type CommerceKind='cosmetic'|'membership'|'bundle'|'donation'|'other';
export type CommerceSummary={id:string;slug:string;name:string;summary:string;kind:CommerceKind;revision:number;definitionOnly:boolean;isPurchasable:boolean;priceMinorUnits:number|null;currencyCode:'VND';checkoutMode:'localDemo'|null;updatedAt:string};
export type CommerceDetail=CommerceSummary&{markdown:string;displayPrice:string;providerProductCode?:string|null;publishedAt:string};
export type CommercePage={items:CommerceSummary[];page:number;pageSize:number;total:number};
const origin=process.env.NOVA_API_ORIGIN??'http://localhost:5080';
async function get<T>(path:string):Promise<T>{const response=await fetch(new URL(path,origin),{cache:'no-store'});if(!response.ok)throw Error(`Commerce API ${response.status}`);return response.json() as Promise<T>;}
export const commerceApi={list:()=>get<CommercePage>('/api/v1/commerce/offers?page=1&pageSize=50'),detail:(slug:string)=>get<CommerceDetail>(`/api/v1/commerce/offers/${encodeURIComponent(slug)}`)};
