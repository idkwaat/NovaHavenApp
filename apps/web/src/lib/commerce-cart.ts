export type CartLine = {slug:string;quantity:number};
const slugPattern=/^[a-z0-9]+(?:-[a-z0-9]+)*$/;

export function readCart(raw:string|null):CartLine[]{
 if(!raw)return [];
 try{
  const value:unknown=JSON.parse(raw);
  if(!Array.isArray(value))return [];
  const rows=new Map<string,number>();
  for(const row of value){
   if(!row||typeof row!=='object')continue;
   const {slug,quantity}=row as Record<string,unknown>;
   if(typeof slug!=='string'||!slugPattern.test(slug.trim())||typeof quantity!=='number'||!Number.isFinite(quantity))continue;
   const key=slug.trim();
   rows.set(key,Math.min(99,(rows.get(key)??0)+Math.max(1,Math.trunc(quantity))));
  }
  return [...rows].slice(0,20).map(([slug,quantity])=>({slug,quantity}));
 }catch{return [];}
}

export function setCartQuantity(cart:readonly CartLine[],slug:string,quantity:number):CartLine[]{
 if(!slugPattern.test(slug)||!Number.isFinite(quantity))return [...cart];
 const next=cart.filter(item=>item.slug!==slug);
 const amount=Math.trunc(quantity);
 if(amount<=0)return next;
 if(!cart.some(item=>item.slug===slug)&&next.length>=20)return [...cart];
 next.push({slug,quantity:Math.max(1,Math.min(99,amount))});
 return next;
}

export function removeCartItem(cart:readonly CartLine[],slug:string):CartLine[]{
 return cart.filter(item=>item.slug!==slug);
}

export function cartCount(cart:readonly CartLine[]):number{
 return cart.reduce((sum,item)=>sum+item.quantity,0);
}
